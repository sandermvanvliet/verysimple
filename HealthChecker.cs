using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace VerySimple
{
    public class HealthChecker
    {
        public static bool Check(IConfiguration configuration)
        {
            try
            {
                using (var connection = new MySqlConnection(configuration["sessionConnectionString"]))
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