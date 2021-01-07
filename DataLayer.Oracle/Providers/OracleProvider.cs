using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DataLayer.Oracle.Model;
using Oracle.ManagedDataAccess.Client;
// ReSharper disable InconsistentNaming

namespace DataLayer.Oracle.Providers
{
    public class OracleProvider : IDatabaseProvider
    {
        private const int cReturnMsgIndx = 0;
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
                throw new ArgumentNullException("Connection String is null. Cannot create connection.");
            }

            var conn = new OracleConnection();

            try
            {
                conn.ConnectionString = ConnectionString;
                conn.Open();
            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                if (conn.State == ConnectionState.Open && setUser)
                {
                   //todo 
                }
            }

            return conn;
        }

        public OracleCommand InitializeCommand(bool setUser, ParameterCollection param)
        {
            if (param == null)
            {
                throw new ArgumentNullException("Parameter Collection is null.", nameof(param.CommandText));
            }

            var conn = CreateOpenConnection(setUser);
            OracleCommand cmd = null;

            try
            {
                if (string.IsNullOrEmpty(param.CommandText))
                {
                    throw new ArgumentNullException("CommandText is null.", nameof(param.CommandText));
                }

                cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = param.CommandText;


            }
            catch (Exception e)
            {
                cmd = null;
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
                throw new ArgumentNullException("Parameter Collection is null.", nameof(param.CommandText));
            }

            if (string.IsNullOrEmpty(param.CommandText))
            {
                throw new ArgumentNullException("CommandText is null.", nameof(param.CommandText));
            }

            conn ??= CreateOpenConnection(setUser);
            var cmd = conn.CreateCommand();

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = param.CommandText;
            }
            catch (Exception e)
            {
                cmd = null;
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
                throw new ArgumentNullException("Command is null.", nameof(cmd));
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
