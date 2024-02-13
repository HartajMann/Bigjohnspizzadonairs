using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.AspNetCore.Components;

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
        public async Task<List<AvailabilityModel>> GetAvailabilityAsync(int employeeId)
        {
            var availabilities = new List<AvailabilityModel>();

            using (var sqlConnection = new SqlConnection(connString))
            {
                await sqlConnection.OpenAsync();
                // Adjust the query to join with the Employees table to fetch the Name
                var command = new SqlCommand(@"
            SELECT ea.*, e.Name 
            FROM EmployeeAvailability ea 
            INNER JOIN Employees e ON ea.EmployeeId = e.EmployeeId 
            WHERE ea.EmployeeId = @EmployeeId", sqlConnection);
                command.Parameters.AddWithValue("@EmployeeId", employeeId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        availabilities.Add(new AvailabilityModel
                        {
                            Id = (int)reader["Id"],
                            EmployeeId = (int)reader["EmployeeId"],
                            Name = reader["Name"].ToString(), // Get the Name from the reader
                            DayOfWeek = (DayOfWeek)reader["DayOfWeek"],
                            StartTime = reader["StartTime"] as TimeSpan?,
                            EndTime = reader["EndTime"] as TimeSpan?
                        });
                    }
                }
            }
            return availabilities;
        }


        // Method to update the availability of an employee
        public async Task<bool> UpdateAvailabilityAsync(int employeeId, List<AvailabilityModel> availabilities)
        {
            using (var sqlConnection = new SqlConnection(connString))
            {
                await sqlConnection.OpenAsync();
                using (var transaction = sqlConnection.BeginTransaction())
                {
                    try
                    {
                        var deleteCommandText = "DELETE FROM EmployeeAvailability WHERE EmployeeId = @EmployeeId";
                        var deleteCommand = new SqlCommand(deleteCommandText, sqlConnection, transaction);
                        deleteCommand.Parameters.AddWithValue("@EmployeeId", employeeId);
                        await deleteCommand.ExecuteNonQueryAsync();

                        var insertCommandText = @"
                    INSERT INTO EmployeeAvailability (EmployeeId, DayOfWeek, StartTime, EndTime)
                    VALUES (@EmployeeId, @DayOfWeek, @StartTime, @EndTime);
                ";

                        foreach (var availability in availabilities)
                        {
                            var insertCommand = new SqlCommand(insertCommandText, sqlConnection, transaction);
                            insertCommand.Parameters.AddWithValue("@EmployeeId", employeeId);
                            insertCommand.Parameters.AddWithValue("@DayOfWeek", (int)availability.DayOfWeek);
                            insertCommand.Parameters.AddWithValue("@StartTime", (object)availability.StartTime ?? DBNull.Value);
                            insertCommand.Parameters.AddWithValue("@EndTime", (object)availability.EndTime ?? DBNull.Value);
                            await insertCommand.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }
        public async Task<List<AvailabilityModel>> GetAvailableEmployeesAsync(DateTime scheduleDate)
        {
            var dayOfWeek = scheduleDate.DayOfWeek;
            var availableEmployees = new List<AvailabilityModel>();
            using (var sqlConnection = new SqlConnection(connString))
            {
                try
                {
                    await sqlConnection.OpenAsync();
                    var commandText = "SELECT ea.EmployeeId, ea.DayOfWeek, ea.StartTime, ea.EndTime, e.Name " +
                                      "FROM EmployeeAvailability ea " +
                                      "INNER JOIN Employees e ON ea.EmployeeId = e.EmployeeId " +
                                      "WHERE ea.DayOfWeek = @DayOfWeek";
                    var command = new SqlCommand(commandText, sqlConnection);
                    command.Parameters.AddWithValue("@DayOfWeek", (int)dayOfWeek);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var availability = new AvailabilityModel
                            {
                                EmployeeId = (int)reader["EmployeeId"],
                                DayOfWeek = (DayOfWeek)reader["DayOfWeek"],
                                StartTime = reader["StartTime"] as TimeSpan?,
                                EndTime = reader["EndTime"] as TimeSpan?,
                                Name = reader["Name"].ToString()
                            };
                            availableEmployees.Add(availability);
                            Debug.WriteLine($"Fetched availability for employee {availability.Name} ({availability.EmployeeId}) on day {availability.DayOfWeek}");
                        }
                    }

                    if (!availableEmployees.Any())
                    {
                        // Add logging here if no employees are fetched
                        Debug.WriteLine("No available employees were fetched for the given day of the week.");
                    }
                }
                catch (Exception ex)
                {
                    // Add logging here to catch any exceptions
                    Debug.WriteLine($"An error occurred while fetching available employees: {ex.Message}");
                }
            }
            return availableEmployees;
        }

        public async Task<bool> SaveScheduleAsync(List<ScheduleModel> schedules)
        {
            using (var sqlConnection = new SqlConnection(connString))
            {
                await sqlConnection.OpenAsync();
                using (var transaction = sqlConnection.BeginTransaction())
                {
                    try
                    {
                        foreach (var schedule in schedules)
                        {
                            var commandText = @"
                        INSERT INTO EmployeeShifts (EmployeeId, ShiftDate, StartTime, EndTime)
                        VALUES (@EmployeeId, @ShiftDate, @StartTime, @EndTime);
                    ";
                            var command = new SqlCommand(commandText, sqlConnection, transaction);
                            command.Parameters.AddWithValue("@EmployeeId", schedule.EmployeeId);
                            command.Parameters.AddWithValue("@ShiftDate", schedule.ScheduleDate);
                            command.Parameters.AddWithValue("@StartTime", schedule.StartTime.TimeOfDay);
                            command.Parameters.AddWithValue("@EndTime", schedule.EndTime.TimeOfDay);

                            await command.ExecuteNonQueryAsync();
                        }
                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        public async Task<List<ScheduleDisplayModel>> GetSchedulesForDate(DateTime date)
        {
            var schedules = new List<ScheduleDisplayModel>();
            using (var connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(@"SELECT es.ShiftId, e.Name AS EmployeeName, es.StartTime, es.EndTime
                                       FROM EmployeeShifts es
                                       JOIN Employees e ON es.EmployeeId = e.EmployeeId
                                       WHERE es.ShiftDate = @Date", connection);
                command.Parameters.AddWithValue("@Date", date);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var schedule = new ScheduleDisplayModel
                        {
                            ScheduleId = reader.GetInt32(reader.GetOrdinal("ShiftId")),
                            EmployeeName = reader.GetString(reader.GetOrdinal("EmployeeName")),
                            StartTime = reader.GetTimeSpan(reader.GetOrdinal("StartTime")),
                            EndTime = reader.GetTimeSpan(reader.GetOrdinal("EndTime")),
                        };
                        schedules.Add(schedule);
                    }
                }
            }
            return schedules;
        }



        public async Task<bool> UpdateScheduleAsync(int scheduleId, TimeSpan startTime, TimeSpan endTime)
        {
            using (var connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("UPDATE EmployeeShifts SET StartTime = @StartTime, EndTime = @EndTime WHERE ShiftId = @ShiftId", connection);
                command.Parameters.AddWithValue("@StartTime", startTime);
                command.Parameters.AddWithValue("@EndTime", endTime);
                command.Parameters.AddWithValue("@ShiftId", scheduleId);

                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
        }

        public async Task<bool> DeleteScheduleAsync(int scheduleId)
        {
            using (var connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("DELETE FROM EmployeeShifts WHERE ShiftId = @ShiftId", connection);
                command.Parameters.AddWithValue("@ShiftId", scheduleId);

                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
        }
        // Method for fetching the schedule for a specific employee

        public async Task<List<ScheduledEmployeeModel>> GetWeeklyScheduleForEmployee(int employeeId)
        {
            var schedules = new List<ScheduledEmployeeModel>();
            var startDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday); // This week's Monday
            var endDate = startDate.AddDays(6); // This week's Sunday

            try
            {
                using (var connection = new SqlConnection(connString))
                {
                    await connection.OpenAsync();
                    var query = @"
                SELECT EmployeeId, ShiftDate, CONVERT(time, StartTime) AS StartTime, CONVERT(time, EndTime) AS EndTime
                FROM EmployeeShifts
                WHERE EmployeeId = @EmployeeId AND ShiftDate BETWEEN @StartDate AND @EndDate
                ORDER BY ShiftDate, StartTime";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@EmployeeId", employeeId);
                        command.Parameters.AddWithValue("@StartDate", startDate);
                        command.Parameters.AddWithValue("@EndDate", endDate);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                schedules.Add(new ScheduledEmployeeModel
                                {
                                    EmployeeId = (int)reader["EmployeeId"],
                                    ShiftDate = (DateTime)reader["ShiftDate"],
                                    StartTime = reader["StartTime"] is TimeSpan ? (TimeSpan)reader["StartTime"] : TimeSpan.Zero,
                                    EndTime = reader["EndTime"] is TimeSpan ? (TimeSpan)reader["EndTime"] : TimeSpan.Zero
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Debug.WriteLine($"SQL Error: {ex.Message}");
                // Consider logging the exception and/or rethrowing it after logging
            }

            return schedules;
        }
    }



}