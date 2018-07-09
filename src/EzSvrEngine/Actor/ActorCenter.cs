using System.Collections.Generic;
using System.Threading;

namespace EzSvrEngine.Actor {

    public class ActorCenter {
        private const uint MaxThreadAmount = 3;

        private readonly List<Thread> _threads = new List<Thread>();

        public void StartRunning(object obj) {
            for (var i = 0; i < MaxThreadAmount; i++) {
                var running_thread = new Thread(DoWork);
                _threads.Add(running_thread);
                running_thread.Start(obj);
            }
        }

        private void DoWork(object obj)
        {
            
        }
    }
}
