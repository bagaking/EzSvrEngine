using System;
using System.Threading;
using System.Threading.Tasks;
using EzSvrEngine.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using StackExchange.Redis;

namespace EzSvrEngine.Sched {

    public abstract partial class Sched {

        protected IConfiguration _conf { get; set; }
        protected ILogger<Sched> _logger { get; set; }
        protected IMongoDatabase _mongo { get; set; }
        protected IDatabase _redis { get; set; }

        protected Sched(IServiceProvider serviceProvider) {
            _conf = (IConfiguration)serviceProvider.GetService(typeof(IConfiguration));
            _logger = (ILogger<Sched>)serviceProvider.GetService(typeof(ILogger<Sched>));
            _mongo = (IMongoDatabase)serviceProvider.GetService(typeof(IMongoDatabase));
            _redis = (IDatabase)serviceProvider.GetService(typeof(IDatabase));
        }

        /// <summary>
        /// 执行周期开始时间
        /// </summary>
        public DateTime PerformStartTime => new DateTime(2018, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// 从执行开始时间计算的Timestamp
        /// </summary>
        public TimeSpan PerformTimeOffset => DateTime.UtcNow - PerformStartTime;

        /// <summary>
        /// 执行周期开始时间点的timestamp
        /// </summary>
        public long PerformTimestamp => (long)(PerformStartTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        /// <summary>
        /// 该scheduler的主Timer
        /// </summary>
        public Timer Timer { get; private set; }

        /// <summary>
        /// 执行间隔时间
        /// </summary>
        public abstract TimeSpan Interval { get; }

        /// <summary>
        /// 成功执行且未退出时的活锁时间
        /// </summary>
        public abstract TimeSpan LockSpan { get; }

        /// <summary>
        /// 一个完整周期时间
        /// </summary>
        public abstract TimeSpan PerformSpan { get; }

        /// <summary>
        /// 功能在该服务器上执行
        /// </summary>
        public abstract bool IsMaster { get; }

        /// <summary>
        /// 功能名前缀
        /// </summary>
        public abstract string Prefix { get; }

        /// <summary>
        /// 功能的允许状态
        /// </summary>
        public virtual bool Enabled => true;

        /// <summary>
        /// 获取当前的RoundKey
        /// </summary>
        public string GetRoundKey(int offset = 0, string sub = "") => GetRoundKey(DateTime.UtcNow, offset, sub);


        public bool RunningAsMaster { get; private set; }

        /// <summary>
        /// 获取某个时间的RoundKey
        /// </summary>
        /// <param name="time">时间</param>
        /// <param name="offset">偏移数</param>
        public string GetRoundKey(DateTime time, int offset = 0, string sub = "") {
            return GetNameKey($"r{GetRound(time) + offset}" + (string.IsNullOrWhiteSpace(sub) ? "" : $"-{sub}"));
        }

        public string GetNameKey(string sub = "") {
            return $"sched:{Prefix}-{Convert.ToString(PerformTimestamp, 16)}" + (string.IsNullOrWhiteSpace(sub) ? "" : $":{sub}");
        }

        public int GetRound(DateTime time) {
            var current_total_ms = (time - PerformStartTime).TotalMilliseconds;
            var round_id = (int)Math.Floor(current_total_ms / PerformSpan.TotalMilliseconds); // PerformSpan Cannot Be Empty
            var round_time_left = current_total_ms % PerformSpan.TotalMilliseconds;
            return round_id;
        }

        public DateTime RoundToDateTime(int round) {
            return PerformStartTime.Add(TimeSpan.FromMilliseconds(round * PerformSpan.TotalMilliseconds));
        }

        public int StartRunning() {
            if (!IsMaster) return int.MinValue;

            // 启用了服务发现的情况下, 检查是否有其他的Master存在
            if (ServiceDiscovery.Instance != null) {
                var discover_name = ServiceDiscovery.GetDiscoverName(GetNameKey());
                var task_checkstate = ServiceDiscovery.Instance.GetSchedServiceState(discover_name);
                Task.WaitAll(task_checkstate);
                var master_state = task_checkstate.Result;
                if (master_state != null) {
                    Console.WriteLine($"Error : master already exist ({Prefix}).");
                    return int.MinValue;
                }
            }

            //标记以Master方式运行
            RunningAsMaster = true;

            CreateContext().RecordAlive().Wait();

            var time_start = PerformStartTime.Add(Math.Ceiling((DateTime.UtcNow - PerformStartTime).TotalMilliseconds /
                                                               Interval.TotalMilliseconds) * Interval);
            var offset_start = time_start - DateTime.UtcNow;

            Timer = new Timer(async t => {
                var ctx = CreateContext(); // 创建演出上下文
                await ctx.RecordAlive();

                if (!Enabled) return;



                await OnUpdate(ctx);

                if (!await ctx.SetCurrentStatus(PerformContext.Status.Started)) return; // 尝试标记开始, 如果已经开始，退出  
                if (!await ctx.Lock()) return; // 尝试上活锁, 如果加锁失败, 退出

                try {
                    await OnPerform(ctx);
                    await ctx.SetCurrentStatus(PerformContext.Status.Completed); //标记完成 
                } catch (Exception ex) {
                    await ctx.SetCurrentStatus(PerformContext.Status.Error); //标记错误 
                    _logger.LogError($"Sechedule {ctx.CurrRoundKey} Error : {ex.Message}");
                } finally {
                    await ctx.Unlock();
                }

            }, null, offset_start, Interval);

            return offset_start.Seconds;
        }

        protected abstract Task<bool> OnPerform(PerformContext context);

        protected virtual async Task<bool> OnUpdate(PerformContext context) {
            return true;
        }

    }
}
