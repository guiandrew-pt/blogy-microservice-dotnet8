using System.Data;
using MySql.Data.MySqlClient;

namespace UserService.Repository.Data
{
    public class MySqlConnectionFactory
    {
        private readonly string _connectionString;

        // Constructor accepts a connection string as an injectable parameter
        public MySqlConnectionFactory(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("The connection string cannot be null or empty!");
            }
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}

