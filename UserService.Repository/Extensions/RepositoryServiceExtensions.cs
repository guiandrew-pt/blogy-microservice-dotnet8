using Microsoft.Extensions.DependencyInjection;
using UserService.Repository.Data;
using UserService.Repository.Repositories;
using UserService.Repository.Repositories.Interfaces;

namespace UserService.Repository.Extensions
{
    public static class RepositoryServiceExtensions
    {
        public static IServiceCollection AddRepositoryServices(this IServiceCollection services, string connectionString)
        {
            // Register MySqlConnectionFactory with the provided connection string
            services.AddSingleton<MySqlConnectionFactory>(_ => new MySqlConnectionFactory(connectionString));

            // Register the repository
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }
    }
}

