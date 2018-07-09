using System;

namespace EzSvrEngine.Cache {

    public class CacheItem : IComparable, IComparable<CacheItem> {

        public DateTime Expire { get; private set; }

        public object obj { get; private set; }

        public CacheItem(TimeSpan offset, object content) {
            Expire = DateTime.UtcNow.Add(offset);
            obj = content;
        }

        public int CompareTo(object obj_other) {
            if (obj_other is CacheItem other) return Expire.CompareTo(other.Expire);
            return 1;
        }

        public int CompareTo(CacheItem other) {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            return Expire.CompareTo(other.Expire);
        }
    }
}
