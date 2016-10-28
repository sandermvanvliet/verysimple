using System;

namespace VerySimple
{
    public interface IMySqlRepository<TEntity>
    {
        TEntity GetByKey(string key);
        void Update(TEntity entity);
        void Remove(TEntity entity);
        void Add(TEntity entity);
    }

    public class MySqlRepository : IMySqlRepository<Session>
    {
        public void Add(Session entity)
        {
            throw new NotImplementedException();
        }

        public Session GetByKey(string key)
        {
            throw new NotImplementedException();
        }

        public void Remove(Session entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Session entity)
        {
            throw new NotImplementedException();
        }
    }
}