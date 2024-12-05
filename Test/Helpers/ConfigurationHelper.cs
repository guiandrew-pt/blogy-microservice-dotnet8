using Microsoft.Extensions.Configuration;

namespace Test.Helpers
{
	public class ConfigurationHelper
	{
        public static IConfiguration GetTestConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory) // Points to the test's output directory
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Loads appsettings.json
                .Build();

        }
    }
}

