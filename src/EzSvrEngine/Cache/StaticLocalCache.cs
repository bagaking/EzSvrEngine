using System;

namespace EzSvrEngine.Cache {

    public static class StaticLocalCache {

        private static readonly ILocalCache cache = new LocalCache(LocalCache.NoExpiration);
         
        public static void SetCache(string key, object obj, TimeSpan expire_offset) {
            cache.SetCache(key, obj, expire_offset);
        }

        public static object GetCache(string key) {
            return cache.GetCache(key);
        }
    }
}
