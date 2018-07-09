using System;
using System.Threading.Tasks;
using EzSvrEngine.Extension;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EzSvrEngine.Sched {

    public partial class Sched {

        public PerformContext CreateSpecifyContext(DateTime time) {
            return new PerformContext(this, time);
        }

        public PerformContext CreateContext() {
            return CreateSpecifyContext(DateTime.UtcNow);
        }

        public class PerformContext {
            public enum Status {
                None = 0,
                Started = 1,
                Completed = 2,
                Error = 10000
            }

            public Sched Master { get; }
            public DateTime TimeStart { get; }
            public TimeSpan TimeOffset => DateTime.UtcNow - TimeStart;
            public int CurrRound { get; }
            public string CurrRoundKey { get; }
            public string LastRoundKey { get; }

            public string RedisKeyStatus { get; }
            public string RedisKeyTimeCost { get; }
            public string RedisKeyLog { get; }
            public string RedisKeyAlive { get; }

            public string RedisKeyRoundLock { get; }
            public string RedisKeyRoundLog { get; }

            public DateTime TimeCTXCreated { get; }
            public DateTime TimeCTXClaimed { get; private set; }

            public PerformContext(Sched scheduler, DateTime time) {
                Master = scheduler;
                TimeStart = time.ToUniversalTime();

                CurrRound = scheduler.GetRound(time);
                CurrRoundKey = scheduler.GetRoundKey(time);
                LastRoundKey = scheduler.GetRoundKey(time, -1);

                RedisKeyStatus = Master.GetNameKey("perform_status");
                RedisKeyTimeCost = Master.GetNameKey("perform_timecost");
                RedisKeyLog = Master.GetNameKey("perform_log");
                RedisKeyAlive = Master.GetNameKey("perform_alive");

                RedisKeyRoundLog = Master.GetRoundKey(0, "log");
                RedisKeyRoundLock = Master.GetRoundKey(0, "lock");

                TimeCTXCreated = DateTime.UtcNow;
                TimeCTXClaimed = TimeCTXCreated;
            }

            public async Task<Status> GetCurrentStatus() {
                var rv_status = await Master._redis.HashGetAsync(RedisKeyStatus, CurrRoundKey);
                if (rv_status.IsNullOrEmpty) return Status.None;
                var status = (int)rv_status;
                return (Status)status;
            }

            public async Task<bool> SetCurrentStatus(Status s) {
                var status_now = await GetCurrentStatus(); ;
                if (s <= status_now) return false;

                switch (s) {
                    case Status.Completed:
                    case Status.Error:
                        await RecordTimeCost();
                        break;
                    case Status.None:
                    case Status.Started:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(s), s, null);
                }

                await Master._redis.HashSetAsync(RedisKeyStatus, new[] {
                    new HashEntry(CurrRoundKey, (int) s)
                });
                return true;
            }

            public async Task RecordTimeCost() {
                await Master._redis.HashSetAsync(RedisKeyTimeCost, new[] {
                    new HashEntry(CurrRoundKey, TimeOffset.TotalMilliseconds)
                });
            }

            public async Task LogPerform(string content) {
                Console.WriteLine($"{RedisKeyLog} | {content}");
                Master._logger.LogInformation(content);
                await Master._redis.ListRightPushAsync(RedisKeyLog, new RedisValue[] { $"r{CurrRound}-ts{(Master.PerformTimestamp + (long)Master.PerformTimeOffset.TotalSeconds)} : {content}" });
            }

            public async Task LogRound(string content) {
                Console.WriteLine($"{RedisKeyRoundLog} | {content}");
                Master._logger.LogInformation(content);
                await Master._redis.SortedSetAddAsync(RedisKeyRoundLog, content, CurrRound + (Master.PerformTimestamp + Master.PerformTimeOffset.TotalSeconds) / 100000000000);
            }

            public async Task<bool> Lock() {
                if (Master.LockSpan.TotalSeconds <= 0.001f) return true; // 不需要锁的时候返回锁成功
                return await Master._redis.TryLockKey(RedisKeyRoundLock, Master.LockSpan); // 返回锁的结果
                //TODO：这个活锁目前并不能防止并发问题（不是cas），所以在这里其实应该就没意义了？ 另外就是看state key存在时的处理方式  
            }

            public async Task<bool> Unlock() {
                if (Master.LockSpan.TotalSeconds <= 0.001f) return true; // 不需要锁的时候返回
                return await Master._redis.TryUnlockKey(RedisKeyRoundLock); // 尝试解锁
            }

            public async Task<bool> RecordAlive() {
                await Master._redis.HashSetAsync(RedisKeyAlive,
                    new[] {
                        new HashEntry("conf_master", Master.IsMaster),
                        new HashEntry("running_as_master", Master.RunningAsMaster),
                        new HashEntry("enabled", Master.Enabled),
                        new HashEntry("round", CurrRound),
                        new HashEntry("server", ServerInfo.IPAggregateString),
                        new HashEntry("time_start", TimeStart.ToString()),
                        new HashEntry("time_triggered", TimeStart.ToString())
                    });
                return await Master._redis.KeyExpireAsync(RedisKeyAlive, Master.Interval + TimeSpan.FromSeconds(1));
            }


            public TimeSpan ClaimTimeSpan() {
                var span = DateTime.UtcNow - TimeCTXClaimed;
                TimeCTXClaimed = DateTime.UtcNow;
                return span;
            }
        }

    }

}