namespace VerySimple
{
    using System;
    using System.Threading.Tasks;
    using Dapper;
    using Microsoft.Extensions.Caching.Distributed;
    using MySql.Data.MySqlClient;

    public class MyDistributedCache : IDistributedCache
    {
        private static readonly Task CompletedTask = Task.FromResult<object>(null);
        private MySqlConnection _connection;

        public MyDistributedCache(string connectionString)
        {
            _connection = new MySqlConnection(connectionString);
            _connection.Open();
        }

        public byte[] Get(string key)
        {
            var session = _connection
                .QuerySingleOrDefault<Session>(
                    "SELECT * FROM `sessions` WHERE sessionid = @sessionid",
                    new { sessionid = key });

            if (session != null)
            {
                if (session.ExpiryDate < DateTime.UtcNow)
                {
                    Remove(key);
                    return null;
                }

                if (session.IsSlidingExpiry)
                {
                    Refresh(key);
                }

                return session.Value;
            }

            return null;
        }

        public void Refresh(string key)
        {
            _connection
                .Execute(
                    "UPDATE `sessions` SET expirydate = ADDTIME(expirydate, lifetime) WHERE `sessionid` = @sessionid",
                    new { sessionid = key });
        }

        public void Remove(string key)
        {
            _connection
                .Execute(
                    "DELETE FROM `sessions` WHERE `sessionid` = @sessionid",
                    new { sessionid = key });
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            var session = _connection
                .QuerySingleOrDefault<Session>(
                    "SELECT * FROM `sessions` WHERE sessionid = @sessionid",
                    new { sessionid = key });

            var insertNew = session == null;

            string commandText;

            if (insertNew)
            {
                commandText = @"INSERT INTO `sessions`(`sessionid`, value, size, expirydate, lifetime, isslidingexpiry) 
                                VALUES(@sessionid, @value, @size, @expirydate, @lifetime, @isslidingexpiry)";
            }
            else
            {
                commandText = @"UPDATE `sessions` 
                            SET `value` = @value, `size` = @size,
                                `expirydate` = @expirydate, `lifetime` = @lifetime, `isslidingexpiry` = @isslidingexpiry 
                            WHERE `sessionid` = @sessionid";
            }

            var expiryDate = options.SlidingExpiration.HasValue ? DateTime.UtcNow.Add(options.SlidingExpiration.Value) : options.AbsoluteExpiration.Value;

            _connection
                .Execute(commandText,
                new
                {
                    sessionid = key,
                    size = value.Length,
                    @value = value,
                    expirydate = options.AbsoluteExpiration,
                    lifetime = options.SlidingExpiration.GetValueOrDefault(),
                    isslidingexpiry = options.SlidingExpiration.HasValue
                });
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