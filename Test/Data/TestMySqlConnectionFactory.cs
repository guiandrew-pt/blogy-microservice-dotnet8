using UserService.Repository.Data;

namespace Test.Data
{
    // Test-specific implementation of MySqlConnectionFactory
    public class TestMySqlConnectionFactory : MySqlConnectionFactory
    {
        // Constructor explicitly accepts the test connection string
        public TestMySqlConnectionFactory(string? testConnectionString)
            : base(testConnectionString!) // Pass the test connection string to the base constructor
        {
        }
    }
}

