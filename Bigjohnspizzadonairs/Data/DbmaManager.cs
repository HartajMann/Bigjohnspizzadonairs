using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace Bigjohnspizzadonairs.Data
{
	public class DbmaManager
	{
		string connString;
		SqlConnection sqlConnection;

		// Constructor to initialize the variables and establish the database connection
		public DbmaManager()
		{
			connString = @"Data Source=HARRY-PC\SQLEXPRESS;Initial Catalog=Bigjohnspizzadonairs;Integrated Security=True;Encrypt=False;TrustServerCertificate=True";
			sqlConnection = new SqlConnection(connString);
			sqlConnection.Open();
		}

        public async Task<bool> ValidateLoginAsync(string userId, string password)
        {
            using (SqlCommand command = new SqlCommand("SELECT Password FROM Login WHERE UserId = @UserId", sqlConnection))
            {
                command.Parameters.AddWithValue("@UserId", userId);

                // Open the connection just before executing the command
                if (sqlConnection.State != System.Data.ConnectionState.Open)
                    await sqlConnection.OpenAsync();

                object result = await command.ExecuteScalarAsync();

                // Close the connection after executing the command
                await sqlConnection.CloseAsync();

                if (result != null && result != DBNull.Value)
                {
                    string storedPassword = (string)result;
                    return storedPassword == password;
                }
                else
                {
                    return false;
                }
            }
        }




    }
}
