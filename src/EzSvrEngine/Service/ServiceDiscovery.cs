using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EzSvrEngine.Actor;
using EzSvrEngine.Sched;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using StackExchange.Redis;

namespace EzSvrEngine.Service {

    public class ServiceDiscovery {

        const string GlobalKey = "khservice:";

        public static Timer Timer { get; private set; }

        private IConfiguration _conf { get; set; }
        private ILogger<Sched.Sched> _logger { get; set; }
        private IMongoDatabase _mongo { get; set; }
        private IDatabase _redis { get; set; }

        public static void Initial(IServiceProvider serviceProvider) {
            Instance = new ServiceDiscovery(serviceProvider);
        }

        protected ServiceDiscovery(IServiceProvider serviceProvider) {
            _conf = (IConfiguration)serviceProvider.GetService(typeof(IConfiguration));
            _logger = (ILogger<Sched.Sched>)serviceProvider.GetService(typeof(ILogger<Sched.Sched>));
            _mongo = (IMongoDatabase)serviceProvider.GetService(typeof(IMongoDatabase));
            _redis = (IDatabase)serviceProvider.GetService(typeof(IDatabase));
            //Instance = this;
        }

        public static ServiceDiscovery Instance { get; private set; }

        public int StartRunning() {

            var now = DateTime.UtcNow;
            var offset_start = now.Date.AddSeconds(Math.Ceiling((now - now.Date).TotalSeconds)) - now;

            Timer = new Timer(async t => {
                await MarkService();
            }, null, offset_start, TimeSpan.FromMinutes(1)); //一分钟刷新一次表

            Timer = new Timer(async t => {
                await MarkService();
                await KeepAlive();
            }, null, offset_start, TimeSpan.FromSeconds(5)); //5秒一次心跳

            return offset_start.Seconds;
        }

        public static string GetDiscoverName(string name_key) {
            return GlobalKey + name_key;
        }


        public async Task<IEnumerable<string>> ListKnownScheds() {
            return from rv in await _redis.SetMembersAsync(GlobalKey + "known_scheds") where !rv.IsNullOrEmpty select (string)rv;
        } 

        public async Task<SchedService> GetSchedServiceState(string discover_name) {
            var result = await _redis.HashGetAllAsync(discover_name);
            return result.Any() ? result : null;
        }

        private async Task<bool> MarkService() {
            foreach (var (_, sched) in SchedFactory.schedulers) {
                await _redis.SetAddAsync(GlobalKey + "known_scheds", GetDiscoverName(sched.GetNameKey()));
            }

            foreach (var actor in ActorFactory.Actors) {
                await _redis.SetAddAsync(GlobalKey + "known_actors", GetDiscoverName(actor.GetNameKey()));
            }
            return true;
        }

        private async Task<bool> KeepAlive() {
            try {
                foreach (var (_, sched) in SchedFactory.schedulers) {
                    if (!sched.IsMaster) continue;

                    var discover_name = GetDiscoverName(sched.GetNameKey());
                    await _redis.HashSetAsync(discover_name, (await SchedService.FromScheduler(sched))?.GetServiceInfo());
                    await _redis.KeyExpireAsync(discover_name, TimeSpan.FromSeconds(7));
                }

                foreach (var actor in ActorFactory.Actors) {
                    switch (actor.RunningThread.ThreadState) {
                        case ThreadState.Running:
                        case ThreadState.WaitSleepJoin: break;
                        default: continue;
                    }
                    var discover_name = GetDiscoverName(actor.GetNameKey(actor.ActorID.ToString()));
                    await _redis.HashSetAsync(discover_name, (await ActorService.FromActor(actor))?.GetServiceInfo());
                    await _redis.KeyExpireAsync(discover_name, TimeSpan.FromSeconds(7));
                }

                return true;
            } catch (Exception ex) {
                _logger.LogError(ex.Message);
                Console.WriteLine(ex.Message);
            }
            return false;
        }

    }
}
