using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace VerySimple
{
    public class HealthChecker
    {
        public static bool Check(IConfiguration configuration)
        {
            var serverName = configuration["MYSQLSERVERNAME"];
            var connectionString = $"Server={serverName};Database=sessionstate;Username=sessionStateUser;Password=aaabbb;SslMode=None";

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    
                    connection.Close();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}