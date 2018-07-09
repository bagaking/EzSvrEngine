using System;
using Microsoft.Extensions.Configuration;

namespace EzSvrEngine.Sched {

    public abstract class ConfigScheduler : Sched {
        protected ConfigScheduler(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public IConfigurationSection conf_section => _conf.GetSection($"plat:scheduler:{Prefix}");

        public override bool IsMaster => conf_section.GetValue<bool>("is_master");
        public bool InTesting => conf_section.GetValue<bool>("in_testing");

        public override TimeSpan Interval => TimeSpan.FromSeconds(conf_section.GetValue<long>("interval"));
        public override TimeSpan LockSpan => TimeSpan.FromSeconds(conf_section.GetValue<long>("lock_span"));
        public override TimeSpan PerformSpan => TimeSpan.FromSeconds(conf_section.GetValue<long>("perform_span")); 
    }
}
