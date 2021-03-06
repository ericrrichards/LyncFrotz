namespace LyncZMachine {
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using log4net;

    using Microsoft.AspNet.SignalR;

    public interface IZMachineHub {
        Task SendMessage(string id, string message);
    }

    public interface IZMachineClient {
        void Quit();
        void AddInput(string input);
        void StartGame(string gameFile);
    }

    public class ZMachineHub : Hub<IZMachineClient>, IZMachineHub {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Dictionary<string, string> _connectionMap = new Dictionary<string, string>();
        private static readonly Dictionary<string, ZMachineSession> _sessionMap = new Dictionary<string, ZMachineSession>();

        public static bool ClientsConnected { get { return _connectionMap.Count > 0; } }

        public static void RegisterSession(ZMachineSession session) {
            _sessionMap[session.ID] = session;
        }

        public override Task OnConnected() {
            _connectionMap[Context.QueryString["id"]] = Context.ConnectionId;
            Log.DebugFormat("Connected {0} as session {1}", Context.ConnectionId, Context.QueryString["id"]);
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled) {
            Log.Debug("ZMachineHub.OnDisconnected " + Context.ConnectionId);
            _connectionMap.Remove(Context.QueryString["id"]);

            return base.OnDisconnected(stopCalled);
        }

        public static void AddInput(string id, string input) {
            GlobalHost.ConnectionManager.GetHubContext("ZMachineHub").Clients.Client(_connectionMap[id]).AddInput(input);
        }

        public static void SendQuit(string id) {
            GlobalHost.ConnectionManager.GetHubContext("ZMachineHub").Clients.Client(_connectionMap[id]).Quit();
        }

        public static void StartGame(string id, string gameChoice) {
            GlobalHost.ConnectionManager.GetHubContext("ZMachineHub").Clients.Client(_connectionMap[id]).StartGame(gameChoice);
        }

        public async Task SendMessage(string id, string message) {
            Log.DebugFormat("ZMachineHub.SendMessage id={0} message={1}", id, message);
            var session = _sessionMap[id];
            await session.SendMessage(message);
        }

        public static void DisconnectAll() {
            Log.Debug("ZMachineHub.DisconnectAll");
            GlobalHost.ConnectionManager.GetHubContext("ZMachineHub").Clients.All.Quit();
        }
    }
}