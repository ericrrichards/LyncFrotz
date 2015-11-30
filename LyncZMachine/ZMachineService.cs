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
    using Microsoft.Rtc.Collaboration.AsyncExtensions;
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

        protected override async void OnStart(string[] args) {
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
            await _collabPlatform.StartupAsync();

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
            await _endpoint.EstablishAsync();

            _endpoint.RegisterForIncomingCall<InstantMessagingCall>(GameStarted);

            _webServer = WebApp.Start<Startup>(string.Format("http://+:{0}/ZMachine", ZMachineSettings.Settings.Port));
        }

        protected override async void OnStop() {
            await _endpoint.TerminateAsync();
            await _collabPlatform.ShutdownAsync();
            _webServer.Dispose();
        }

        private static async void GameStarted(object sender, CallReceivedEventArgs<InstantMessagingCall> e) {
            Log.Debug("Call received from " + e.RemoteParticipant.Uri);
            var call = e.Call;
            call.StateChanged += (o, args) => Log.DebugFormat("{0} -> {1}", e.RemoteParticipant.Uri, args.State);
            await call.AcceptAsync();
            var session = new ZMachineSession(call);
            session.Start();
        }
    }
}
