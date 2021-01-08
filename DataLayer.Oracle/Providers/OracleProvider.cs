using System;
using System.Data;
using System.Linq;
using DataLayer.Oracle.Model;
using Oracle.ManagedDataAccess.Client;
// ReSharper disable InconsistentNaming

namespace DataLayer.Oracle.Providers
{
    public class OracleProvider : IDatabaseProvider
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

            try
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
            }
            finally
            {
                if (conn.State == ConnectionState.Open && setUser)
                {
                    throw new NotImplementedException("Set user function will be added in the future");
                }
            }

            return conn;
        }

        public OracleCommand InitializeCommand(bool setUser, ParameterCollection param)
        {
            if (param == null)
            {
                throw new ArgumentNullException(nameof(param.CommandText), "Parameter Collection is null.");
            }

            var conn = CreateOpenConnection(setUser);
            OracleCommand cmd = null;

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

        public OracleCommand InitializeCommand(ref OracleConnection conn, bool setUser, ParameterCollection param)
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

        public string GetReturnMessage(ref OracleCommand cmd)
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
    }
}
