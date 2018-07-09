using System.Linq;
using System.Net;
using System.Text;

namespace EzSvrEngine {

    public static class ServerInfo {

        public static IPAddress[] IPAddrs => Dns.GetHostEntry(Dns.GetHostName()).AddressList;

        public static string[] IPStrs => (from addr in IPAddrs select addr.ToString()).ToArray();

        public static string IPAggregateString => (from addr in IPAddrs select addr).Aggregate(new StringBuilder(), (_, ip) => _.Append(',').Append(ip)).ToString();

    }
}
