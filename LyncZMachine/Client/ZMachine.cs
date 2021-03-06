namespace LyncZMachine.Client {
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Xml.Linq;

    using log4net;
    using log4net.Config;

    using Microsoft.AspNet.SignalR.Client;

    public class ZMachine : MarshalByRefObject, IZMachineClient {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private string ID { get; set; }
        private string PlayerName { get; set; }
        private UcmaFrotzScreen _frotz;
        private Thread _messageQueue;
        private readonly Queue<string> _queuedMessages;
        private Thread _frotzThread;
        private HubConnection _hubConn;
        private IHubProxy<IZMachineHub, IZMachineClient> _proxy;
        private bool Running { get; set; }
        private bool DrainQueueAndQuit { get; set; }

        internal static bool FrotzWaitingForInput { get; set; }

        internal static event EventHandler<string> InputReceived;

        private void OnInputReceived(string e) {
            var handler = InputReceived;
            if (handler != null) {
                handler(this, e);
            }
        }

        public ZMachine() {
            _queuedMessages = new Queue<string>();


        }

        private void SetupLogging() {
            try {
                var configfile = XDocument.Load(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
                var loggingConfig = configfile.Descendants("log4net").FirstOrDefault();
                if (loggingConfig != null) {
                    var appender = loggingConfig.Descendants("appender").FirstOrDefault(e => e.Attribute("name").Value == "DebugAppender");
                    if (appender != null) {
                        var fileElem = appender.Descendants("file").FirstOrDefault();
                        if (fileElem != null) {
                            fileElem.Attribute("value").SetValue(string.Format("logs/{0}_", ID));
                        }
                    }
                }

                XmlConfigurator.Configure(loggingConfig.ToXmlElement());
            } catch (Exception ex) {
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
            }
        }

        public void Run() {
            try {
                ID = AppDomain.CurrentDomain.GetData("id").ToString();
                PlayerName = AppDomain.CurrentDomain.GetData("player").ToString();



                var qsData = new Dictionary<string, string>();
                qsData["id"] = ID;
                var url = string.Format("http://localhost:{0}/ZMachine", ZMachineSettings.Settings.Port);
                _hubConn = new HubConnection(url, qsData);
                _proxy = _hubConn.CreateHubProxy<IZMachineHub, IZMachineClient>("ZMachineHub");
                _proxy.SubscribeOn<string>(hub => hub.StartGame, StartGame);
                _proxy.SubscribeOn<string>(hub => hub.AddInput, AddInput);
                _proxy.SubscribeOn(hub => hub.Quit, Quit);
                _hubConn.Start().Wait();

                SetupLogging();


                _frotz = new UcmaFrotzScreen(PlayerName);
                Frotz.os_.SetScreen(_frotz);
                _frotz.MessageReady += (o, s) => { SendMessage(s); };
            } catch (AggregateException aex) {
                var ex1 = aex.InnerException;
                Log.Error("Exception in " + ex1.TargetSite.Name, ex1);
            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
            }
        }

        public void Quit() {
            Log.Debug("Quit message received");
            DrainQueueAndQuit = true;
        }

        private void SendMessage(string s) {
            try {
                _proxy.Call(hub => hub.SendMessage(ID, s));
            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
            }
        }

        public void AddInput(string input) {
            if (!FrotzWaitingForInput) {
                _queuedMessages.Enqueue(input);
            } else {
                FrotzWaitingForInput = false;
                OnInputReceived(input);
            }
        }

        public void StartGame(string filename) {
            _frotzThread = new Thread(() => {
                Frotz.Generic.main.MainFunc(new[] { filename });
            });

            _frotzThread.Start();
            Running = true;
            _messageQueue = new Thread(ProcessMessages);
            _messageQueue.Start();
        }

        private void ProcessMessages() {
            while (Running) {
                if (_queuedMessages.Count != 0) {
                    _frotz.AddInput(_queuedMessages.Dequeue());
                } else if (DrainQueueAndQuit) {
                    Log.Debug("Killing zMachine");
                    Running = false;
                }
                Thread.Sleep(100);
            }
            if (_frotzThread != null) {
                _frotzThread.Abort();
                _frotzThread = null;
            }
            Log.Debug("zMachine dead");
            _hubConn.Stop();
        }

        
    }
}