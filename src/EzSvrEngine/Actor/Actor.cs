using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using StackExchange.Redis;
using System;
using System.Threading;
using EzSvrEngine.Extension;
using EzSvrEngine.Service;
using EzSvrEngine.Utils;

namespace EzSvrEngine.Actor {

    public abstract class Actor {

        protected static IServiceProvider _provider { get; set; }
        protected IConfiguration _conf { get; set; }
        protected ILogger<Actor> _logger { get; set; }
        protected IMongoDatabase _mongo { get; set; }
        protected IDatabase _redis { get; set; }

        protected Actor(IServiceProvider serviceProvider, string mq_name) {
            ActorID = Guid.NewGuid();
            _provider = serviceProvider;
            _conf = (IConfiguration)_provider.GetService(typeof(IConfiguration));
            _logger = (ILogger<Actor>)_provider.GetService(typeof(ILogger<Actor>));
            _mongo = (IMongoDatabase)_provider.GetService(typeof(IMongoDatabase));
            _redis = (IDatabase)_provider.GetService(typeof(IDatabase));

            Prefix = mq_name;
        }

        /// <summary>
        /// 功能名前缀, MQTopic
        /// </summary>
        public string Prefix { get; private set; }

        public Guid ActorID { get; }

        public abstract ServiceRole Role { get; }

        public Thread RunningThread { get; private set; }

        public string GetNameKey(string sub = "") {
            return $"actor:{Prefix}" + (string.IsNullOrWhiteSpace(sub) ? "" : $":{sub}");
        }

        public int StartRunning() {
            RunningThread = new Thread(DoWork);
            RunningThread.Start(_provider);
            return RunningThread.ManagedThreadId;
        }

        protected abstract bool OnPerform(string msg);

        private void DoWork(object state) {

            if (Role == ServiceRole.Abort) return; // 禁止则永远不可能进入
            try {
                while (true) {
                    var task = _redis.ExpirationMqPop(Prefix);
                    task.Wait();
                    var (ok, msg) = task.Result;
                    if (!ok) {
                        Thread.Sleep(500);
                        continue;
                    }
                    OnPerform(msg);
                }
            } catch (Exception e) {
                Console.WriteLine($"Do work exception {e.Message} {e.StackTrace}");
                _logger.LogError(e.Message);
            }
             
            lock (_provider) {
                ColorConsole.WriteLine(ConsoleColor.Red,
                    $"Actor {ActorID} of {Prefix} at thread {RunningThread.ManagedThreadId} exited.");
            }



        }


    }
}
