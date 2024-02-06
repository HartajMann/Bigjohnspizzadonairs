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
        public async Task<bool> VerifyPasswordAsync(string userId, string password)
        {
            using (var connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT Password FROM Employees WHERE EmployeeId = @UserId", connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var storedPassword = (string)await command.ExecuteScalarAsync();
                return storedPassword == password;
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
        public async Task<bool> AddEmployeeAsync(EmployeeModel employee)
        {
            using (var sqlConnection = new SqlConnection(connString))
            {
                await sqlConnection.OpenAsync();

                var command = new SqlCommand(@"
            INSERT INTO Employees (
                Name, Email, Age, Position, ContactNumber, EmergencyContactNumber, Password
            ) VALUES (
                @Name, @Email, @Age, @Position, @ContactNumber, @EmergencyContactNumber, @Password
            )", sqlConnection);

                command.Parameters.AddWithValue("@Name", employee.Name);
                command.Parameters.AddWithValue("@Email", employee.Email);
                command.Parameters.AddWithValue("@Age", employee.Age);
                command.Parameters.AddWithValue("@Position", employee.Position);
                command.Parameters.AddWithValue("@ContactNumber", employee.ContactNumber);
                command.Parameters.AddWithValue("@EmergencyContactNumber", employee.EmergencyContactNumber);
                command.Parameters.AddWithValue("@Password", employee.Password); 

                int result = await command.ExecuteNonQueryAsync();

                await sqlConnection.CloseAsync();

                return result > 0;
            }
        }
        public async Task<List<EmployeeModel>> GetAllEmployeesAsync()
        {
            var employees = new List<EmployeeModel>();
            using (var sqlConnection = new SqlConnection(connString))
            {
                await sqlConnection.OpenAsync();
                var command = new SqlCommand("SELECT * FROM Employees", sqlConnection);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        employees.Add(new EmployeeModel
                        {
                            EmployeeId = (int)reader["EmployeeId"],
                            Name = reader["Name"].ToString(),
                            Email = reader["Email"].ToString(),
                            Age = (int)reader["Age"],
                            Position = reader["Position"].ToString(),
                            ContactNumber = reader["ContactNumber"].ToString(),
                            EmergencyContactNumber = reader["EmergencyContactNumber"].ToString(),
                            // Do not retrieve the password
                        });
                    }
                }
            }
            return employees;
        }

        public async Task<bool> DeleteEmployeeAsync(int employeeId)
        {
            using (var sqlConnection = new SqlConnection(connString))
            {
                await sqlConnection.OpenAsync();
                var command = new SqlCommand("DELETE FROM Employees WHERE EmployeeId = @EmployeeId", sqlConnection);
                command.Parameters.AddWithValue("@EmployeeId", employeeId);

                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
        }

        public async Task<EmployeeModel> GetEmployeeAsync(int employeeId)
        {
            using (var sqlConnection = new SqlConnection(connString))
            {
                await sqlConnection.OpenAsync();
                var command = new SqlCommand("SELECT * FROM Employees WHERE EmployeeId = @EmployeeId", sqlConnection);
                command.Parameters.AddWithValue("@EmployeeId", employeeId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new EmployeeModel
                        {
                            EmployeeId = (int)reader["EmployeeId"],
                            Name = reader["Name"].ToString(),
                            Email = reader["Email"].ToString(),
                            Age = (int)reader["Age"],
                            Position = reader["Position"].ToString(),
                            ContactNumber = reader["ContactNumber"].ToString(),
                            EmergencyContactNumber = reader["EmergencyContactNumber"].ToString(),
                            // Password is not directly fetched for security reasons
                        };
                    }
                }
            }
            return null;
        }

        public async Task<bool> UpdateEmployeeAsync(EmployeeModel employee, string newPassword = null)
        {
            using (var sqlConnection = new SqlConnection(connString))
            {
                await sqlConnection.OpenAsync();
                var commandText = @"UPDATE Employees SET 
            Name = @Name, Email = @Email, Age = @Age, Position = @Position, 
            ContactNumber = @ContactNumber, EmergencyContactNumber = @EmergencyContactNumber
            {0}
            WHERE EmployeeId = @EmployeeId";

                if (!string.IsNullOrEmpty(newPassword))
                {
                    commandText = string.Format(commandText, ", Password = @Password");
                }
                else
                {
                    commandText = string.Format(commandText, "");
                }

                var command = new SqlCommand(commandText, sqlConnection);

                command.Parameters.AddWithValue("@Name", employee.Name);
                command.Parameters.AddWithValue("@Email", employee.Email);
                command.Parameters.AddWithValue("@Age", employee.Age);
                command.Parameters.AddWithValue("@Position", employee.Position);
                command.Parameters.AddWithValue("@ContactNumber", employee.ContactNumber);
                command.Parameters.AddWithValue("@EmergencyContactNumber", employee.EmergencyContactNumber);
                command.Parameters.AddWithValue("@EmployeeId", employee.EmployeeId);
                if (!string.IsNullOrEmpty(newPassword))
                {
                    command.Parameters.AddWithValue("@Password", newPassword); // Remember to hash the password in future iterations
                }

                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
        }
        public async Task<bool> VerifyOldPasswordAsync(int employeeId, string oldPassword)
        {
            using (var sqlConnection = new SqlConnection(connString))
            {
                await sqlConnection.OpenAsync();
                var command = new SqlCommand("SELECT Password FROM Employees WHERE EmployeeId = @EmployeeId", sqlConnection);
                command.Parameters.AddWithValue("@EmployeeId", employeeId);

                var storedPassword = (string)await command.ExecuteScalarAsync();
                return storedPassword == oldPassword;
            }
        }

    }
}
