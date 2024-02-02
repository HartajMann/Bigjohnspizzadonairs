using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace Bigjohnspizzadonairs.Data
{
    public class DbmaManager
    {
        string connString;

        // Constructor to initialize the connection string
        public DbmaManager()
        {
            connString = @"Data Source=HARRY-PC\SQLEXPRESS;Initial Catalog=Bigjohnspizzadonairs;Integrated Security=True;Encrypt=False;TrustServerCertificate=True";
        }

        public async Task<bool> ValidateLoginAsync(string userId, string password)
        {
            using (var connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                var query = "SELECT Password FROM Login WHERE UserId = @UserId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    var result = await command.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        string storedPassword = (string)result;
                        return storedPassword == password;
                    }
                }
            }
            return false;
        }

        public async Task<bool> PunchInAsync(string userId)
        {
            using (var sqlConnection = new SqlConnection(connString))
            {
                await sqlConnection.OpenAsync();

                var command = new SqlCommand("INSERT INTO TimeTracking (UserId, PunchIn) VALUES (@UserId, GETDATE())", sqlConnection);
                command.Parameters.AddWithValue("@UserId", userId);

                int result = await command.ExecuteNonQueryAsync();

                await sqlConnection.CloseAsync();

                return result > 0;
            }
        }

        public async Task<bool> PunchOutAsync(string userId)
        {
            using (var sqlConnection = new SqlConnection(connString))
            {
                await sqlConnection.OpenAsync();

                var command = new SqlCommand("UPDATE TimeTracking SET PunchOut = GETDATE() WHERE UserId = @UserId AND PunchOut IS NULL", sqlConnection);
                command.Parameters.AddWithValue("@UserId", userId);

                int result = await command.ExecuteNonQueryAsync();

                await sqlConnection.CloseAsync();

                return result > 0;
            }
        }

        public async Task<(DateTime? punchIn, DateTime? punchOut)> GetLastActivityAsync(string userId)
        {
            using (var sqlConnection = new SqlConnection(connString))
            {
                await sqlConnection.OpenAsync();

                var command = new SqlCommand("SELECT TOP 1 PunchIn, PunchOut FROM TimeTracking WHERE UserId = @UserId ORDER BY TrackingId DESC", sqlConnection);
                command.Parameters.AddWithValue("@UserId", userId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var punchIn = reader["PunchIn"] as DateTime?;
                        var punchOut = reader["PunchOut"] as DateTime?;
                        return (punchIn, punchOut);
                    }
                }

                await sqlConnection.CloseAsync();
                return (null, null);
            }
        }

    }
}
