namespace LyncZMachine.Client {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    using Frotz.Blorb;
    using Frotz.Screen;

    using log4net;

    class UcmaFrotzScreen : IZScreen {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const int FontHeight = 10;
        private const int FontWidth = 10;

        private readonly Dictionary<int, StringBuilder> _screenBuffers = new Dictionary<int, StringBuilder>();
        private StringBuilder _currentBuffer;
        private readonly string _playerName;
        private string PlayerName { get { return _playerName; } }

        public UcmaFrotzScreen(string playerName) {
            _playerName = playerName;
        }

        private string DefaultSaveFile(string defaultName) {
            var fileName = Path.GetFileName(defaultName);
            if (fileName != null) {
                fileName = fileName.Split('-').LastOrDefault();
                return Path.Combine(ZMachineSettings.AppDataFolder, "Saves", PlayerName + "-" + fileName);
            }
            return defaultName;
        }

        public void AddInput(string message) {
            foreach (var c in message.Trim()) {
                OnKeyPressed(c);
            }
            OnKeyPressed((char)13); // add carriage return to commit input

        }

        public event EventHandler<string> MessageReady;
        protected virtual void OnMessageReady(string e) {
            var handler = MessageReady;
            if (handler != null) {
                handler(this, e);
            }
        }
        public event EventHandler<ZKeyPressEventArgs> KeyPressed;
        protected virtual void OnKeyPressed(char key) {
            var handler = KeyPressed;
            if (handler != null) {
                handler(this, new ZKeyPressEventArgs(key));
            }
        }

        public void HandleFatalError(string Message) {
            OnMessageReady(Message);
        }

        public ScreenMetrics GetScreenMetrics() {
            return new ScreenMetrics(new ZSize(10, 10), new ZSize(400, 300), 25, 80, 1);
        }

        public void DisplayChar(char c) {
            _currentBuffer.Append(c);
            Log.DebugFormat("DisplayChar: {0} [{1}]", c, (int)c);
        }

        public void RefreshScreen() {
            Log.Debug("RefreshScreen:");
            var s = string.Empty;
            foreach (var stringBuilder in _screenBuffers) {
                var str = stringBuilder.Value.ToString();
                if (string.IsNullOrWhiteSpace(str)) {
                    continue;
                }
                if (stringBuilder.Key != 0) {
                    var scorePos = str.IndexOf("Score:");
                    if (scorePos > 0) { // ZORK 1 seems to not have any space between the location name and the score text, which looks like garbage
                        str = "<span style=\"font-weight: bold; font-family:monospace;\">" + str.Insert(scorePos, " ") + "</span>";
                    } else {
                        str = "<span style=\"font-weight: bold; font-family:monospace;\">" + str + "</span>";
                    }
                } else {
                    str = ("<span style=\"font-family:monospace; font-size:x-small;\">" + str + "</span>");
                }
                s += str.Replace("\r\n", "<br/>") + "<br/>";
            }
            foreach (var value in _screenBuffers.Values) {
                value.Clear();
            }
            if (!string.IsNullOrWhiteSpace(s)) {
                s = "<div>" + s + "</div>";

                Log.Debug(s);
                OnMessageReady(s);
            }
        }

        private ZPoint _lastScreenCursorPoint;
        public void SetCursorPosition(int x, int y) {
            Log.DebugFormat("SetCursorScreenPosition: [{0},{1}]", x, y);
            if (_lastScreenCursorPoint != null && y != _lastScreenCursorPoint.Y) {
                _currentBuffer.AppendLine();
            }
            _lastScreenCursorPoint = new ZPoint(x, y);
        }

        public void ScrollLines(int top, int height, int lines) {
            Log.DebugFormat("ScrollLines: [{0},{1},{2}]", top, height, lines);
            _currentBuffer.AppendLine();
        }



        public void SetTextStyle(int new_style) {
            Log.Debug("SetTextStyle: " + new_style);
        }

