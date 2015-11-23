using System;
using System.Windows.Forms;

namespace LyncZMachine {
    using System.Configuration;
    using System.Net;
    using System.Reflection;

    using LyncZMachine.Installer;

    using Microsoft.Rtc.Collaboration;
    using Microsoft.Rtc.Signaling;

    public partial class SetupForm : Form {
        public SetupForm() {
            InitializeComponent();

            nudPort.Value = ZMachineSettings.Settings.Port;//Convert.ToDecimal(ConfigurationManager.AppSettings["Port"]);
            txtServer.Text = ZMachineSettings.Settings.LyncServer;//ConfigurationManager.AppSettings["LyncServer"];
            txtSip.Text = ZMachineSettings.Settings.Sip;//ConfigurationManager.AppSettings["sip"];
            txtUsername.Text = ZMachineSettings.Settings.Username;//ConfigurationManager.AppSettings["username"];
            txtPassword.Text = ZMachineSettings.Settings.Password;//ConfigurationManager.AppSettings["pw"];
            txtDomain.Text = ZMachineSettings.Settings.Domain; //ConfigurationManager.AppSettings["domain"];
        }

        private void cbShowPassword_CheckedChanged(object sender, EventArgs e) {
            txtPassword.UseSystemPasswordChar = !cbShowPassword.Checked;
        }

        private void btnSave_Click(object sender, EventArgs e) {
            //var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            //config.AppSettings.Settings["Port"].Value = nudPort.Value.ToString();
            //config.AppSettings.Settings["LyncServer"].Value = txtServer.Text;
            //config.AppSettings.Settings["sip"].Value = txtSip.Text;
            //config.AppSettings.Settings["username"].Value = txtUsername.Text;
            //config.AppSettings.Settings["pw"].Value = txtPassword.Text;
            //config.AppSettings.Settings["domain"].Value = txtDomain.Text;

            //config.Save();
            //ConfigurationManager.RefreshSection("appSettings");

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

        private void btnInstallService_Click(object sender, EventArgs e) {
            
            

        }
    }
}
