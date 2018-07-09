using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EzSvrEngine.Service;
using StackExchange.Redis;

namespace EzSvrEngine.Sched {

    public class SchedService : KHService {
        public string[] IpAddrs { get; set; }
        public bool Enabled { get; set; }
        public int CurrRound { get; set; }
        public Sched.PerformContext.Status CurrState { get; set; }

        public long StartTimestamp { get; set; }
        public long ExecuteInterval { get; set; }
        public long PerformInterval { get; set; }

        public override HashEntry[] GetServiceInfo() {
            return new[] {
                new HashEntry("IpAddrs", ServerInfo.IPAggregateString),
                new HashEntry("Enabled", Enabled),
                new HashEntry("CurrRound", CurrRound),
                new HashEntry("CurrState", CurrState.ToString()),

                new HashEntry("StartTimestamp", StartTimestamp),
                new HashEntry("ExecuteInterval", ExecuteInterval),
                new HashEntry("PerformInterval", PerformInterval)
            };
        } 

        public static async Task<SchedService> FromScheduler(Sched sched) {
            try {
                var ctx = sched.CreateContext();
                var s = new SchedService {
                    IpAddrs = ServerInfo.IPStrs,
                    Enabled = sched.Enabled,
                    StartTimestamp = sched.PerformTimestamp,
                    ExecuteInterval = (long)sched.Interval.TotalSeconds,
                    PerformInterval = (long)sched.PerformSpan.TotalSeconds,
                    CurrRound = ctx.CurrRound,
                    CurrState = await ctx.GetCurrentStatus(),

                };
                return s;
            } catch {
            }
            return null;
        }


        public static implicit operator SchedService(HashEntry[] hes) {
            try {
                if (hes == null) return null;
                var s = new SchedService();
                foreach (var hs in hes) {
                    switch (hs.Name) {
                        case "IpAddrs":
                            s.IpAddrs = (from str_ip in ((string)hs.Value).Split(',')
                                         where !string.IsNullOrWhiteSpace(str_ip)
                                         select str_ip.Trim()).ToArray();
                            break;
                        case "Enabled":
                            s.Enabled = (bool)hs.Value;
                            break;
                        case "CurrRound":
                            s.CurrRound = (int)hs.Value;
                            break;
                        case "CurrState":
                            s.CurrState = Enum.Parse<Sched.PerformContext.Status>(hs.Value, true);
                            break;
                        case "StartTimestamp":
                            s.StartTimestamp = (long)hs.Value;
                            break;
                        case "ExecuteInterval":
                            s.ExecuteInterval = (long)hs.Value;
                            break;
                        case "PerformInterval":
                            s.PerformInterval = (long)hs.Value;
                            break;
                    }
                }

                return s;
            } catch {
            }
            return null;
        }

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append("IpAddrs\t:").Append((from addr in IpAddrs select addr)
                .Aggregate(new StringBuilder(), (_, ip) => _.Append(',').Append(ip))).AppendLine();
            sb.Append("Enabled\t:").Append(Enabled).AppendLine();
            sb.Append("CurrRound\t:").Append(CurrRound).AppendLine();
            sb.Append("CurrState\t:").Append(CurrState).AppendLine();
            sb.Append("StartTimestamp\t:").Append(StartTimestamp).AppendLine();
            sb.Append("ExecuteInterval\t:").Append(ExecuteInterval).AppendLine();
            sb.Append("PerformInterval\t:").Append(PerformInterval).AppendLine();
            return sb.ToString();
        }
    }


}
