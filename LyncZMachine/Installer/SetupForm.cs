using System;
using System.Windows.Forms;

namespace LyncZMachine {
    using System.Configuration;
    using System.IO;
    using System.Net;
    using System.Reflection;

    using log4net;

    using LyncZMachine.Installer;

    using Microsoft.Rtc.Collaboration;
    using Microsoft.Rtc.Signaling;

    public partial class SetupForm : Form {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public SetupForm() {
            InitializeComponent();

            nudPort.Value = ZMachineSettings.Settings.Port;
            txtServer.Text = ZMachineSettings.Settings.LyncServer;
            txtSip.Text = ZMachineSettings.Settings.Sip;
            txtUsername.Text = ZMachineSettings.Settings.Username;
            txtPassword.Text = ZMachineSettings.Settings.Password;
            txtDomain.Text = ZMachineSettings.Settings.Domain;

            LoadGames();
        }

        private void LoadGames() {
            try {
                lbCurrentGames.Items.Clear();
                if (!Directory.Exists(Path.Combine(ZMachineSettings.AppDataFolder, "Games"))) {
                    Directory.CreateDirectory(Path.Combine(ZMachineSettings.AppDataFolder, "Games"));
                }
                var games = Directory.GetFiles(Path.Combine(ZMachineSettings.AppDataFolder, "Games"));
                foreach (var game in games) {
                    lbCurrentGames.Items.Add(Path.GetFileNameWithoutExtension(game));
                }
            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
            }
        }

        private void cbShowPassword_CheckedChanged(object sender, EventArgs e) {
            txtPassword.UseSystemPasswordChar = !cbShowPassword.Checked;
        }

        private void btnSave_Click(object sender, EventArgs e) {

            ZMachineSettings.Settings.Port = (int)nudPort.Value;
            ZMachineSettings.Settings.LyncServer = txtServer.Text;
            ZMachineSettings.Settings.Sip = txtSip.Text;
            ZMachineSettings.Settings.Username = txtUsername.Text;
            ZMachineSettings.Settings.Password = txtPassword.Text;
            ZMachineSettings.Settings.Domain = txtDomain.Text;

            ZMachineSettings.Settings.Save();

            MessageBox.Show("Settings saved.  The Lync ZMachine service will be restarted to apply these settings", "Settings Saved");

            const string serviceName = "Lync Z-Machine";

            try {
                MessageBox.Show(Assembly.GetExecutingAssembly().Location);
                Cursor = Cursors.WaitCursor;
                if (Services.IsInstalled(serviceName)) {
                    Services.Uninstall(serviceName);
                    Services.InstallAndStart(serviceName, serviceName, Assembly.GetExecutingAssembly().Location);
                } else {
                    Services.InstallAndStart(serviceName, serviceName, Assembly.GetExecutingAssembly().Location);
                }
                MessageBox.Show("Service started successfully");
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "Error starting service");
            } finally {
                Cursor = Cursors.Default;
            }
        }

        private void btnTest_Click(object sender, EventArgs e) {
            try {
                Cursor = Cursors.WaitCursor;
                var clientPlatformSettings = new ClientPlatformSettings("LyncZMachine", SipTransportType.Tls);
                var collabPlatform = new CollaborationPlatform(clientPlatformSettings);
                collabPlatform.EndStartup(collabPlatform.BeginStartup(null, collabPlatform));

                var settings = new UserEndpointSettings(txtSip.Text, txtServer.Text) {
                    Credential = new NetworkCredential(txtUsername.Text, txtPassword.Text, txtDomain.Text),
                    AutomaticPresencePublicationEnabled = true
                };

                var endpoint = new UserEndpoint(collabPlatform, settings);

                endpoint.EndEstablish(endpoint.BeginEstablish(null, null));
                btnSave.Enabled = true;
                MessageBox.Show("Connected successfully to " + txtServer.Text + " as " + txtSip.Text);
                try {
                    endpoint.EndTerminate(endpoint.BeginTerminate(null, null));
                    collabPlatform.EndShutdown(collabPlatform.BeginShutdown(null, null));
                } catch (Exception ex) {
                    MessageBox.Show(ex.Message, "An Error Occurred", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }

            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "An Error Occurred", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                btnSave.Enabled = false;
            } finally {
                Cursor = Cursors.Default;
            }
        }

        private void DisableSave(object sender, EventArgs e) { btnSave.Enabled = false; }

        private void btnAddGames_Click(object sender, EventArgs e) {
            var picker = new OpenFileDialog();
            if (picker.ShowDialog() == DialogResult.OK) {
                var filename = picker.FileName;
                File.Copy(filename, Path.Combine(ZMachineSettings.AppDataFolder, "Games", Path.GetFileName(filename)));
                LoadGames();
            }
        }

    }
}
