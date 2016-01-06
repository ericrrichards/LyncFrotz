namespace LyncZMachine {
    using System;
    using System.IO;
    using System.Net.Mime;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    using log4net;

    using LyncZMachine.Client;

    using Microsoft.Rtc.Collaboration;
    using Microsoft.Rtc.Collaboration.AsyncExtensions;

    public class ZMachineSession {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private InstantMessagingCall _call;
        private ZMachineSessionState _state;
        private readonly string[] _gameChoices;

        private Isolated<ZMachine> _engine;
        public string ID { get { return _call.CallId; } }

        public ZMachineSession(InstantMessagingCall call) {

            _state = ZMachineSessionState.InitialMessage;
            _call = call;
            Log.Debug("CallID: " + call.CallId);

            _call.StateChanged += CallOnStateChanged;
            _call.Flow.MessageReceived += FlowOnMessageReceived;
            try {
                _gameChoices = Directory.GetFiles(Path.Combine(ZMachineSettings.AppDataFolder,"Games"), "*");
            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
                _gameChoices = new string[0];
            }

        }

        public void Start() {
            ZMachineHub.RegisterSession(this);
            _engine = new Isolated<ZMachine>();
            _engine.SetData("id", ID);
            _engine.SetData("player", _call.RemoteEndpoint.Participant.UserAtHost);
            _engine.Value.Run();
        }

        private void AddInput(string input) {
            ZMachineHub.AddInput(ID, input);
        }


        private void CallOnStateChanged(object sender, CallStateChangedEventArgs e) {
            switch (_state) {
                case ZMachineSessionState.InitialMessage:
                    break;
                case ZMachineSessionState.PickGame:
                    break;
                case ZMachineSessionState.PlayingGame:
                    if (e.State == CallState.Terminated) {
                        //AddInput("save");
                        //AddInput("quit");
                        //AddInput("y");
                        ZMachineHub.SendQuit(ID);
                        Task.Delay(3000).ContinueWith(task => _engine.Dispose());
                    }
                    break;
                case ZMachineSessionState.Quiting:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task SendMessage(string message) {
            if (_call.State == CallState.Established) {
                if (message.Contains("<span")) {
                    await _call.Flow.SendInstantMessageAsync(new ContentType("text/html"), Encoding.UTF8.GetBytes(message));
                    //_call.Flow.EndSendInstantMessage(_call.Flow.BeginSendInstantMessage(new ContentType("text/html"), Encoding.UTF8.GetBytes(message), null, null));
                } else {
                    await _call.Flow.SendInstantMessageAsync(message);
                    
                    //_call.Flow.EndSendInstantMessage(_call.Flow.BeginSendInstantMessage(message, null, null));
                }
            }
        }

        private async void FlowOnMessageReceived(object sender, InstantMessageReceivedEventArgs e) {
            switch (_state) {
                case ZMachineSessionState.InitialMessage:
                    _state = ZMachineSessionState.PickGame;

                    await SendGameChoiceMenu();
                    break;
                case ZMachineSessionState.PickGame:
                    uint pickedOption;
                    if (uint.TryParse(e.TextBody, out pickedOption) && _gameChoices.Length >= pickedOption) {
                        _state = ZMachineSessionState.PlayingGame;
                        await SendMessage("HINT: You can resume your last session by sending \"restore\"");
                        StartFrotz(pickedOption);
                    } else {
                        await SendMessage("That's not a valid option...");
                        await SendGameChoiceMenu();
                    }

                    break;
                case ZMachineSessionState.PlayingGame:
                    AddInput(e.TextBody);
                    //_frotz.AddInput(e.TextBody);
                    if (e.TextBody.Trim().Equals("quit", StringComparison.InvariantCultureIgnoreCase)) {
                        _state = ZMachineSessionState.Quiting;
                    }
                    break;
                case ZMachineSessionState.Quiting:
                    AddInput(e.TextBody);
                    //_frotz.AddInput(e.TextBody);
                    if (e.TextBody.Trim().Equals("y", StringComparison.InvariantCultureIgnoreCase)) {
                        await SendMessage("Thanks for playing!");
                        await _call.TerminateAsync();
                        ZMachineHub.SendQuit(ID);
                    } else {
                        _state = ZMachineSessionState.PlayingGame;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void StartFrotz(uint pickedOption) {
            ZMachineHub.StartGame(ID, _gameChoices[pickedOption - 1]);
        }

        private async Task SendGameChoiceMenu() {
            if (_gameChoices.Length == 0) {
                await SendMessage("Sorry, we're out of games right now :-(");
                await _call.TerminateAsync();
            }
            var sb = new StringBuilder("Which game would you like to play?\n");
            for (int i = 0; i < _gameChoices.Length; i++) {
                var choice = _gameChoices[i];
                sb.AppendFormat("{0})  {1}\n", i + 1, Path.GetFileNameWithoutExtension(choice));
            }

            await SendMessage(sb.ToString());
        }
    }
    
}