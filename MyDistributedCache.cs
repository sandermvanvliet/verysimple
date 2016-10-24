namespace VerySimple
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Caching.Distributed;
    using MySql.Data.MySqlClient;

    public class MyDistributedCache : IDistributedCache
    {
        private static readonly Task CompletedTask = Task.FromResult<object>(null);
        private MySqlConnection _connection;

        public MyDistributedCache()
        {
            _connection = new MySqlConnection();
        }

        public byte[] Get(string key)
        {
            using (var command = new MySqlCommand("SELECT value, expiry_date, is_slidingexpiration FROM data WHERE key = @key", _connection))
            {
                command.Parameters.Add("key", MySqlDbType.VarChar, 32);
                command.Parameters["key"].Value = key;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var data = reader.GetStream(0).GetBuffer();
                        var expiryDate = reader.GetDateTime(1);
                        var isSlidingExpiration = reader.GetBoolean(2);

                        if (expiryDate > DateTime.UtcNow)
                        {
                            Remove(key);
                            return null;
                        }

                        if (isSlidingExpiration)
                        {
                            Refresh(key);
                        }

                        return data;
                    }
                }
            }

            return null;
        }

        public void Refresh(string key)
        {
            using (var command = new MySqlCommand("UPDATE data SET expiry_date = expiry_date + lifetime WHERE key = @key", _connection))
            {
                command.Parameters.Add("key", MySqlDbType.VarChar);
                command.Parameters["key"].Value = key;

                command.ExecuteNonQuery();
            }
        }

        public void Remove(string key)
        {
            using (var command = new MySqlCommand("DELETE FROM data WHERE key = @key", _connection))
            {
                command.Parameters.Add("key", MySqlDbType.VarChar);
                command.Parameters["key"].Value = key;

                command.ExecuteNonQuery();
            }
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            using (var command = new MySqlCommand("INSERT INTO data(key, expiry_date, lifetime, is_slidingexpiration, value) VALUES(@key, @expiry_date, @lifetime, @is_slidingexpiration, @value", _connection))
            {
                command.Parameters.Add("key", MySqlDbType.VarChar);
                command.Parameters.Add("expiry_date", MySqlDbType.DateTime);
                command.Parameters.Add("lifetime", MySqlDbType.Time);
                command.Parameters.Add("is_slidingexpiration", MySqlDbType.Bit);
                command.Parameters.Add("value", MySqlDbType.Blob);

                command.Parameters["key"].Value = key;
                command.Parameters["expiry_date"].Value = options.AbsoluteExpiration;
                command.Parameters["lifetime"].Value = options.SlidingExpiration.GetValueOrDefault();
                command.Parameters["is_slidingexpiration"].Value = options.SlidingExpiration.HasValue;
                command.Parameters["value"].Value = value;

                command.ExecuteNonQuery();
            }
        }

        public Task<byte[]> GetAsync(string key)
        {
            return Task.FromResult(Get(key));
        }

        public Task RefreshAsync(string key)
        {
            Refresh(key);
            return CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            Remove(key);
            return CompletedTask;
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Set(key, value, options);
            return CompletedTask;
        }
    }
}