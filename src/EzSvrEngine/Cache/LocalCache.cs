using System;
using System.Collections.Concurrent;

namespace EzSvrEngine.Cache {

    public class LocalCache : ILocalCache {
  
        private readonly ConcurrentDictionary<string, CacheItem> _cacheMap = new ConcurrentDictionary<string, CacheItem>();

        public static readonly TimeSpan NoExpiration = TimeSpan.Zero;

        public readonly TimeSpan DefaultExpiration;
        
        public LocalCache(TimeSpan? default_span) {
            DefaultExpiration = default_span ?? NoExpiration;
        }

        object ILocalCache.GetCache(string key) {
            if (!_cacheMap.ContainsKey(key)) return null;
            var now = DateTime.UtcNow;

            var cache = _cacheMap[key];
            if (cache != null && cache.Expire >= now) return _cacheMap[key].obj;

            _cacheMap.TryRemove(key, out cache);
            return null;
        }

        void ILocalCache.SetCache(string key, object obj, TimeSpan? expire_offset) {
            _cacheMap[key] = new CacheItem(expire_offset ?? DefaultExpiration, obj);
        } 
    }
}
