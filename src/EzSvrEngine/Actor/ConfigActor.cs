using System;
using EzSvrEngine.Service;
using Microsoft.Extensions.Configuration;

namespace EzSvrEngine.Actor {

    public abstract class ConfigActor : Actor {
        protected ConfigActor(IServiceProvider serviceProvider, string prefix) : base(serviceProvider, prefix) { } 

        public IConfigurationSection conf_section => _conf.GetSection($"plat:actor:{Prefix}");

        public override ServiceRole Role => (ServiceRole)conf_section.GetValue<int>("role");
    }
}
