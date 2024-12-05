using System.Data;
using System.Text.Json;
using Dapper;
using UserService.Domain.Models;
using UserService.Repository.Data;
using UserService.Repository.Repositories.Interfaces;
using static Dapper.SqlMapper;

namespace UserService.Repository.Repositories
{
	public class UserRepository : IUserRepository
    {
        private readonly MySqlConnectionFactory _connectionFactory;

        public UserRepository(MySqlConnectionFactory connectionFactory)
		{
            _connectionFactory = connectionFactory;
        }

        // Get users with pagination and optional filtering
        public async Task<(IEnumerable<User> Users, int TotalCount)> GetUsersAsync(string? search = null, int page = 1, int pageSize = 10)
        {
            //const string querySql = @"
            //    SELECT
            //        u.Id, u.Username, u.Email, u.PasswordHash, u.FirstName, u.LastName, u.ProfilePictureUrl, u.DateCreated,
            //        p.UserId, p.Bio, p.WebsiteUrl, p.SocialLinks
            //    FROM Users u
            //        LEFT JOIN UserProfile p ON u.Id = p.UserId
            //    WHERE (@Search IS NULL OR u.Username LIKE CONCAT(@Search, '%')) -- Optimized prefix search
            //        LIMIT @Offset, @PageSize;";
            const string querySql = @"
                SELECT
                    u.Id, u.Username, u.Email, u.PasswordHash, u.FirstName, u.LastName, u.ProfilePictureUrl, u.DateCreated,
                    p.UserId, p.Bio, p.WebsiteUrl, p.SocialLinks
                FROM Users u
                    LEFT JOIN UserProfile p ON u.Id = p.UserId
                WHERE (@Search IS NULL OR u.Username LIKE CONCAT(@Search, '%'))
                LIMIT @Offset, @PageSize;

                SELECT COUNT(1)
                    FROM Users u
                WHERE (@Search IS NULL OR u.Username LIKE CONCAT(@Search, '%'));";

            int offset = (page - 1) * pageSize;

            using IDbConnection? connection = _connectionFactory.CreateConnection();

            using GridReader? multi = await connection.QueryMultipleAsync(querySql, new
            {
                Search = search,
                Offset = offset,
                PageSize = pageSize
            });

            // Fetch users
            var users = multi.Read<User, UserProfile, User>(
                (user, profile) =>
                {
                    // Attach profile to user
                    if (profile != null && !string.IsNullOrEmpty(profile.SocialLinks))
                    {
                        profile.SocialLinks = JsonSerializer.Serialize(JsonSerializer.Deserialize<Dictionary<string, string>>(profile.SocialLinks));
                    }
                    user.Profile = profile;
                    return user;
                },
                splitOn: "UserId");

            // Fetch total count
            int totalCount = multi.ReadSingle<int>();

            return (users, totalCount);
        }

        // Get a user by ID with their profile
        public async Task<User?> GetUserWithProfileByIdAsync(int userId)
        {
            const string querySql = @"
                SELECT
                  Id, Username, Email, PasswordHash, FirstName, LastName, ProfilePictureUrl, DateCreated
                FROM Users WHERE Id = @UserId;
                SELECT
                  UserId, Bio, WebsiteUrl, SocialLinks
                FROM UserProfile WHERE UserId = @UserId;";

            using IDbConnection? connection = _connectionFactory.CreateConnection();
            using GridReader? multi = await connection.QueryMultipleAsync(querySql, new { UserId = userId });

            // Read the user
            User? user = await multi.ReadSingleOrDefaultAsync<User>();

            if (user is not null)
            {
                // Read the profile
                UserProfile? profile = await multi.ReadSingleOrDefaultAsync<UserProfile>();

                if (profile is not null && !string.IsNullOrEmpty(profile.SocialLinks))
                {
                    try
                    {
                        // Deserialize the raw JSON into a Dictionary
                        Dictionary<string, string>? socialLinksDict = JsonSerializer.Deserialize<Dictionary<string, string>>(profile.SocialLinks!);

                        // Optionally re-serialize for consistency
                        profile.SocialLinks = JsonSerializer.Serialize(socialLinksDict);
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Error deserializing SocialLinks for UserId {userId}: {ex.Message}");
                        profile.SocialLinks = null; // Fallback to null if deserialization fails
                    }
                }

                // Assign the profile to the user
                user.Profile = profile;
            }

            return user;
        }

