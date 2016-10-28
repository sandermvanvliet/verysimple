namespace VerySimple
{
    using System;
    using System.Collections.Generic;
    using Polly;
    using MySql.Data.MySqlClient;

    public class NewCache
    {
        private Dictionary<string, Session> _cache;
        private Dictionary<string, Session> _db;
        private readonly TimeSpan _breakDuration;
        private readonly Policy _dbCircuitBreaker;

        public NewCache()
        {
            _cache = new Dictionary<string, Session>();
            _db = new Dictionary<string, Session>();
            _breakDuration = TimeSpan.FromMilliseconds(150);
            _dbCircuitBreaker = Policy.Handle<MySqlException>().CircuitBreaker(0, _breakDuration);
        }

        /*
     - Get()
         - Try DB
             -> FAIL
                 -> Try cache
                     -> MISS
                         -> null
                     -> HIT
                         -> return value
             -> SUCCESS
                 -> Expired?
                     -> Remove from cache
                     -> Remove from DB  
                         -> FAIL
                             -> Nothing
                 -> Sliding?
                     -> Update DB
                         -> FAIL
                             -> Nothing


         */

        public byte[] Get(string key)
        {
            Session session = null;

            var getFromCachePolicy = Policy
                .Handle<MySqlException>()
                .Fallback(() => GetFromCache(key));

            session = getFromCachePolicy.Execute(() => GetFromDb(key));

            if (session != null)
            {
                if (IsExpired(session))
                {
                    _cache.Remove(key);
                    _dbCircuitBreaker.Execute(() => RemoveFromDb(key));
                }
                else
                {
                    if(session.IsSlidingExpiry)
                    {
                        _dbCircuitBreaker.Execute(() => RefreshInDb(key));
                    }
                }
            }

            return null;
        }

        private bool IsExpired(Session session)
        {
            return session.ExpiryDate < DateTime.UtcNow;
        }

        private Session GetFromCache(string key)
        {
            if (!_cache.ContainsKey(key))
            {
                throw new KeyNotFoundException();
            }

            return _cache[key];
        }

        private Session GetFromDb(string key)
        {
            if (!_db.ContainsKey(key))
            {
                throw new KeyNotFoundException();
            }

            return _db[key];
        }

        private void RemoveFromDb(string key) 
        {
            _db.Remove(key);
        }

        private void RefreshInDb(string key)
        {
            _db[key].ExpiryDate = _db[key].ExpiryDate.Value.Add(_db[key].Lifetime.Value);
        }
    }
}