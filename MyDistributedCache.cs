namespace VerySimple
{
    using System;
    using System.Threading.Tasks;
    using Dapper;
    using Microsoft.Extensions.Caching.Distributed;
    using MySql.Data.MySqlClient;

    public class MyDistributedCache : IDistributedCache
    {
        private MySqlConnection _connection;

        public MyDistributedCache(string connectionString)
        {
            _connection = new MySqlConnection(connectionString);
        }

        public byte[] Get(string key)
        {
            Session session;

            try
            {
                 session = _connection
                    .QuerySingleOrDefault<Session>(
                        "SELECT * FROM `sessions` WHERE sessionid = @sessionid",
                        new { sessionid = key });
            }
            catch (Exception)
            {
                session = null;
            }

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
            try
            {
                _connection
                    .Execute(
                        "UPDATE `sessions` SET expirydate = ADDTIME(expirydate, lifetime) WHERE `sessionid` = @sessionid",
                        new { sessionid = key });
            }
            catch (Exception)
            {
            }
        }

        public void Remove(string key)
        {
            try
            {
                _connection
                    .Execute(
                        "DELETE FROM `sessions` WHERE `sessionid` = @sessionid",
                        new { sessionid = key });
            }
            catch (Exception)
            {
            }
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Session session = null;

            try
            {
                session = _connection
                    .QuerySingleOrDefault<Session>(
                        "SELECT * FROM `sessions` WHERE sessionid = @sessionid",
                        new { sessionid = key });
            }
            catch (Exception)
            {
            }

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

            try
            {
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
            catch(Exception)
            {
            }
        }

        public Task<byte[]> GetAsync(string key)
        {
            return Task.FromResult(Get(key));
        }

        public Task RefreshAsync(string key)
        {
            Refresh(key);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }
}