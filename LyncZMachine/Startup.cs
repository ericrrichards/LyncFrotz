namespace LyncZMachine {
    using System.Reflection;

    using log4net;

    using Microsoft.AspNet.SignalR;

    using Owin;

    public class Startup {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public void Configuration(IAppBuilder app) {
            app.Map("/signalr", map => {


                var hubconf = new HubConfiguration {
                    EnableJSONP = true,
                    EnableDetailedErrors = true,
                    EnableJavaScriptProxies = true,
                };


                map.RunSignalR(hubconf);
                Log.Debug("SignalR started");

            });
        }
    }
}