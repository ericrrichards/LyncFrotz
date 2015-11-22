namespace LyncZMachine {
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Reflection;
    using System.Threading;

    using log4net;

    using Microsoft.AspNet.SignalR.Client;

    public class ZMachine : MarshalByRefObject {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private string ID { get; set; }
        private string PlayerName { get; set; }
        private UcmaFrotzScreen _frotz;
        private Thread _messageQueue;
        private readonly Queue<string> _queuedMessages;
        private Thread _frotzThread;
        private HubConnection _hubConn;
        private IHubProxy _proxy;
        private bool Running { get; set; }
        private bool DrainQueueAndQuit { get; set; }

        public ZMachine() {
            _queuedMessages = new Queue<string>();
        }

        public void Run() {
            try {
                ID = AppDomain.CurrentDomain.GetData("id").ToString();
                PlayerName = AppDomain.CurrentDomain.GetData("player").ToString();

                var qsData = new Dictionary<string, string>();
                qsData["id"] = ID;
                var url = string.Format("http://localhost:{0}/ZMachine", ConfigurationManager.AppSettings["Port"]);
                _hubConn = new HubConnection(url, qsData);
                _proxy = _hubConn.CreateHubProxy("ZMachineHub");
                _proxy.On<string>("StartGame", StartFrotz);
                _proxy.On<string>("AddInput", AddInput);
                _proxy.On("Quit", () => {
                    Log.Debug("Quit message received");
                    DrainQueueAndQuit = true;
                });
                _hubConn.Start().Wait();

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

        private void SendMessage(string s) {
            _proxy.Invoke("SendMessage", ID, s);
        }
        private void AddInput(string input) {
            _queuedMessages.Enqueue(input);
        }

        private void StartFrotz(string filename) {
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