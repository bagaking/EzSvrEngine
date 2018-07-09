using System;
using System.Collections.Generic;
using EzSvrEngine.Utils;

namespace EzSvrEngine.Actor {

    public static class ActorFactory {

        public static readonly List<Actor> Actors = new List<Actor>();
 
        public static T Create<T>(IServiceProvider service_provider, string mq_name) where T : Actor {
            var type_obj = typeof(T);
            var type_name = type_obj.FullName;
            var instance = type_obj.Assembly.CreateInstance(
                typeName: type_name,
                ignoreCase: true,
                bindingAttr: System.Reflection.BindingFlags.CreateInstance,
                binder: null,
                args: new object[] {
                    service_provider, mq_name
                },
                culture: null,
                activationAttributes: null);
             
            var actor = instance as T;

            if (null != actor) {
                Actors.Add(actor);

                ColorConsole.WriteLine(ConsoleColor.DarkGray,
                    $"No.{Actors.Count}Actor<{type_name}> Created [ role : {actor.Role}, ID : {actor.ActorID} ] ");
            }

            return actor;
        }

        public static void StartAll() {
            foreach (var actor in Actors) {
                if (actor == null) continue;

                var thread_id = actor.StartRunning();
                ColorConsole.WriteLine(ConsoleColor.DarkGreen,
                    $"Running Actor<{actor.Prefix}:{actor.ActorID}> at thread {thread_id} [ role : {actor.Role} ] ");
            }
        }

    }
}
