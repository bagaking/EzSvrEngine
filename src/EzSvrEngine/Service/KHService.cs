using StackExchange.Redis;

namespace EzSvrEngine.Service {
     
    public abstract class KHService {
        public abstract HashEntry[] GetServiceInfo(); 
    }


}
