using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DataLayer.Oracle.Model;
using Oracle.ManagedDataAccess.Client;
// ReSharper disable InconsistentNaming

namespace DataLayer.Oracle.Providers
{
    public class OracleProvider
    {
        internal const int cReturnMsgIndx = 0;
        private const string cParamReturn = "RETURN_VALUE";
        public const string cNullValue = "NULL";

        private int UserId { get; set; }

        private string ConnectionString { get; set; }

        public void SetUser(int userId)
        {
            UserId = userId;
        }

        public void SetConnectionString(string connStr)
        {
            ConnectionString = connStr;
        }

        public OracleConnection CreateOpenConnection(bool setUser)
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                throw new ArgumentNullException(nameof(ConnectionString), "Connection String is null. Cannot create connection.");
            }

            var conn = new OracleConnection();

            conn.ConnectionString = ConnectionString;
            conn.Open();
            conn.BeginTransaction();
            if (conn.State == ConnectionState.Open && setUser)
            {
                throw new NotImplementedException("Set user function will be added in the future");
            }

            return conn;
        }

        public OracleCommand InitializeCommand(ParameterCollection param, bool setUser = false)
        {
            if (param == null)
            {
                throw new ArgumentNullException(nameof(param.CommandText), "Parameter Collection is null.");
            }

            var conn = CreateOpenConnection(setUser);
            OracleCommand cmd;

            try
            {
                if (string.IsNullOrEmpty(param.CommandText))
                {
                    throw new ArgumentNullException(nameof(param.CommandText), "CommandText is null.");
                }

                cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = param.CommandText;


            }
            catch (Exception)
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                throw;
            }

            return cmd;
        }

        public OracleCommand InitializeCommand(ref OracleConnection conn, ParameterCollection param, bool setUser = false)
        {
            if (param == null)
            {
                throw new ArgumentNullException(nameof(param.CommandText), "Parameter Collection is null.");
            }

            if (string.IsNullOrEmpty(param.CommandText))
            {
                throw new ArgumentNullException(nameof(param.CommandText), "CommandText is null.");
            }

            conn ??= CreateOpenConnection(setUser);
            var cmd = conn.CreateCommand();

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = param.CommandText;
            }
            catch (Exception)
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                throw;
            }

            return cmd;
        }

        public void AssignCommandParams(ref OracleCommand cmd, ParameterCollection param)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException(nameof(cmd), "Command is null.");
            }

            try
            {
                OracleCommandBuilder.DeriveParameters(cmd);

                if (param.InputParameters != null)
                {
                    foreach (var item in param.InputParameters
                        .Select((input, index) => new { index, input }))
                        try
                        {
                            object paramValue = DBNull.Value;
                            if (item.input.Item2 != null)
                            {
                                paramValue = item.input.Item2;
                            }
                            cmd.Parameters[item.input.Item1].Value = paramValue;
                            cmd.Parameters[item.input.Item1].Direction = ParameterDirection.Input;
                        }
                        catch (Exception)
                        {
                            cmd.Parameters[item.input.Item1].Value = DBNull.Value;
                        }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void DisposeAndCloseConn(ref OracleConnection conn, ref OracleCommand cmd, string returnMsg)
        {
            EvaluateError(ref cmd, returnMsg);
            cmd?.Dispose();
            conn?.Dispose();
            conn?.Close();
        }

        public void EvaluateError(ref OracleCommand cmd, string returnMsg)
        {
            if (string.IsNullOrEmpty(returnMsg))
                cmd.Transaction?.Commit();
            else
                cmd.Transaction?.Rollback();
        }
        
        private string GetReturnMessage(ref OracleCommand cmd)
        {
            var returnMsg = string.Empty;
            try
            {
                if (cmd.Parameters.Contains(cParamReturn))
                {
                    var firstOrDefault = cmd.Parameters
                        .Cast<OracleParameter>()
                        .FirstOrDefault(p => p.ParameterName == cParamReturn);
                    if (firstOrDefault != null)
                        returnMsg =
                            firstOrDefault
                                .Value.ToString();
                }
            }
            catch (Exception)
            {
                returnMsg = string.Empty;
            }
            return returnMsg;
        }
        
        private void GetOutParamValueList(ref OracleCommand cmd, ref ParameterCollection param)
        {
            if (param.OutputParameters != null)
            {
                var outParameters = new List<Tuple<string, object>>();
                foreach (var parameter in param.OutputParameters)
                {
                    string paramName = parameter.Item1;
                    object paramValue = string.Empty;
                    if (!string.IsNullOrEmpty(paramName))
                    {
                        if (cmd.Parameters[paramName].Value != DBNull.Value)
                        {
                            paramValue = cmd.Parameters[paramName].Value;
                        }

                        outParameters.Add(new Tuple<string, object>(paramName, paramValue));
                    }
                }

                param.OutputParameters = outParameters;
            }
        }

        public ResponseList<T> ExecuteReader<T>(ref ParameterCollection param, bool setUser = false) where T : class, new()
        {
            var returnMsg = string.Empty;
            var conn = CreateOpenConnection(setUser);
            var cmd = InitializeCommand(ref conn, param, setUser);
            ResponseList<T> response;
            try
            {
                AssignCommandParams(ref cmd, param);
                IDataReader reader = cmd.ExecuteReader();

                var list = OracleHelpers.MapDataToList<T>(ref reader);
                returnMsg = GetReturnMessage(ref cmd);
                response = new ResponseList<T>(list, returnMsg);
            }
            catch (Exception e)
            {
                response = new ResponseList<T>(null, e.Message);
            }
            finally
            {
                DisposeAndCloseConn(ref conn, ref cmd, returnMsg);

            }

            return response;
        }

        public BasicResponse ExecuteBasicStoredProcedure(ref ParameterCollection param, bool setUser = false)
        {
            var returnMsg = string.Empty;
            var conn = CreateOpenConnection(setUser);
            var cmd = InitializeCommand(ref conn, param, setUser);
            BasicResponse response;

            try
            {
                AssignCommandParams(ref cmd, param);
                cmd.ExecuteNonQuery();
                returnMsg = GetReturnMessage(ref cmd);
                GetOutParamValueList(ref cmd, ref param);
            }
            catch (Exception e)
            {
                returnMsg = e.Message;
            }
            finally
            {
                response = new BasicResponse(returnMsg);
                DisposeAndCloseConn(ref conn, ref cmd, returnMsg);
            }

            return response;
        }

        public ResponseList<T> ExecuteStoredProcedure<T>(ref ParameterCollection param, bool setUser = false) where T : class, new()
        {
            var returnMsg = string.Empty;
            var conn = CreateOpenConnection(setUser);
            var cmd = InitializeCommand(ref conn, param, setUser);
            ResponseList<T> response;
            List<T> listOfT = new List<T>();

            try
            {
                AssignCommandParams(ref cmd, param);
                cmd.ExecuteNonQuery();
                returnMsg = GetReturnMessage(ref cmd);
                GetOutParamValueList(ref cmd, ref param);

                var outputCursor = param.OutputParameters?.FirstOrDefault(p => p.Item2 is OracleDataReader);
                if (outputCursor != null)
                {
                    IDataReader reader = (OracleDataReader)outputCursor.Item2;
                    var cursor = OracleHelpers.MapDataToList<T>(ref reader);
                    listOfT = cursor;
                }
            }
            catch (Exception e)
            {
                returnMsg = e.Message;
            }
            finally
            {
                response = new ResponseList<T>(listOfT, returnMsg);
                DisposeAndCloseConn(ref conn, ref cmd, returnMsg);
            }

            return response;
        }
    }

    static class OracleHelpers
    {
        public static List<T> MapDataToList<T>(ref IDataReader dr)
                where T : new()
        {
            var entityType = typeof(T);
            var entityList = new List<T>();
            var hashtable = new Hashtable();
            var properties = entityType.GetProperties();
            foreach (var info in properties)
                hashtable[info.Name.ToUpper(new CultureInfo("en-Us"))] = info;

            try
            {
                while (dr.Read())
                {
                    var newObject = new T();
                    for (var index = 0; index < dr.FieldCount; index++)
                    {
                        var info = (PropertyInfo)
                            hashtable[dr.GetName(index).ToUpper()];
                        try
                        {
                            if ((info != null) && info.CanWrite)
                            {
                                dynamic value = GetTypedValue(ref info, ref dr, index);
                                if ((value != null) && !value.ToString().Equals(string.Empty))
                                {
                                    if (info.PropertyType == typeof(Boolean))
                                    {
                                        info.SetValue(newObject, Convert.ToBoolean(value), null);
                                    }
                                    else
                                    {
                                        info.SetValue(newObject, value, null);
                                    }
                                }


                            }
                        }
                        catch (Exception exc)
                        {
                            Trace.WriteLine("Property Mapping Error. Property Name: " + dr.GetName(index).ToUpper() + " Error message: " + exc.Message);
                        }
                    }
                    entityList.Add(newObject);
                }
            }
            finally
            {
                dr.Close();
            }

            return entityList;
        }

        private static dynamic GetTypedValue(ref PropertyInfo info, ref IDataReader dr, int index)
        {
            dynamic value;
            if ((info.PropertyType == typeof(byte)) || (info.PropertyType == typeof(byte?)))
                value = !dr.GetValue(index).Equals(DBNull.Value) ? (dynamic)dr.GetByte(index) : DBNull.Value;
            else if ((info.PropertyType == typeof(int)) || (info.PropertyType == typeof(int?)))
                value = !dr.GetValue(index).Equals(DBNull.Value) ? (dynamic)dr.GetInt32(index) : DBNull.Value;
            else if ((info.PropertyType == typeof(long)) || (info.PropertyType == typeof(long?)))
                value = !dr.GetValue(index).Equals(DBNull.Value) ? (dynamic)dr.GetInt64(index) : DBNull.Value;
            else if ((info.PropertyType == typeof(decimal)) || (info.PropertyType == typeof(decimal?)))
                value = !dr.GetValue(index).Equals(DBNull.Value) ? (dynamic)dr.GetDecimal(index) : DBNull.Value;
            else if ((info.PropertyType == typeof(double)) || (info.PropertyType == typeof(double?)))
                value = !dr.GetValue(index).Equals(DBNull.Value) ? (dynamic)dr.GetDouble(index) : DBNull.Value;
            else if ((info.PropertyType == typeof(short)) || (info.PropertyType == typeof(short?)))
                value = !dr.GetValue(index).Equals(DBNull.Value) ? (dynamic)dr.GetInt16(index) : string.Empty;
            else if ((info.PropertyType == typeof(short)) || (info.PropertyType == typeof(short?)))
                value = !dr.GetValue(index).Equals(DBNull.Value) ? (dynamic)dr.GetInt16(index) : string.Empty;
            else
                value = !dr.GetValue(index).Equals(DBNull.Value) ? dr.GetValue(index) : string.Empty;

            return value;
        }

    }
}
