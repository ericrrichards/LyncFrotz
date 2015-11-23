using System;
using System.ServiceProcess;

namespace LyncZMachine {
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;

    using log4net;

    using Microsoft.Owin.Hosting;
    using Microsoft.Rtc.Collaboration;
    using Microsoft.Rtc.Collaboration.Presence;
    using Microsoft.Rtc.Signaling;

    public partial class ZMachineService : ServiceBase {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private CollaborationPlatform _collabPlatform;
        private UserEndpointSettings _settings;
        private UserEndpoint _endpoint;
        private IDisposable _webServer;

        public ZMachineService() {
            InitializeComponent();
        }

        public void RunDebug(string[] args) {
            OnStart(args);
            Console.WriteLine("Press enter to shut down");
            Console.ReadLine();
            OnStop();
        }

        protected override void OnStart(string[] args) {
            if (!Directory.Exists(Path.Combine(ZMachineSettings.AppDataFolder, "Saves"))) {
                Directory.CreateDirectory(Path.Combine(ZMachineSettings.AppDataFolder, "Saves"));
            }
            if (!Directory.Exists(Path.Combine(ZMachineSettings.AppDataFolder, "Games"))) {
                Log.Warn("No Z-Machine games included");
                Directory.CreateDirectory(Path.Combine(ZMachineSettings.AppDataFolder, "Games"));
            } else {
                if (!Directory.GetFiles(Path.Combine(ZMachineSettings.AppDataFolder, "Games")).Any()) {
                    Log.Warn("No Z-Machine games included");
                }
            }


            var clientPlatformSettings = new ClientPlatformSettings("LyncZMachine", SipTransportType.Tls);
            _collabPlatform = new CollaborationPlatform(clientPlatformSettings);
            _collabPlatform.EndStartup(_collabPlatform.BeginStartup(null, _collabPlatform));

            _settings = new UserEndpointSettings(
                ZMachineSettings.Settings.Sip,
                ZMachineSettings.Settings.LyncServer
                //ConfigurationManager.AppSettings["sip"], 
                //ConfigurationManager.AppSettings["LyncServer"]
            ) {
                Credential = new NetworkCredential(
                    ZMachineSettings.Settings.Username,
                    ZMachineSettings.Settings.Password,
                    ZMachineSettings.Settings.Domain
                    //ConfigurationManager.AppSettings["username"], 
                    //ConfigurationManager.AppSettings["pw"], 
                    //ConfigurationManager.AppSettings["domain"]
                ),
                AutomaticPresencePublicationEnabled = true
            };
            _settings.Presence.UserPresenceState = PresenceState.UserAvailable;

            _endpoint = new UserEndpoint(_collabPlatform, _settings);

            _endpoint.EndEstablish(_endpoint.BeginEstablish(null, null));

            _endpoint.RegisterForIncomingCall<InstantMessagingCall>(GameStarted);

            _webServer = WebApp.Start<Startup>(string.Format("http://+:{0}/ZMachine", ZMachineSettings.Settings.Port));
        }

        protected override void OnStop() {
            _endpoint.EndTerminate(_endpoint.BeginTerminate(null, null));
            _collabPlatform.EndShutdown(_collabPlatform.BeginShutdown(null, null));
            _webServer.Dispose();
        }

        private void GameStarted(object sender, CallReceivedEventArgs<InstantMessagingCall> e) {
            Log.Debug("Call received from " + e.RemoteParticipant.Uri);
            var call = e.Call;
            call.StateChanged += (o, args) => Log.DebugFormat("{0} -> {1}", e.RemoteParticipant.Uri, args.State);
            call.EndAccept(call.BeginAccept(null, null));
            var session = new ZMachineSession(call);
            session.Start();
        }
    }
}
