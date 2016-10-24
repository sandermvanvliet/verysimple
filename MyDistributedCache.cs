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
            _connection = new MySqlConnection("Server=localhost;Database=sessionstate;Username=sessionStateUser;Password=aaabbb");
            _connection.Open();
        }

        public byte[] Get(string key)
        {
            using (var command = new MySqlCommand("SELECT value, size, expiry_date, is_slidingexpiry FROM `data` WHERE `key` = @key", _connection))
            {
                command.Parameters.Add("key", MySqlDbType.VarChar, 32);
                command.Parameters["key"].Value = key;

                DateTime expiryDate = DateTime.MinValue;
                bool isSlidingExpiration = false;
                byte[] data = null;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var size = reader.GetInt32(1);
                        expiryDate = reader.GetDateTime(2);
                        isSlidingExpiration = reader.GetBoolean(3);

                        if (!reader.IsDBNull(0) && size > 0)
                        {
                            data = new byte[size];
                            reader.GetBytes(0, 0, data, 0, data.Length);
                        }
                    }
                }

                if (expiryDate < DateTime.UtcNow)
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

        public void Refresh(string key)
        {
            using (var command = new MySqlCommand("UPDATE `data` SET expiry_date = ADDTIME(expiry_date, lifetime) WHERE `key` = @key", _connection))
            {
                command.Parameters.Add("key", MySqlDbType.VarChar);
                command.Parameters["key"].Value = key;

                command.ExecuteNonQuery();
            }
        }

        public void Remove(string key)
        {
            using (var command = new MySqlCommand("DELETE FROM `data` WHERE `key` = @key", _connection))
            {
                command.Parameters.Add("key", MySqlDbType.VarChar);
                command.Parameters["key"].Value = key;

                command.ExecuteNonQuery();
            }
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            var insertNew = true;

            using (var command = new MySqlCommand("SELECT 1 FROM `data` WHERE `key` = @key", _connection))
            {
                command.Parameters.Add("key", MySqlDbType.VarChar, 32);
                command.Parameters["key"].Value = key;

                var result = command.ExecuteScalar();
                if(Convert.ToInt32(result) == 1)
                {
                    insertNew = false;
                }
            }

            var commandText = insertNew
            ? "INSERT INTO `data`(`key`, expiry_date, lifetime, is_slidingexpiry, `value`, `size`) VALUES(@key, @expiry_date, @lifetime, @is_slidingexpiration, @value, @size)"
            : "UPDATE `data` SET `value` = @value, `size` = @size, `expiry_date` = @expiry_date, `lifetime` = @lifetime, `is_slidingexpiry` = @is_slidingexpiration WHERE `key` = @key";

            using (var command = new MySqlCommand(commandText, _connection))
            {
                command.Parameters.Add("key", MySqlDbType.VarChar);
                command.Parameters.Add("expiry_date", MySqlDbType.DateTime);
                command.Parameters.Add("lifetime", MySqlDbType.Time);
                command.Parameters.Add("is_slidingexpiration", MySqlDbType.Bit);
                command.Parameters.Add("value", MySqlDbType.Blob, value.Length);
                command.Parameters.Add("size", MySqlDbType.Int32);

                var expiryDate = options.SlidingExpiration.HasValue ? DateTime.UtcNow.Add(options.SlidingExpiration.Value) : options.AbsoluteExpiration.Value;

                command.Parameters["key"].Value = key;
                command.Parameters["expiry_date"].Value = expiryDate.ToString("yyyy-MM-dd HH:mm:ss");
                command.Parameters["lifetime"].Value = options.SlidingExpiration.GetValueOrDefault();
                command.Parameters["is_slidingexpiration"].Value = options.SlidingExpiration.HasValue;
                command.Parameters["value"].Value = value;
                command.Parameters["size"].Value = value.Length;

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