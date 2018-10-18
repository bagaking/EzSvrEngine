using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace EzSvrEngine.Extension {

    public static class RedisCacheExtension {

        public static async Task<RedisResult> Watch(this IDatabase _redis, string key) {
            return await _redis.ExecuteAsync("WATCH", key);
        }

        public static async Task<RedisResult> StringSetNX(this IDatabase _redis, RedisKey key, RedisValue value) {
            return await _redis.ExecuteAsync("SETNX", key, value);
        }

        public static async Task<RedisResult> UnWatch(this IDatabase _redis, string key) {
            return await _redis.ExecuteAsync("UNWATCH", key);
        }

        public static async Task<RedisResult> Multi(this IDatabase _redis) {
            return await _redis.ExecuteAsync("MULTI");
        }

        public static async Task<RedisResult> Exec(this IDatabase _redis) {
            return await _redis.ExecuteAsync("EXEC");
        }

        /// <summary>
        /// 按 key 加锁, 并不是分布式锁, 没有CAS操作, 锁后加检查
        /// 没锁上或者锁已经存在都会返回false
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> TryLockKey(this IDatabase _redis, string key, TimeSpan? expi_time_span = null) {
            var rand_hash = DateTime.UtcNow.ToString() + "_" + EzSvrEngine.Utils.Random.RandUInt32();
            var lock_state = await _redis.StringGetAsync(key);
            if (!lock_state.IsNullOrEmpty && (string)lock_state != "__unlock__") {
                return false;
            }
            var result = await _redis.StringSetAsync(key, rand_hash, expi_time_span);
            return !((!result) || !(rand_hash == await _redis.StringGetAsync(key)));
        }

        /// <summary>
        /// 按 key 解锁
        /// </summary>
        public static async Task<bool> TryUnlockKey(this IDatabase _redis, string key) {
            return await _redis.StringSetAsync(key, "__unlock__", new TimeSpan(0, 0, 1)); //1秒后解锁
        }


        #region ExpirationMq
        public static async Task<List<Tuple<string, long>>> GetAllExpirationMqTopic(this IDatabase redis) {
            var topics = await redis.SetMembersAsync("rmq_topics");

            var results = new List<Tuple<string, long>>(topics.Length);
            foreach (var topic in topics) {
                if (topic.IsNullOrEmpty) continue;
                var s_topic = (string)topic;
                var topic_length = await redis.ListLengthAsync(s_topic);
                if (topic_length <= 0) continue;
                results.Add(Tuple.Create(s_topic, topic_length));
            }
            return results;
        }



        public static async Task ExpirationMqPush<TMsgType>(this IDatabase redis, string key, TMsgType msg) {
            key = $"rmq:{key}";
            var json = JsonConvert.SerializeObject(msg);
            await redis.ListLeftPushAsync(key, json);
            await redis.SetAddAsync("rmq_topics", key);
            await redis.KeyExpireAsync(key, TimeSpan.FromDays(3)); //如果消息队列内还有内容, 则刷新三天过期时间
        }

        public static async Task<Tuple<bool, TMsgType>> ExpirationMqPop<TMsgType>(this IDatabase redis, string key) {
            var (ok, value) = await redis.ExpirationMqPop(key);
            if (!ok) return Tuple.Create(false, default(TMsgType));
            return Tuple.Create(true, JsonConvert.DeserializeObject<TMsgType>(value));
        }

        public static async Task<Tuple<bool, string>> ExpirationMqPop(this IDatabase redis, string key) {            
            
            try {
                key = $"rmq:{key}";
                var value = await redis.ListRightPopAsync(key);
                if (value.IsNullOrEmpty) return Tuple.Create(false, "");
                if (await redis.ListLengthAsync(key) <= 0) await redis.KeyExpireAsync(key, TimeSpan.FromHours(1)); //如果消息队列已经清空, 一小时过期

                return Tuple.Create(true, (string)value);
            }
            catch (Exception e) {
                Console.WriteLine(e);
                return Tuple.Create(false, "");
            }
        }

        public static async Task<long> ExpirationMqLength(this IDatabase redis, string key) {
            key = $"rmq:{key}";
            return await redis.ListLengthAsync(key);
        }
        #endregion ExpirationMq
    }
}
