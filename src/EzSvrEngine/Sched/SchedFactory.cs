using System;
using System.Collections.Generic;
using EzSvrEngine.Utils;

namespace EzSvrEngine.Sched {

    public static class SchedFactory {
        public static readonly Dictionary<string, Sched> schedulers = new Dictionary<string, Sched>();

        public static T Get<T>() where T : class {
            var type_name = typeof(T).FullName;
            schedulers.TryGetValue(type_name, out var scheduler);
            return scheduler as T;
        }

        public static T Create<T>(IServiceProvider service_provider) where T : Sched {
            var type_obj = typeof(T);
            var type_name = type_obj.FullName;
            if (schedulers.ContainsKey(type_name)) {
                ColorConsole.WriteLine(ConsoleColor.Red, $"Error: Scheduler<{type_name}> Already Exist.");
                return schedulers[type_name] as T;
            }

            var instance = type_obj.Assembly.CreateInstance(
                typeName: type_name,
                ignoreCase: true,
                bindingAttr: System.Reflection.BindingFlags.CreateInstance,
                binder: null,
                args: new object[] {
                    service_provider
                },
                culture: null,
                activationAttributes: null);

            var scheduler = instance as T;
            if (null != scheduler) {
                schedulers.Add(type_name, scheduler);

                ColorConsole.WriteLine(ConsoleColor.DarkGray,
                    $"No.{schedulers.Count} Scheduler<{type_name}> Created [ isMaster : {scheduler.IsMaster}, Key : {scheduler.GetNameKey()} ] ");
            }

            return scheduler;
        }

        public static void StartAll() {

            foreach (var scheduler in schedulers.Values) {
                if (scheduler == null) continue;
                var result = scheduler.StartRunning();
                var str_result = result != int.MinValue ? $"after {result} sec" : "failed";

                ColorConsole.WriteLine(ConsoleColor.DarkGreen,
                    $"Running Scheduler<{scheduler.GetType()}> {str_result} [ isMaster : {scheduler.IsMaster}, Key : {scheduler.GetNameKey()} ] ");

            }
        }


    }
}
