using System; 

namespace EzSvrEngine.Cache {

    public interface ILocalCache {

        void SetCache(string key, object obj, TimeSpan? expire_offset);

        object GetCache(string key);
    }

    public interface ILocalCache<T> : ILocalCache {

    }
}
