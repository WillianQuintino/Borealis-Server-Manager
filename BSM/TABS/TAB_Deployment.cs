﻿using System;
using System.Windows.Forms;
using MetroFramework;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Borealis
{
    public partial class ServerDeployment : Form
    {
        public ServerDeployment()
        {
            InitializeComponent();
        }

        //===================================================================================//
        // STARTUP:                                                                          //
        //===================================================================================//
        private void ServerDeployment_Load(object sender, EventArgs e)
        {
            //Populate gameserver list by querying the available configurations from the server.
            try
            {
                using (var webClient = new System.Net.WebClient())
                {
                    var json = webClient.DownloadString("http://phantom-net.duckdns.org:1337/index");

                    foreach (var serverAppName in JObject.Parse(json))
                    {
                        JToken value = serverAppName.Value;
                        dropdownServerSelection.Items.Add(value.ToString());
                    }
                }
                dropdownServerSelection.PromptText = "< Select a gameserver to deploy >";
            }
            catch (Exception)
            {
                MetroMessageBox.Show(BorealisServerManager.ActiveForm, "Cannot connect to http://phantom-net.duckdns.org:1337 \nThis means that deployment at this time is impossible.", "Server Unreachable", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //===================================================================================//
        // DEPLOYMENT:                                                                       //
        //===================================================================================//
        //Class to store relevant deployment values during deployment
        private static class DeploymentValues
        {
            public static string deployment_directory { get; set; }
            public static string verify_integrity { get; set; }
        }

        //Methods that handle reporting progress back to the UI
        private void updateProgressStatus(int currentProgress, int overallProgress, string progressDetails)
        {
            progressbarDownloadProgressOverall.Value = overallProgress;
            lblDownloadProgressDetails.Text = progressDetails;
        }
        private void proc_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                lblDownloadProgressDetails.Text = e.Data;
                try
                {
                    progressbarDownloadProgressOverall.Value = Convert.ToInt32(Double.Parse(Regex.Match(e.Data, @"\:([^)]*)\(").Groups[1].Value));
                }
                catch (Exception)
                {
                    //Do nothing
                }

                //SteamCMD error and success handler.
                if (ServerAPI.QUERY_JOBJECT.steamcmd_required == "True")
                {
                    if (e.Data == "Success! App '" + ServerAPI.QUERY_STEAM_APPID(dropdownServerSelection.Text) + "' already up to date." || e.Data == "Success! App '" + ServerAPI.QUERY_STEAM_APPID(dropdownServerSelection.Text) + "' fully installed." || e.Data == "[----] Update complete, launching..." || e.Data == "[---] Update complete, launching...")
                    {
                        GameServer_Management DeployConfig = new GameServer_Management();
                        MetroMessageBox.Show(BorealisServerManager.ActiveForm, txtServerGivenName.Text + " [" + dropdownServerSelection.Text + "]" + " has been successfully deployed with default configurations!\nPlease goto the management tab to configure it.", "Complete!", MessageBoxButtons.OK, MessageBoxIcon.Question);
                        DeployConfig.DeployGameserver(txtServerGivenName.Text, dropdownServerSelection.Text, DeploymentValues.deployment_directory, ServerAPI.QUERY_JOBJECT.server_executable_location, ServerAPI.QUERY_JOBJECT.default_launch_arguments, ServerAPI.QUERY_JOBJECT.server_config_file, false, false);
                        btnCancelDeployGameserver.Visible = false;
                        btnDeployGameserver.Enabled = true;
                    }
                    else if (e.Data == "Error! App '" + ServerAPI.QUERY_STEAM_APPID(dropdownServerSelection.Text) + "' state is 0x202 after update job.")
                    {
                        btnDeployGameserver.Enabled = false;
                        btnCancelDeployGameserver.Visible = true;
                        DeployGameServer();
                    }
                }
                else
                {
                    //RUN OTHER CODE FOR NON-STEAMCMD GAMESERVER
                }
            }
        }
        private void dropdownServerSelection_SelectedValueChanged(object sender, EventArgs e)
        {
            //Enable the option to choose where to install the server.
            lblDestination.Visible = true;
            lblDestinationDetails.Visible = true;
            lblDestinationDetailsSubtext.Visible = true;
            txtboxDestinationFolder.Visible = true;
            btnBrowseDestination.Visible = true;
            chkSeparateConfig.Visible = true;
            lblSeparateConfig.Visible = true;
            chkVerifyIntegrity.Visible = true;
            lblVerifyIntegrity.Visible = true;
            panelProgress.Visible = true;

            //Server Name Controls
            lblServerName.Visible = true;
            lblServerNameDetails.Visible = true;
            txtServerGivenName.Visible = true;

            //Deployment Button
            btnDeployGameserver.Enabled = true;

            if (chkSeparateConfig.Checked == true)
            {
                lblDestination.Text = "Step 2: Choose existing " + dropdownServerSelection.Text + " server";
            }

            if (chkSeparateConfig.Checked == true)
            {
                txtServerGivenName.Text = dropdownExistingServer.Text + " Instance01";
            }
            else
            {
                txtServerGivenName.Text = dropdownServerSelection.Text;
            }
        }
        private void dropdownExistingServer_SelectedValueChanged(object sender, EventArgs e)
        {
            if (chkSeparateConfig.Checked == true)
            {
                txtServerGivenName.Text = dropdownExistingServer.Text + " Instance";
            }
            else
            {
                txtServerGivenName.Text = dropdownServerSelection.Text;
            }
        }
        private void chkSeparateConfig_OnChange_1(object sender, EventArgs e)
        {
            if (chkSeparateConfig.Checked == true)
            {
                lblDestination.Text = "Step 2: Choose existing " + dropdownServerSelection.Text + " server";
                lblDestinationDetails.Text = "This will add a new configuration to an existing server to run a new instance of the server";
                txtboxDestinationFolder.Text = "N/A";
                txtboxDestinationFolder.Visible = false;
                dropdownExistingServer.Visible = true;
                txtServerGivenName.Text = "";
            }
            else
            {
                lblDestination.Text = "Step 2: Destination";
                lblDestinationDetails.Text = "Choose where you want to install the server";
                txtboxDestinationFolder.Text = @"C:\BSM\";
                txtboxDestinationFolder.Visible = true;
                dropdownExistingServer.Visible = false;
            }
        }

        //Methods that handle deployment itself.
        private void ExecuteWithRedirect(string argProgramName, string argParameters)
        {
            var proc = new Process();
            proc.StartInfo.Arguments = argParameters;
            proc.StartInfo.FileName = argProgramName;
            proc.StartInfo.UseShellExecute = false;

            // set up output redirection
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.EnableRaisingEvents = true;
            proc.StartInfo.CreateNoWindow = true;
            // see below for output handler
            proc.ErrorDataReceived += proc_DataReceived;
            proc.OutputDataReceived += proc_DataReceived;

            proc.Start();

            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();
        }
        private void DeployGameServer()
        {
            //Enable cancel button to terminate deployment process if needed.
            lblDownloadProgressDetails.Text = "Status: Downloading " + dropdownServerSelection.Text + "...";

            switch (ServerAPI.QUERY_JOBJECT.steamcmd_required)
            {
                case "True":
                    {
                        lblDownloadProgressDetails.Text = "Status: Downloading / Initializing SteamCMD...";
                        SteamCMD.DownloadSteamCMD();
                        switch (ServerAPI.QUERY_JOBJECT.steam_authrequired)
                        {
                            case "True":
                                MetroMessageBox.Show(BorealisServerManager.ActiveForm, "Due to the fact that we do not have an authentication system in place for Steam, you cannot download non-anonymous SteamCMD dedicated servers at this time.  We apologize, and hope to get this incorporated soon!", "Steam Authentication Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;

                            case "False":
                                try
                                {
                                    ExecuteWithRedirect(Environment.CurrentDirectory + @"\steamcmd.exe", string.Concat("+login anonymous +force_install_dir ", "\"", DeploymentValues.deployment_directory, "\"", " +app_update ", ServerAPI.QUERY_STEAM_APPID(dropdownServerSelection.Text), DeploymentValues.verify_integrity, " +quit"));
                                }
                                catch (Exception)
                                {
                                    MetroMessageBox.Show(BorealisServerManager.ActiveForm, "We cannot find the required executable to deploy the server!  Either it is missing, or the server configuration for this gameserver is corrupted.", "Error Deploying GameServer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                break;
                        }
                    }
                    break;

                case "False":
                    {
                        //RUN OTHER CODE TO DEPLOY THE NON-STEAMCMD GAMESERVER
                    }
                    break;
            }
        }
        private void btnBrowseDestination_Click(object sender, EventArgs e)
        {
            browseDestinationFolder.ShowDialog();
            txtboxDestinationFolder.Text = browseDestinationFolder.SelectedPath;
        }
        private void btnDeployGameserver_Click(object sender, EventArgs e)
        {
            //Query specific appID for all required data.
            ServerAPI.QUERY_DATA(ServerAPI.QUERY_STEAM_APPID(dropdownServerSelection.Text));

            //Determine where to deploy the server based on user input.
            if (txtboxDestinationFolder.Text == "")
            {
                DeploymentValues.deployment_directory = Environment.CurrentDirectory;
            }
            else
            {
                DeploymentValues.deployment_directory = txtboxDestinationFolder.Text;
            }

            //Determine whether or not to verify integrity of the installation.
            if (chkVerifyIntegrity.Value == true)
            {
                DeploymentValues.verify_integrity = " +validate";
            }
            else
            {
                DeploymentValues.verify_integrity = "";
            }

            switch (ServerAPI.QUERY_JOBJECT.bsm_integration)
            {
                case "none":
                    if (MetroMessageBox.Show(BorealisServerManager.ActiveForm, "Type of GameServer: [" + dropdownServerSelection.Text + "]\n" + "Deploy to: [" + DeploymentValues.deployment_directory + "]" + "\n\nWARNING: This gameserver currently has NO BGM support.\nYou can deploy it, but BGM cannot configure or control it at this time.", "Deploy GameServer?", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) == DialogResult.Yes)
                    {
                        btnCancelDeployGameserver.Visible = true;
                        btnDeployGameserver.Enabled = false;
                        DeployGameServer();
                    }
                    break;
                case "partial":
                    if (MetroMessageBox.Show(BorealisServerManager.ActiveForm, "Type of GameServer: [" + dropdownServerSelection.Text + "]\n" + "Deploy to: [" + DeploymentValues.deployment_directory + "]" + "\n\nWARNING: This gameserver currently has PARTIAL BGM support.\nYou can deploy it, but BGM can only configure it at this time, you have no ability to control it directly through BGM.", "Deploy GameServer?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                    {
                        btnCancelDeployGameserver.Visible = true;
                        btnDeployGameserver.Enabled = false;
                        DeployGameServer();
                    }
                    break;
                case "full":
                    if (MetroMessageBox.Show(BorealisServerManager.ActiveForm, "Type of GameServer: [" + dropdownServerSelection.Text + "]\n" + "Deploy to: [" + DeploymentValues.deployment_directory + "]", "Deploy GameServer?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        btnCancelDeployGameserver.Visible = true;
                        btnDeployGameserver.Enabled = false;
                        DeployGameServer();
                    }
                    break;
            }
        }
        private void btnCancelDeployGameserver_Click(object sender, EventArgs e)
        {
            if (ServerAPI.QUERY_JOBJECT.steamcmd_required == "True")
            {
                ExecuteWithRedirect(@"C:\Windows\System32\taskkill", "/F /IM steamcmd.exe");
                btnCancelDeployGameserver.Visible = false;
            }
            else
            {
                //Kill program being used to deploy a server.
            }
            btnDeployGameserver.Enabled = true;
            progressbarDownloadProgressOverall.Value = 0;
        }
    }
 }