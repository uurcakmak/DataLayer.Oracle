using DataLayer.Oracle.Model;
using Oracle.ManagedDataAccess.Client;

namespace DataLayer.Oracle.Providers
{
    interface IDatabaseProvider<T> where T : class
    {
        public void SetUser(int userId);

        public void SetConnectionString(string connStr);

        public OracleConnection CreateOpenConnection(bool setUser);

        public OracleCommand InitializeCommand(bool setUser, ParameterCollection param);

        public OracleCommand InitializeCommand(ref OracleConnection conn, bool setUser, ParameterCollection param);

        public void AssignCommandParams(ref OracleCommand cmd, ParameterCollection param);

        public void DisposeAndCloseConn(ref OracleConnection conn, ref OracleCommand cmd, string returnMsg);

        public void EvaluateError(ref OracleCommand cmd, string returnMsg);

        public ResponseList<T> ExecuteReader<T>(ParameterCollection param, bool setUser = true) where T : class;
    }
}
