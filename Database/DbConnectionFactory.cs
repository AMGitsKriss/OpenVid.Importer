using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;

namespace Database
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly ConnectionStringOptions _connectionStrings;

        public IDbConnection OpenDefault() => OpenConnection(_connectionStrings.DefaultDatabase);

        public DbConnectionFactory(IOptions<ConnectionStringOptions> connectionStrings)
        {
            _connectionStrings = connectionStrings.Value;
        }

        private IDbConnection OpenConnection(string connectionString)
        {
            var sqlConnection = CreateConnection(connectionString);
            sqlConnection.Open();
            return sqlConnection;
        }

        private IDbConnection CreateConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

    }
}