        public void Clear() {
            Log.Debug("Clear:");
        }

        public void ClearArea(int top, int left, int bottom, int right) {
            Log.DebugFormat("ClearArea: [{0},{1},{2},{3}]", top, left, bottom, right);
        }

        public Stream OpenExistingFile(string defaultName, string Title, string Filter) {
            var files = Directory.GetFiles(Path.Combine(ZMachineSettings.AppDataFolder,"Saves"), "*.sav");
            var sb = new StringBuilder();
            foreach (var file in files) {
                if (file.Contains(PlayerName)) {
                    sb.AppendLine(file);
                }
            }
            OnMessageReady(sb.ToString());


            Log.DebugFormat("OpenExistingFile: [{0},{1},{2}]", defaultName, Title, Filter);
            return new FileStream(DefaultSaveFile(defaultName), FileMode.Open);
        }

        public Stream OpenNewOrExistingFile(string defaultName, string Title, string Filter, string defaultExtension) {
            Log.DebugFormat("OpenNewOrExistingFile: [{0},{1},{2},{3}]", defaultName, Title, Filter, defaultExtension);
            return new FileStream(DefaultSaveFile(defaultName), FileMode.OpenOrCreate);
        }



        public string SelectGameFile(out byte[] filedata) {
            Log.Debug("SelectGameFile:");
            filedata = new byte[1];
            return "derp!";
        }

        public ZSize GetImageInfo(byte[] Image) { throw new NotImplementedException(); }

        public void ScrollArea(int top, int bottom, int left, int right, int units) {

        }

        public void DrawPicture(int picture, byte[] Image, int y, int x) { throw new NotImplementedException(); }

        public void SetFont(int font) {
            Log.Debug("SetFont: " + font);
        }

        public void DisplayMessage(string Message, string Caption) { OnMessageReady(string.Format("{0}\n\n{1}", Caption, Message)); }

        public int GetStringWidth(string s, CharDisplayInfo Font) { return 1; }

        public void RemoveChars(int count) { }

        public bool GetFontData(int font, ref ushort height, ref ushort width) {
            height = FontHeight;
            width = FontWidth;
            return true;
        }

        public void GetColor(out int foreground, out int background) {
            foreground = 0;
            background = 0;
        }

        public void SetColor(int new_foreground, int new_background) {
            Log.DebugFormat("SetColor: [{0},{1}]", new_foreground, new_background);
        }

        public ushort PeekColor() {
            return 0;
        }

        public void FinishWithSample(int number) { throw new NotImplementedException(); }

        public void PrepareSample(int number) { throw new NotImplementedException(); }

        public void StartSample(int number, int volume, int repeats, ushort eos) { throw new NotImplementedException(); }

        public void StopSample(int number) { throw new NotImplementedException(); }

        public void SetInputMode(bool InputMode, bool CursorVisibility) {
            Log.DebugFormat("SetInputMode: [{0},{1}]", InputMode, CursorVisibility);
        }

        public void SetInputColor() {
            Log.Debug("SetInputColor:");
        }

        public void addInputChar(char c) {
            Log.Debug("addInputChar: " + c);
            //sb.Append(c);
        }

        public void StoryStarted(string StoryName, Blorb BlorbFile) {
            Log.DebugFormat("StoryStarted: [{0},{1}]", StoryName, BlorbFile);
        }

        public ZPoint GetCursorPosition() {
            return new ZPoint(0, 0);
        }

        public void SetActiveWindow(int win) {
            _currentBuffer = _screenBuffers[win];
            Log.Debug("SetActiveWindow: " + win);
        }

        public void SetWindowSize(int win, int top, int left, int height, int width) {
            if (!_screenBuffers.ContainsKey(win)) {
                _screenBuffers[win] = new StringBuilder();
            }
            Log.DebugFormat("SetWindowSize: [{0},{1},{2},{3},{4}]", win, top, left, height, width);
        }

        public bool ShouldWrap() {
            return true;
        }


    }
}