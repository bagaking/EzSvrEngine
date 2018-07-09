using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EzSvrEngine.Service;
using StackExchange.Redis;

namespace EzSvrEngine.Actor {

    public class ActorService : KHService {
        public string[] IpAddrs { get; set; }
        public ServiceRole Role { get; set; }
        public Guid ActorID { get; set; }
        public string MQTopic { get; set; } 

        public override HashEntry[] GetServiceInfo() {
            return new[] {
                new HashEntry("IpAddrs", ServerInfo.IPAggregateString),
                new HashEntry("Role", (int)Role),
                new HashEntry("ActorID", ActorID.ToString()),
                new HashEntry("MQTopic", MQTopic), 
            };
        }  

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append("IpAddrs\t:").Append((from addr in IpAddrs select addr)
                .Aggregate(new StringBuilder(), (_, ip) => _.Append(',').Append(ip))).AppendLine();
            sb.Append("Role\t:").Append(Role).AppendLine();
            sb.Append("ActorID\t:").Append(ActorID).AppendLine();
            sb.Append("MQTopic\t:").Append(MQTopic).AppendLine(); 
            return sb.ToString();
        }

        public static async Task<ActorService> FromActor(Actor actor) {
            try { 
                var s = new ActorService {
                    IpAddrs = ServerInfo.IPStrs,
                    Role = actor.Role,
                    ActorID = actor.ActorID,
                    MQTopic = actor.Prefix
                };
                return s;
            } catch {
            }
            return null;
        }
    }


}
