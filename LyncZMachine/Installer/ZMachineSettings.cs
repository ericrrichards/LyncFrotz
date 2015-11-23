namespace LyncZMachine {
    using System;
    using System.IO;
    using System.Reflection;
    using System.Xml.Serialization;

    using log4net;

    [Serializable]
    public class ZMachineSettings{
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static string AppDataFolder { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LyncZMachine"); } }

        public int Port { get; set; }
        public string LyncServer { get; set; }
        public string Sip { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Domain { get; set; }

        private static readonly Lazy<ZMachineSettings> Lazy = new Lazy<ZMachineSettings>( ()=> {
            try {
                if (!Directory.Exists(AppDataFolder)) {
                    Directory.CreateDirectory(AppDataFolder);
                }
                var serializer = new XmlSerializer(typeof(ZMachineSettings));
                using (var fs = new FileStream(Path.Combine(AppDataFolder, "settings.xml"), FileMode.Open)) {
                    var settings = serializer.Deserialize(fs) as ZMachineSettings;
                    if (settings != null) {
                        return settings;
                    }
                }
            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
            }
            return new ZMachineSettings();
        });
        

        public static ZMachineSettings Settings { get { return Lazy.Value; } }

        public void Save() {
            var serializer = new XmlSerializer(typeof(ZMachineSettings));
            using (var fs = new FileStream(Path.Combine(AppDataFolder, "settings.xml"), FileMode.OpenOrCreate)) {
                serializer.Serialize(fs,this);
            }
        }
    }
}