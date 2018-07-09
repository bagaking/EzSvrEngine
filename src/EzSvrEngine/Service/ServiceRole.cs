namespace EzSvrEngine.Service {

    public enum ServiceRole {
        Abort = 0,  // 禁止启动
        Master = 1, // 以主方式启动, 没有其他主时自动提供服务, 不降级
        Slave = 2,  // 以主方式启动, 没有其他主时自动提供服务, 检测到主自动降级
        Group = 3,  // 允许多台, 无视其他任何情况
    }

}
