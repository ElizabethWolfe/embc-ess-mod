﻿using EMBC.Utilities.Configuration;
using EMBC.Utilities.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace EMBC.ESS.Managers.Reports
{
    public class Configuration : IConfigureComponentServices
    {
        public void ConfigureServices(ConfigurationServices configurationServices)
        {
            var services = configurationServices.Services;

            services.AddTransient<ReportsManager>();
            services.Configure<MessageHandlerRegistryOptions>(opts => opts.Add(typeof(ReportsManager)));
        }
    }
}
