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

        public NewCache(IMySqlRepository<Session> db)
        {
            _cache = new Dictionary<string, Session>();
            _db = new Dictionary<string, Session>();
            _breakDuration = TimeSpan.FromMilliseconds(150);
            _dbCircuitBreaker = Policy
                .Handle<MySqlException>()
                .Or<KeyNotFoundException>()
                .CircuitBreaker(1, _breakDuration);
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

            var getPolicy = Policy
                .Wrap(
                    Policy.Handle<MySqlException>().Fallback(() => GetFromCache(key)),
                    Policy.Handle<MySqlException>().Retry(),
                    Policy.Handle<MySqlException>().CircuitBreaker(2, _breakDuration)
                );
            
            session = getPolicy.Execute(() => GetFromDb(key));

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
                return null;
            }

            return _cache[key];
        }

        private Session GetFromDb(string key)
        {
            if (!_db.ContainsKey(key))
            {
                return null;
            }

            return _db[key];
        }

        private void RemoveFromDb(string key) 
        {
            _db.Remove(key);
        }

        private void RefreshInDb(string key)
        {
            if(!_db.ContainsKey(key))
            {
                throw new KeyNotFoundException();
            }

            var session = _db[key];

            session.ExpiryDate = session.ExpiryDate.Value.Add(session.Lifetime.Value);
        }
    }
}