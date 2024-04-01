using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using System.Data;

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


        public async Task<(bool IsValidLogin, bool IsEmployee, bool IsManager)> ValidateLoginAsync(string userId, string password)
        {
            bool isValidLogin = false;
            bool isEmployee = false;
            bool isManager = false;

            using (var connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();

                // First, check in the Login table (assuming managers are stored here)
                var loginQuery = "SELECT Password FROM Login WHERE UserId = @UserId";
                using (var loginCommand = new SqlCommand(loginQuery, connection))
                {
                    loginCommand.Parameters.AddWithValue("@UserId", userId);
                    var result = await loginCommand.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value && (string)result == password)
                    {
                        isValidLogin = true;
                        isManager = true; // Validated as a manager
                    }
                }

                // If not found in Login, check in the Employees table
                if (!isValidLogin)
                {
                    var employeeQuery = "SELECT Password FROM Employees WHERE Name = @Name";
                    using (var employeeCommand = new SqlCommand(employeeQuery, connection))
                    {
                        employeeCommand.Parameters.AddWithValue("@Name", userId);
                        var result = await employeeCommand.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value && (string)result == password)
                        {
                            isValidLogin = true;
                            isEmployee = true; // Validated as an employee
                        }
                    }
                }
            }

            return (isValidLogin, isEmployee, isManager);
        }
        public async Task<string> GetUserRoleAsync(string Name)
        {
            string role = "Employee"; // Default role
            using (var connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                var roleQuery = "SELECT Position FROM Employees WHERE Name = @Name";
                using (var roleCommand = new SqlCommand(roleQuery, connection))
                {
                    roleCommand.Parameters.Add(new SqlParameter("@Name", SqlDbType.VarChar)).Value = Name; // More explicit parameter declaration
                    var result = await roleCommand.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        role = result.ToString();
                    }
                }
            }
            Debug.WriteLine($"Role for employee {Name} is {role}");
            return role;
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

        public async Task<bool> DeleteEmployeeAsync(int EmployeeId)
        {
            using (var sqlConnection = new SqlConnection(connString))
            {
                await sqlConnection.OpenAsync();
                using (var transaction = sqlConnection.BeginTransaction())
                {
                    try
                    {
                        // Delete referencing rows from EmployeeAvailability
                        var deleteAvailabilityCommand = new SqlCommand("DELETE FROM EmployeeAvailability WHERE EmployeeId = @EmployeeId", sqlConnection, transaction);
                        deleteAvailabilityCommand.Parameters.AddWithValue("@EmployeeId", EmployeeId);
                        await deleteAvailabilityCommand.ExecuteNonQueryAsync();

                        // Delete referencing rows from EmployeeShifts
                        var deleteShiftsCommand = new SqlCommand("DELETE FROM EmployeeShifts WHERE EmployeeId = @EmployeeId", sqlConnection, transaction);
                        deleteShiftsCommand.Parameters.AddWithValue("@EmployeeId", EmployeeId);
                        await deleteShiftsCommand.ExecuteNonQueryAsync();

                        // Finally, delete the employee
                        var deleteEmployeeCommand = new SqlCommand("DELETE FROM Employees WHERE EmployeeId = @EmployeeId", sqlConnection, transaction);
                        deleteEmployeeCommand.Parameters.AddWithValue("@EmployeeId", EmployeeId);
                        int result = await deleteEmployeeCommand.ExecuteNonQueryAsync();

                        transaction.Commit();

                        return result > 0;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error deleting employee: " + ex.Message);
                        transaction.Rollback();
                        return false;
                    }
                }
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
        public async Task<EmployeeModel> GetEmployeeById(int employeeId)
        {
            EmployeeModel employee = null;

            using (var connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                var query = "SELECT EmployeeId, Name, Email FROM Employees WHERE EmployeeId = @EmployeeId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EmployeeId", employeeId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            employee = new EmployeeModel
                            {
                                EmployeeId = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Email = reader.GetString(2)
                            };
                        }
                    }
                }
            }

            return employee;
        }

        public async Task<bool> AddInventoryItemAsync(string branch, string Name, string description, int quantity, int alertQuantity, DateTime? expiryDate)
        {
            try
            {
                using (var connection = new SqlConnection(connString))
                {
                    await connection.OpenAsync();
                    var query = @"
                INSERT INTO InventoryItems (Branch, Name, Description, Quantity, AlertQuantity, ExpiryDate)
                VALUES (@Branch, @Name, @Description, @Quantity, @AlertQuantity, @ExpiryDate)";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Branch", branch);
                        command.Parameters.AddWithValue("@Name", Name);
                        command.Parameters.AddWithValue("@Description", description);
                        command.Parameters.AddWithValue("@Quantity", quantity);
                        command.Parameters.AddWithValue("@AlertQuantity", alertQuantity);
                        command.Parameters.AddWithValue("@ExpiryDate", (object)expiryDate ?? DBNull.Value);

                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<List<InventoryItemModel>> GetInventoryItemsAsync()
        {
            var inventoryItems = new List<InventoryItemModel>();

            try
            {
                using (var connection = new SqlConnection(connString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT InventoryId, Branch, Name, Description, Quantity, ExpiryDate FROM InventoryItems";
                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                inventoryItems.Add(new InventoryItemModel
                                {
                                    InventoryId = (int)reader["InventoryId"],
                                    Branch = reader["Branch"].ToString(),
                                    Name = reader["Name"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    Quantity = (int)reader["Quantity"],
                                    ExpiryDate = reader["ExpiryDate"] as DateTime?
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return inventoryItems;
        }
        public async Task<bool> DeleteInventoryItemAsync(int inventoryId)
        {
            try
            {
                using (var connection = new SqlConnection(connString))
                {
                    await connection.OpenAsync();
                    var query = "DELETE FROM InventoryItems WHERE InventoryId = @InventoryId";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@InventoryId", inventoryId);
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        public async Task<InventoryItemModel> GetInventoryItemAsync(int inventoryId)
        {
            InventoryItemModel item = null;
            using (var connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                var query = "SELECT * FROM InventoryItems WHERE InventoryId = @InventoryId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@InventoryId", inventoryId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            item = new InventoryItemModel
                            {
                                InventoryId = (int)reader["InventoryId"],
                                Branch = reader["Branch"].ToString(),
                                Name = reader["Name"].ToString(),
                                Description = reader["Description"].ToString(),
                                Quantity = (int)reader["Quantity"],
                                ExpiryDate = reader["ExpiryDate"] as DateTime?
                            };
                        }
                    }
                }
            }
            return item;
        }

        public async Task<bool> UpdateInventoryItemAsync(InventoryItemModel item)
        {
            try
            {
                using (var connection = new SqlConnection(connString))
                {
                    await connection.OpenAsync();
                    var query = @"
                UPDATE InventoryItems 
                SET Branch = @Branch, 
                    Name = @Name, 
                    Description = @Description, 
                    Quantity = @Quantity, 
                    ExpiryDate = @ExpiryDate 
                WHERE InventoryId = @InventoryId";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@InventoryId", item.InventoryId);
                        command.Parameters.AddWithValue("@Branch", item.Branch);
                        command.Parameters.AddWithValue("@Name", item.Name);
                        command.Parameters.AddWithValue("@Description", item.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Quantity", item.Quantity);
                        command.Parameters.AddWithValue("@ExpiryDate", item.ExpiryDate ?? (object)DBNull.Value);
                        var result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        public List<InventoryItemModel> GetInventoryAlerts()
        {
            var alerts = new List<InventoryItemModel>();

            using (var connection = new SqlConnection(connString))
            {
                connection.Open();

                var command = new SqlCommand(@"
SELECT * FROM [InventoryItems] 
WHERE 
    Quantity <= AlertQuantity OR 
    (ExpiryDate IS NOT NULL AND ExpiryDate <= @expiryThreshold)", connection);

                command.Parameters.AddWithValue("@expiryThreshold", DateTime.Now.AddDays(7));

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new InventoryItemModel
                        {
                            InventoryId = (int)reader["InventoryId"],
                            Branch = reader["Branch"].ToString(),
                            Name = reader["Name"].ToString(),
                            Description = reader["Description"].ToString(),
                            Quantity = (int)reader["Quantity"],
                            ExpiryDate = reader["ExpiryDate"] as DateTime?,
                            Timestamp = (DateTime)reader["Timestamp"],
                            Alert = (int)reader["AlertQuantity"] // Ensure this matches the column name in your table
                        };
                        alerts.Add(item);
                    }
                }
            }

            return alerts;
        }
        public async Task<List<ShiftDetail>> GetTodaysShiftsAsync()
        {
            var todaysShifts = new List<ShiftDetail>();
            var currentDate = DateTime.Now.Date;

            using (var connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();

                var shiftQuery = @"
        SELECT es.ShiftId, es.EmployeeId, es.ShiftDate, es.StartTime, es.EndTime, e.Name
        FROM EmployeeShifts es
        INNER JOIN Employees e ON es.EmployeeId = e.EmployeeId
        WHERE es.ShiftDate = @ShiftDate";

                using (var command = new SqlCommand(shiftQuery, connection))
                {
                    command.Parameters.AddWithValue("@ShiftDate", currentDate);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var shift = new ShiftDetail
                            {
                                ShiftId = reader.GetInt32(reader.GetOrdinal("ShiftId")),
                                EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeId")),
                                ShiftDate = reader.GetDateTime(reader.GetOrdinal("ShiftDate")),
                                StartTime = reader.GetTimeSpan(reader.GetOrdinal("StartTime")),
                                EndTime = reader.GetTimeSpan(reader.GetOrdinal("EndTime")),
                                EmployeeName = reader.GetString(reader.GetOrdinal("Name"))
                            };
                            todaysShifts.Add(shift);
                        }
                    }
                }
            }

            return todaysShifts;
        }

		public async Task<bool> ValidateManagerLoginAsync(string userId, string password)
		{
			bool isValidLogin = false;

			using (var connection = new SqlConnection(connString))
			{
				await connection.OpenAsync();

				var loginQuery = "SELECT Password FROM Login WHERE UserId = @UserId";
				using (var loginCommand = new SqlCommand(loginQuery, connection))
				{
					loginCommand.Parameters.AddWithValue("@UserId", userId);
					var result = await loginCommand.ExecuteScalarAsync();
					if (result != null && result != DBNull.Value && (string)result == password)
					{
						isValidLogin = true;
					}
				}
			}
            Debug.WriteLine($"Manager login validation for {userId} is {isValidLogin}");
			return (isValidLogin);
		}

        public async Task<List<EmployeeShiftSummary>> GetShiftSummariesByMonthAsync(int year, int month)
        {
            var summaries = new List<EmployeeShiftSummary>();

            using (var connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                var query = @"
        SELECT e.Name, tt.UserId, SUM(tt.MinutesWorked) AS TotalMinutesWorked
        FROM TimeTracking tt
        JOIN Employees e ON tt.UserId = e.EmployeeId
        WHERE YEAR(tt.PunchIn) = @Year AND MONTH(tt.PunchIn) = @Month
        GROUP BY tt.UserId, e.Name";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Year", year);
                    command.Parameters.AddWithValue("@Month", month);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            summaries.Add(new EmployeeShiftSummary
                            {
                                Name = reader.GetString(0), // Adjusted index for Name
                                UserId = reader.GetString(1),
                                TotalMinutesWorked = reader.GetInt32(2) // Adjusted index for TotalMinutesWorked
                            });
                        }
                    }
                }
            }

            return summaries;
        }
        public async Task<List<ShiftDetail>> GetShiftDetailsForEmployee(string userId)
        {
            var shiftDetails = new List<ShiftDetail>();
            using (var connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                var query = @"
            SELECT t.TrackingId, t.UserId, e.Name, t.PunchIn, t.PunchOut, t.MinutesWorked
            FROM TimeTracking t
            JOIN Employees e ON t.UserId = e.EmployeeId
            WHERE t.UserId = @UserId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var punchIn = (DateTime)reader["PunchIn"];
                            var punchOut = (DateTime)reader["PunchOut"];
                            shiftDetails.Add(new ShiftDetail
                            {
                                ShiftId = (int)reader["TrackingId"],
                                UserId = reader["UserId"].ToString(),
                                EmployeeName = reader["Name"].ToString(),
                                ShiftDate = punchIn.Date,
                                StartTime = punchIn.TimeOfDay,
                                EndTime = punchOut.TimeOfDay,
                                TotalMinutesWorked = (int)reader["MinutesWorked"]
                            });
                        }
                    }
                }
            }
            return shiftDetails;
        }

        public async Task<bool> UpdateShiftTimeAsync(int shiftId, TimeSpan newStartTime, TimeSpan newEndTime)
        {
            using (var connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();

                // Retrieve the current PunchIn time to use its date part
                var getDateQuery = "SELECT PunchIn FROM TimeTracking WHERE TrackingId = @ShiftId";
                DateTime currentPunchInDate;
                using (var getDateCommand = new SqlCommand(getDateQuery, connection))
                {
                    getDateCommand.Parameters.AddWithValue("@ShiftId", shiftId);
                    currentPunchInDate = (DateTime)await getDateCommand.ExecuteScalarAsync();
                }

                var newPunchIn = currentPunchInDate.Date + newStartTime;
                var newPunchOut = currentPunchInDate.Date + newEndTime;

                var query = @"
            UPDATE TimeTracking
            SET PunchIn = @NewPunchIn, PunchOut = @NewPunchOut
            WHERE TrackingId = @ShiftId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ShiftId", shiftId);
                    command.Parameters.AddWithValue("@NewPunchIn", newPunchIn);
                    command.Parameters.AddWithValue("@NewPunchOut", newPunchOut);

                    var result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
        }


    }

}