        // Add a new user with profile
        public async Task<int> AddUserWithProfileAsync(User user)
        {
            const string insertUserSql = @"
                INSERT INTO Users
                    (Username, Email, PasswordHash, FirstName, LastName, ProfilePictureUrl, DateCreated)
                VALUES
                    (@Username, @Email, @PasswordHash, @FirstName, @LastName, @ProfilePictureUrl, @DateCreated);
                SELECT LAST_INSERT_ID();";

            const string insertProfileSql = @"
                INSERT INTO UserProfile
                    (UserId, Bio, WebsiteUrl, SocialLinks)
                VALUES
                    (@UserId, @Bio, @WebsiteUrl, @SocialLinks);";

            using IDbConnection? connection = _connectionFactory.CreateConnection();

            try
            {
                // Insert the user into the Users table and retrieve the generated UserId
                int userId = await connection.ExecuteScalarAsync<int>(insertUserSql, user);

                // Insert the profile into the UserProfile table, if the profile is provided
                if (user.Profile is not null)
                {
                    // Ensure SocialLinks is valid JSON
                    string? serializedSocialLinks = JsonSerializer.Serialize(
                        JsonSerializer.Deserialize<Dictionary<string, string>>(user.Profile.SocialLinks ?? "{}")
                    );

                    // Insert the profile into the database
                    await connection.ExecuteAsync(insertProfileSql, new UserProfile
                    {
                        UserId = userId,
                        Bio = user.Profile.Bio,
                        WebsiteUrl = user.Profile.WebsiteUrl,
                        SocialLinks = serializedSocialLinks  // Already a JSON string
                    });
                }

                return userId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user and profile: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        // Update a user and their profile
        public async Task<int> UpdateUserWithProfileAsync(User user)
        {
            const string updateUserSql = @"
                UPDATE Users
                SET Username = @Username,
                    Email = @Email,
                    PasswordHash = @PasswordHash,
                    FirstName = @FirstName,
                    LastName = @LastName,
                    ProfilePictureUrl = @ProfilePictureUrl,
                    DateCreated = @DateCreated
                WHERE Id = @Id;";

            const string updateProfileSql = @"
                UPDATE UserProfile
                SET Bio = @Bio,
                    WebsiteUrl = @WebsiteUrl,
                    SocialLinks = @SocialLinks
                WHERE UserId = @UserId;";

            using IDbConnection? connection = _connectionFactory.CreateConnection();

            try
            {
                // Update the User table
                await connection.ExecuteAsync(updateUserSql, user);

                // Update the UserProfile table if a profile is provided
                if (user?.Profile is not null)
                {
                    // Ensure UserId matches the Users table's Id
                    user.Profile.UserId = user.Id;

                    // Ensure SocialLinks is valid JSON
                    string? serializedSocialLinks = JsonSerializer.Serialize(
                        JsonSerializer.Deserialize<Dictionary<string, string>>(user.Profile.SocialLinks ?? "{}")
                    );

                    // Update profile with serialized SocialLinks
                    int rowsUpdated = await connection.ExecuteAsync(updateProfileSql, new UserProfile
                    {
                        UserId = user!.Profile.UserId,
                        Bio = user.Profile.Bio,
                        WebsiteUrl = user.Profile.WebsiteUrl,
                        SocialLinks = serializedSocialLinks
                    });

                    // Console.WriteLine($"Rows updated in UserProfile: {rowsUpdated}");
                    return rowsUpdated;
                }

                // If no profile exists, return 0 to indicate no profile was updated
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user and profile: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        // Delete a user and their profile
        public async Task<int> DeleteUserWithProfileAsync(int userId)
        {
            // const string deleteProfileSql = "DELETE FROM UserProfile WHERE UserId = @UserId;";
            const string deleteUserSql = "DELETE FROM Users WHERE Id = @Id;";

            using IDbConnection? connection = _connectionFactory.CreateConnection();
            try
            {
                // Delete Profile
                //await connection.ExecuteAsync(deleteProfileSql, new { UserId = userId }, transaction);

                // Delete User (cascades to UserProfile)
                return await connection.ExecuteAsync(deleteUserSql, new { Id = userId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user and profile: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
    }
}

