using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Test.Data;
using Test.Helpers;
using UserService.Domain.Models;
using UserService.Repository.Repositories;

namespace Test.UserTests
{
	public class UserRepositoryTests
	{
        private readonly string? _testConnection;

        public UserRepositoryTests()
		{
            // Load configuration from appsettings.json
            IConfiguration? configuration = ConfigurationHelper.GetTestConfiguration();

            if (configuration is null)
                throw new InvalidOperationException("Appsettings not found!");

            // Retrieve the connection string from configuration
            string connectionString = configuration.GetConnectionString("DefaultConnectionTest")!;

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("The test connection string is not configured!");

            _testConnection = connectionString;
        }

        [Fact]
        public async Task GetUsersAsync_ShouldReturnUsers_WithProfiles_AndTotalCount()
        {
            // Arrange
            using MySqlConnection connection = await CreateAndOpenConnectionAsync();

            await ClearSchemaAsync(connection);
            await CreateSchemaAsync(connection);

            // Insert user data
            const string insertUserQuery = @"
                INSERT INTO Users
                    (Username, Email, PasswordHash, FirstName, LastName, ProfilePictureUrl, DateCreated)
                VALUES
                    (@Username, @Email, @PasswordHash, @FirstName, @LastName, @ProfilePictureUrl, @DateCreated);
                SELECT LAST_INSERT_ID();";

            // Insert user profile data
            const string insertProfileQuery = @"
                INSERT INTO UserProfile
                    (UserId, Bio, WebsiteUrl, SocialLinks)
                VALUES
                    (@UserId, @Bio, @WebsiteUrl, @SocialLinks);";

            // Create and insert the first user with a profile
            User? user1 = new User
            {
                Username = "profileuser1",
                Email = "profileuser1@example.com",
                PasswordHash = "password1",
                FirstName = "Profile",
                LastName = "User1",
                DateCreated = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    Bio = "User bio 1",
                    WebsiteUrl = "http://example1.com",
                    SocialLinks = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        { "twitter", "https://twitter.com/user1" },
                        { "github", "https://github.com/user1" }
                    })
                }
            };

            int userId1 = await connection.ExecuteScalarAsync<int>(insertUserQuery, user1);

            await connection.ExecuteAsync(insertProfileQuery, new
            {
                UserId = userId1,
                user1.Profile.Bio,
                user1.Profile.WebsiteUrl,
                user1.Profile.SocialLinks
            });

            // Create and insert the second user with a profile
            User? user2 = new User
            {
                Username = "profileuser2",
                Email = "profileuser2@example.com",
                PasswordHash = "password2",
                FirstName = "Profile",
                LastName = "User2",
                DateCreated = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    Bio = "User bio 2",
                    WebsiteUrl = "http://example2.com",
                    SocialLinks = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        { "twitter", "https://twitter.com/user2" },
                        { "github", "https://github.com/user2" }
                    })
                }
            };

            int userId2 = await connection.ExecuteScalarAsync<int>(insertUserQuery, user2);

            await connection.ExecuteAsync(insertProfileQuery, new
            {
                UserId = userId2,
                user2.Profile.Bio,
                user2.Profile.WebsiteUrl,
                user2.Profile.SocialLinks
            });

            // Use the TestMySqlConnectionFactory to ensure the repository uses the test connection
            UserRepository repository = CreateUserRepository();

            string search = "profileuser";
            int page = 1;
            int pageSize = 2;

            // Act 
            (IEnumerable<User> users, int totalCount) = await repository.GetUsersAsync(search, page, pageSize);

            // Assert
            Assert.NotNull(users);
            Assert.True(users.Any(), "Users list should not be empty.");

            // Assert total count
            Assert.Equal(2, totalCount); // Two users match the search term "profileuser"

            // Assert returned users are within the page size
            Assert.True(users.Count() <= pageSize, "Returned users should be within the page size.");

            // Assert that all returned users match the search filter
            Assert.All(users, user =>
            {
                Assert.StartsWith(search, user.Username, StringComparison.OrdinalIgnoreCase);

                // Assert user profile is attached correctly
                Assert.NotNull(user.Profile);
                Assert.False(string.IsNullOrEmpty(user.Profile.Bio));
                Assert.False(string.IsNullOrEmpty(user.Profile.WebsiteUrl));
            });

            // Assert specific data for user1 and user2
            User? returnedUser1 = users.FirstOrDefault(u => u.Username == "profileuser1");
            Assert.NotNull(returnedUser1);
            Assert.Equal("User bio 1", returnedUser1?.Profile?.Bio);

            User? returnedUser2 = users.FirstOrDefault(u => u.Username == "profileuser2");
            Assert.NotNull(returnedUser2);
            Assert.Equal("User bio 2", returnedUser2?.Profile?.Bio);
        }

        [Fact]
        public async Task GetUserWithProfileByIdAsync_ShouldReturnUserWithProfile()
        {
            // Arrange
            using MySqlConnection connection = await CreateAndOpenConnectionAsync();

            await ClearSchemaAsync(connection);
            await CreateSchemaAsync(connection);

            const string insertUserQuery = @"
                INSERT INTO Users
                    (Username, Email, PasswordHash, FirstName, LastName, ProfilePictureUrl, DateCreated)
                VALUES
                    (@Username, @Email, @PasswordHash, @FirstName, @LastName, @ProfilePictureUrl, @DateCreated);
                SELECT LAST_INSERT_ID();";

            const string insertUserProfileQuery = @"
                INSERT INTO UserProfile
                    (UserId, Bio, WebsiteUrl, SocialLinks)
                VALUES
                    (@UserId, @Bio, @WebsiteUrl, @SocialLinks);";

            // Create and insert the user and profile
            User? user = new User
            {
                Username = "profileuser",
                Email = "profileuser@example.com",
                PasswordHash = "password",
                FirstName = "Profile",
                LastName = "User",
                DateCreated = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    Bio = "User bio",
                    WebsiteUrl = "http://example.com",
                    SocialLinks = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        { "twitter", "https://twitter.com/user" },
                        { "github", "https://github.com/user" }
                    })
                }
            };

            int userId = await connection.ExecuteScalarAsync<int>(insertUserQuery, user);
            user.Id = userId;

            await connection.ExecuteAsync(insertUserProfileQuery, new
            {
                UserId = user.Id,
                user.Profile.Bio,
                user.Profile.WebsiteUrl,
                user.Profile.SocialLinks
            });

            // Act
            UserRepository repository = CreateUserRepository();

            User? retrievedUser = await repository.GetUserWithProfileByIdAsync(user.Id);

            // Assert
            Assert.NotNull(retrievedUser);
            Assert.Equal(user.Username, retrievedUser?.Username);

            Assert.NotNull(retrievedUser?.Profile);
            Assert.Equal(user.Profile.Bio, retrievedUser?.Profile?.Bio);

            Assert.NotNull(retrievedUser?.Profile?.SocialLinks);

            // Deserialize and assert SocialLinks
            try
            {
                Dictionary<string, string>? socialLinks = JsonSerializer.Deserialize<Dictionary<string, string>>(retrievedUser.Profile.SocialLinks!);
                Assert.NotNull(socialLinks);
                Assert.Equal("https://twitter.com/user", socialLinks?["twitter"]);
                Assert.Equal("https://github.com/user", socialLinks?["github"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing SocialLinks for UserId {retrievedUser?.Id}: {ex.Message}");
                throw;
            }
        }

        [Fact]
        public async Task AddUserWithProfileAsync_ShouldAddUserAndProfile()
        {
            // Arrange
            using MySqlConnection connection = await CreateAndOpenConnectionAsync();

            await ClearSchemaAsync(connection); // Clear schema
            await CreateSchemaAsync(connection); // Create schema

            UserRepository repository = CreateUserRepository();

            // User and profile to add
            User? user = new User
            {
                Username = "profileuser",
                Email = "profileuser@example.com",
                PasswordHash = "password",
                FirstName = "Profile",
                LastName = "User",
                DateCreated = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    Bio = "User bio",
                    WebsiteUrl = "http://example.com",
                    SocialLinks = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        { "Twitter", "https://twitter.com/user" },
                        { "GitHub", "https://github.com/user" }
                    })
                }
            };

            // Act
            int userId = await repository.AddUserWithProfileAsync(user);

            // Assert
            User? addedUser = await repository.GetUserWithProfileByIdAsync(userId);

            Assert.NotNull(addedUser);
            Assert.Equal(user.Username, addedUser.Username);

            Assert.NotNull(addedUser.Profile);
            Assert.Equal(user.Profile.Bio, addedUser.Profile.Bio);
            Assert.Equal(user.Profile.WebsiteUrl, addedUser.Profile.WebsiteUrl);

            // Validate SocialLinks
            Assert.NotNull(addedUser.Profile.SocialLinks);

            // Attempt to deserialize the JSON string
            Dictionary<string, string>? socialLinks = JsonSerializer.Deserialize<Dictionary<string, string>>(addedUser.Profile.SocialLinks);

            // Assert that the deserialized dictionary is not null
            Assert.NotNull(socialLinks);

            // Validate the values in the dictionary
            Assert.Equal("https://twitter.com/user", socialLinks?["Twitter"]);
            Assert.Equal("https://github.com/user", socialLinks?["GitHub"]);
        }

        [Fact]
        public async Task UpdateUserWithProfileAsync_ShouldUpdateUserAndProfile()
        {
            // Arrange
            using MySqlConnection connection = await CreateAndOpenConnectionAsync();

            await ClearSchemaAsync(connection); // Clear schema
            await CreateSchemaAsync(connection); // Create schema

            UserRepository repository = CreateUserRepository();

            // Insert a user with a profile
            User? user = new User
            {
                Username = "olduser",
                Email = "olduser@example.com",
                PasswordHash = "oldpassword",
                FirstName = "Old",
                LastName = "User",
                DateCreated = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    Bio = "Old bio",
                    WebsiteUrl = "http://old.example.com",
                    SocialLinks = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        { "Twitter", "http://twitter.com/olduser" },
                        { "GitHub", "http://github.com/olduser" }
                    })
                }
            };

            int userId = await repository.AddUserWithProfileAsync(user);

            // Modify user and profile data for the update
            user.Id = userId;
            user.Username = "updateduser";
            user.Email = "updateduser@example.com";
            user.PasswordHash = "newpassword";
            user.FirstName = "Updated";
            user.LastName = "User";
            user.Profile.Bio = "Updated bio";
            user.Profile.WebsiteUrl = "http://updated.example.com";
            user.Profile.SocialLinks = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "Twitter", "http://twitter.com/updateduser" },
                    { "LinkedIn", "http://linkedin.com/in/updateduser" }
                });

            // Act: Update the user and their profile
            int rowsAffected = await repository.UpdateUserWithProfileAsync(user);

            // Assert: Check if rows were updated
            Assert.True(rowsAffected > 0, "No rows were updated in the UserProfile table.");

            // Fetch updated user and profile
            User? updatedUser = await repository.GetUserWithProfileByIdAsync(userId);

            // Assert: User details
            Assert.NotNull(updatedUser);
            Assert.Equal("updateduser", updatedUser.Username);
            Assert.Equal("updateduser@example.com", updatedUser.Email);
            Assert.Equal("Updated", updatedUser.FirstName);
            Assert.Equal("User", updatedUser.LastName);

            // Deserialize SocialLinks for assertion
            Dictionary<string, string>? deserializedSocialLinks = updatedUser.Profile?.SocialLinks is not null
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(updatedUser.Profile.SocialLinks)
                : null;

            // Assert: Profile details
            Assert.NotNull(updatedUser.Profile);
            Assert.Equal("Updated bio", updatedUser.Profile.Bio);
            Assert.Equal("http://updated.example.com", updatedUser.Profile.WebsiteUrl);

            // Assert: SocialLinks deserialization
            Assert.NotNull(deserializedSocialLinks);
            Assert.Equal(2, deserializedSocialLinks.Count);
            Assert.Equal("http://twitter.com/updateduser", deserializedSocialLinks["Twitter"]);
            Assert.Equal("http://linkedin.com/in/updateduser", deserializedSocialLinks["LinkedIn"]);
        }

        [Fact]
        public async Task DeleteUserWithProfileAsync_ShouldDeleteUserAndProfile()
        {
            // Arrange
            using MySqlConnection connection = await CreateAndOpenConnectionAsync();

            await ClearSchemaAsync(connection); // Clear schema
            await CreateSchemaAsync(connection); // Create schema

            UserRepository repository = CreateUserRepository();

            User? user = new User
            {
                Username = "deleteuser",
                Email = "deleteuser@example.com",
                PasswordHash = "password",
                FirstName = "Delete",
                LastName = "User",
                DateCreated = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    Bio = "Delete bio",
                    WebsiteUrl = "http://example.com"
                }
            };

            int userId = await repository.AddUserWithProfileAsync(user);

            // Act
            await repository.DeleteUserWithProfileAsync(userId);
            User? deletedUser = await repository.GetUserWithProfileByIdAsync(userId);

            // Assert
            Assert.Null(deletedUser);
        }

        private async Task CreateSchemaAsync(MySqlConnection connection)
        {
            const string createTablesQuery = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Username VARCHAR(50) NOT NULL,
                    Email VARCHAR(100) NOT NULL UNIQUE,
                    PasswordHash VARCHAR(50) NOT NULL,
                    FirstName VARCHAR(50) NOT NULL,
                    LastName VARCHAR(50) NOT NULL,
                    ProfilePictureUrl VARCHAR(255), -- Optional
                    DateCreated DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
                CREATE TABLE IF NOT EXISTS UserProfile (
                    UserId INT PRIMARY KEY, 
                    Bio TEXT,
                    WebsiteUrl VARCHAR(255),
                    SocialLinks JSON,   
                    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
                );";
            await connection.ExecuteAsync(createTablesQuery);
        }

        private async Task ClearSchemaAsync(MySqlConnection connection)
        {
            const string clearTablesQuery = @"
                DELETE FROM UserProfile;
                DELETE FROM Users;";
            await connection.ExecuteAsync(clearTablesQuery);
        }

        private async Task<MySqlConnection> CreateAndOpenConnectionAsync()
        {
            MySqlConnection? connection = new MySqlConnection(_testConnection);
            await connection.OpenAsync();
            return connection;
        }

        private UserRepository CreateUserRepository()
        {
            var factory = new TestMySqlConnectionFactory(_testConnection);
            return new UserRepository(factory);
        }
    }
}

