﻿using System;
using System.Windows.Forms;

namespace LyncZMachine {
    using System.Configuration;
    using System.Net;

    using Microsoft.Rtc.Collaboration;
    using Microsoft.Rtc.Signaling;

    public partial class SetupForm : Form {
        public SetupForm() {
            InitializeComponent();

            nudPort.Value = Convert.ToDecimal(ConfigurationManager.AppSettings["Port"]);
            txtServer.Text = ConfigurationManager.AppSettings["LyncServer"];
            txtSip.Text = ConfigurationManager.AppSettings["sip"];
            txtUsername.Text = ConfigurationManager.AppSettings["username"];
            txtPassword.Text = ConfigurationManager.AppSettings["pw"];
            txtDomain.Text = ConfigurationManager.AppSettings["domain"];
        }

        private void cbShowPassword_CheckedChanged(object sender, EventArgs e) {
            txtPassword.UseSystemPasswordChar = !cbShowPassword.Checked;
        }

        private void btnSave_Click(object sender, EventArgs e) {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings["Port"].Value = nudPort.Value.ToString();
            config.AppSettings.Settings["LyncServer"].Value = txtServer.Text;
            config.AppSettings.Settings["sip"].Value = txtSip.Text;
            config.AppSettings.Settings["username"].Value = txtUsername.Text;
            config.AppSettings.Settings["pw"].Value = txtPassword.Text;
            config.AppSettings.Settings["domain"].Value = txtDomain.Text;

            config.Save();
            ConfigurationManager.RefreshSection("appSettings");

            MessageBox.Show("Settings saved.  The Lync ZMachine service must be restarted to apply these settings", "Settings Saved");
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
    }
}