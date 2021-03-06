﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using InstagramDownloaderV2.Classes.Downloader;
using InstagramDownloaderV2.Classes.Objects.OtherObjects;
using InstagramDownloaderV2.Classes.Requests;
using InstagramDownloaderV2.Classes.Settings;
using InstagramDownloaderV2.Classes.Validation;
using InstagramDownloaderV2.Enums;
using InstaSharper.API;
using InstaSharper.API.Builder;
using InstaSharper.Classes;

namespace InstagramDownloaderV2.Forms
{
    public partial class frmMain : Form
    {
        #region Properties
        private IInstaApi _instaApi;
        private readonly HttpClientHandler _httpClientHandler;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        private ProxyObject _proxy;
        private bool _isLogged;
#endregion

        #region Constructor
        public frmMain()
        {
            InitializeComponent();
            _httpClientHandler = new HttpClientHandler();
            _isLogged = false;
        }
#endregion

        #region Menu - File
        /// <summary>
        /// Update the software.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Add this part later on.
            // Process.Start("url to new version for now");
        }

        /// <summary>
        /// Exit the software
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        #endregion

        #region Menu - Filters
        // Reset all filter settings
        private void resetAllFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!InputValidation.ConfirmUserAction(resetAllFilterToolStripMenuItem)) return;

            foreach (Control c in gbMediaFilters.Controls)
            {
                if (c is CheckBox cb)
                    cb.Checked = false;
                if (c is TextBox tb)
                    tb.Clear();
                if (c is ComboBox comboBox)
                    comboBox.Text = "";
            }
        }
        #endregion

        #region ContextMenuStrip Events For ListView Input
        /// <summary>
        /// Remove selected row(s)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var selectedItems = lvInput.SelectedItems;

            for (int i = selectedItems.Count - 1; i >= 0; i--)
            {
                lvInput.Items.Remove(selectedItems[i]);
            }
        }

        /// <summary>
        /// Remove all rows
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            lvInput.Items.Clear();
        }

        /// <summary>
        /// Edit selected row
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editSelectedRowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lvInput.SelectedItems.Count != 1)
            {
                MessageBox.Show(@"Can't edit more/less than one row at the same time.");
                return;
            }
            using (EditInputRow editInput = new EditInputRow(lvInput))
            {
                editInput.ShowDialog();
            }
        }

        /// <summary>
        /// Export selected row(s)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exportSelectedRowsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!(lvInput.SelectedItems.Count > 0)) return;

            var output = InputValidation.ValidExportFile();

            if (output.Item1 == false) return;

            var outputFile = output.Item2;

            foreach (ListViewItem row in lvInput.SelectedItems)
            {
                var fileContent =
                    row.SubItems[0].Text + "|" +
                    row.SubItems[1].Text + "|" +
                    row.SubItems[2].Text + Environment.NewLine;

                File.AppendAllText(outputFile, fileContent);
            }
        }

        /// <summary>
        /// Export all row(s)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exportAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!(lvInput.Items.Count > 0)) return;

            var output = InputValidation.ValidExportFile();

            if (output.Item1 == false) return;

            var outputFile = output.Item2;

            foreach (ListViewItem row in lvInput.Items)
            {
                var fileContent =
                    row.SubItems[0].Text + "|" +
                    row.SubItems[1].Text + "|" +
                    row.SubItems[2].Text + Environment.NewLine;

                File.AppendAllText(outputFile, fileContent);
            }
        }
        #endregion

        #region User Input
        /// <summary>
        /// Adds a new input to the list view control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddInput_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtInput.Text)) return;
            if (rbUsername.Checked && !InputValidation.IsValidInstagramUsername(txtInput.Text)) return;

            if (rbUrl.Checked) lvInput.Items.Add(new ListViewItem(new[] { "Url", txtInput.Text, "1"}));
            if (rbMediaId.Checked) lvInput.Items.Add(new ListViewItem(new[] { "MediaId", txtInput.Text, "1" }));
            if (rbUsername.Checked) lvInput.Items.Add(new ListViewItem(new[] {"Username", txtInput.Text, numTotalDownloads.Text}));
            if (rbUserId.Checked) lvInput.Items.Add(new ListViewItem(new[] { "UserId", txtInput.Text, numTotalDownloads.Text }));
            if (rbHashtag.Checked) lvInput.Items.Add(new ListViewItem(new[] { "Hashtag", txtInput.Text, numTotalDownloads.Text }));
            if (rbLocation.Checked) lvInput.Items.Add(new ListViewItem(new[] { "Location", txtInput.Text, numTotalDownloads.Text }));

            var noInputMethodSelected = gbInputMethod.Controls.OfType<RadioButton>().Any(rb => rb.Checked);

            if (!noInputMethodSelected)
            {
                MessageBox.Show(@"Please select an input method and try again.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            txtInput.Clear();
            txtInput.Focus();
        }

        /// <summary>
        /// Clears all input (text box and list view).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClearAllInput_Click(object sender, EventArgs e)
        {
            if (!InputValidation.ConfirmUserAction(btnClearAllInput)) return;

            txtInput.Clear();
            lvInput.Items.Clear();
        }
        #endregion

        #region Download Process
        /// <summary>
        /// Starts the download process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnStartDownloading_Click(object sender, EventArgs e)
        {
            // Input validation
            if (!InputValidation.ValidateWebSettings(txtUserAgent.Text, txtRequestTimeout.Text, txtProxy.Text, ':', txtThreads.Text)) return;
            if (!InputValidation.ValidateDownloadSettings(txtDownloadFolder.Text, cbSaveStats.Checked, txtDelimiter.Text)) return;

            if (lvInput.Items.Count == 0)
            {
                Log("No input found to download. Please enter at least one and try again.", nameof(LogType.Error));
                return;
            }

            if (!InputValidation.IsInt(txtThreads.Text))
            {
                Log("Invalid threads input. Fix your threads input and try again.", nameof(LogType.Error));
                return;
            }

            // Proxy initialization
            _proxy = new ProxyObject(txtProxy.Text, ':');

            // Filters initialization
            var descriptionStrings = new List<string>();
            descriptionStrings.AddRange(txtSkipDescriptionStrings.Lines);

            var mediaFilter = new MediaFilter
            {
                SkipTopPosts = cbSkipTopPosts.Checked,
                SkipMediaIfVideo = cbSkipVideos.Checked,
                SkipMediaIfPhoto = cbSkipPhotos.Checked,
                SkipMediaComments = cbSkipMediaComments.Checked,
                SkipMediaCommentsMore = cbSkipMediaCommentsMoreLess.Text == @"more",
                SkipMediaCommentsCount = !String.IsNullOrEmpty(txtSkipMediaCommentsCount.Text) ? int.Parse(txtSkipMediaCommentsCount.Text) : 0,
                SkipMediaLikes = cbSkipMediaLikes.Checked,
                SkipMediaLikesMore = cbSkipMediaLikesMoreLess.Text == @"more",
                SkipMediaLikesCount = !String.IsNullOrEmpty(txtSkipMediaLikesCount.Text) ? int.Parse(txtSkipMediaLikesCount.Text) : 0,
                SkipMediaIfDescriptionContans = cbSkipMediaDescription.Checked,
                DescriptionStrings = descriptionStrings,
                SkipMediaUploadDateEnabled = cbSkipMediaUploadDate.Checked,
                SkipMediaUploadDateNewer = cbSkipMediaUploadDateMoreLess.Text == @"newer",
                SkipMediaUploadDate = dtUploadTime.Value,
                //SkipMediaUploadDate = ((DateTimeOffset)dtUpladTime.Value).ToUnixTimeSeconds(),
                SkipMediaVideoViews = cbSkipVideoViews.Checked,
                SkipMediaVideoViewsMore = cbSkipVideoViewsMoreLess.Text == @"more",
                SkipMediaVideoViewsCount = !String.IsNullOrEmpty(txtSkipVideoViewsCount.Text) ? int.Parse(txtSkipVideoViewsCount.Text) : 0,
                CustomFolder = cbCreateNewFolder.Checked,
                SaveStatsInCsvFile = cbSaveStats.Checked
            };

            if (!InputValidation.ValidateFilters(mediaFilter))
            {
                Log("Error detected in the filters. Please check your filter settings and try again.", nameof(LogType.Error));
                return;
            }

            // Download process
            if (!InputValidation.IsDouble(txtRequestTimeout.Text)) return;

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            var requestTimeout = double.Parse(txtRequestTimeout.Text);

            // Initialize downloader object
            var downloader = new InstagramDownload(_instaApi, mediaFilter, txtUserAgent.Text, _proxy.GetWebProxy(), requestTimeout,
                txtDownloadFolder.Text, _cancellationToken, txtDelimiter.Text)
            {
                IsTotalDownloadsEnabled = cbTotalDownloads.Checked
            };

            // Set downloader properties
            if (!string.IsNullOrEmpty(txtTotalDownloads.Text)) downloader.TotalDownloads = int.Parse(txtTotalDownloads.Text);
            downloader.CustomFolder = cbCreateNewFolder.Checked;

            // Update form controls
            btnStartDownloading.Enabled = false;
            btnStopDownloading.Enabled = true;

            // Upload logs
            Log(@"Started downloading...", nameof(LogType.Success));

            foreach (ListViewItem item in lvInput.Items)
            {
                try
                {
                    switch (item.SubItems[0].Text)
                    {
                        case "Url":
                            try
                            {
                                await downloader.Download(item.SubItems[1].Text, InputType.Url, item.SubItems[2].Text);
                            }
                            catch (OperationCanceledException ex)
                            {
                                Log(ex.Message, nameof(LogType.Error));
                            }

                            break;
                        case "MediaId":
                            try
                            {
                                await downloader.Download(item.SubItems[1].Text, InputType.MediaId, item.SubItems[2].Text);
                            }
                            catch (OperationCanceledException ex)
                            {
                                Log(ex.Message, nameof(LogType.Error));
                            }

                            break;
                        case "Username":
                            try
                            {
                                await downloader.Download(item.SubItems[1].Text, InputType.Username, item.SubItems[2].Text);
                            }
                            catch (OperationCanceledException ex)
                            {
                                Log(ex.Message, nameof(LogType.Error));
                            }

                            break;
                        case "UserId":
                            try
                            {
                                await downloader.Download(item.SubItems[1].Text, InputType.UserId, item.SubItems[2].Text);
                            }
                            catch (OperationCanceledException ex)
                            {
                                Log(ex.Message, nameof(LogType.Error));
                            }

                            break;
                        case "Hashtag":
                            try
                            {
                                await downloader.Download(item.SubItems[1].Text, InputType.Hashtag, item.SubItems[2].Text);
                            }
                            catch (OperationCanceledException ex)
                            {
                                Log(ex.Message, nameof(LogType.Error));
                            }

                            break;
                        case "Location":
                            try
                            {
                                await downloader.Download(item.SubItems[1].Text, InputType.Location, item.SubItems[2].Text);
                            }
                            catch (OperationCanceledException ex)
                            {
                                Log(ex.Message, nameof(LogType.Error));
                            }

                            break;

                    }
                }
                catch (OperationCanceledException ex)
                {
                    Log(ex.Message, nameof(LogType.Error));
                }
            }

            btnStartDownloading.Enabled = true;
            btnStopDownloading.Enabled = false;

            Log(@"Finished downloading...", nameof(LogType.Success));

            // Start all tasks
            //using (var semaphore = new SemaphoreSlim(int.Parse(txtThreads.Text)))
            //{
            //    var tasks = new List<Task>();
            //    foreach (ListViewItem item in lvInput.Items)
            //    {
            //        await semaphore.WaitAsync();

            //        try
            //        {
            //            tasks.Add(
            //                Task.Run(async () =>
            //                {
            //                    switch (item.SubItems[0].Text)
            //                    {
            //                        case "Url":
            //                            try
            //                            {
            //                                await downloader.Download(item.SubItems[1].Text, InputType.Url,
            //                                    mediaFilter);
            //                            }
            //                            catch (OperationCanceledException ex)
            //                            {
            //                                Log(ex.Message, nameof(LogType.Error));
            //                            }

            //                            break;
            //                        case "Username":
            //                            try
            //                            {
            //                                await downloader.Download(item.SubItems[1].Text, InputType.Username,
            //                                    mediaFilter, item.SubItems[2].Text);
            //                            }
            //                            catch (OperationCanceledException ex)
            //                            {
            //                                Log(ex.Message, nameof(LogType.Error));
            //                            }

            //                            break;
            //                        case "Hashtag":
            //                            try
            //                            {
            //                                await downloader.Download(item.SubItems[1].Text, InputType.Hashtag,
            //                                    mediaFilter, item.SubItems[2].Text);
            //                            }
            //                            catch (OperationCanceledException ex)
            //                            {
            //                                Log(ex.Message, nameof(LogType.Error));
            //                            }

            //                            break;
            //                        case "Location":
            //                            try
            //                            {
            //                                await downloader.Download(item.SubItems[1].Text, InputType.Location,
            //                                    mediaFilter, item.SubItems[2].Text);
            //                            }
            //                            catch (OperationCanceledException ex)
            //                            {
            //                                Log(ex.Message, nameof(LogType.Error));
            //                            }

            //                            break;
            //                    }
            //                }, _cancellationToken)
            //            );
            //        }
            //        catch (Exception ex)
            //        {
            //            Log(ex.Message, nameof(LogType.Error));
            //        }
            //        finally
            //        {
            //            semaphore.Release();
            //        }
            //    }

            //    // Wait for tasks to finish
            //    try
            //    {
            //        await Task.WhenAll(tasks); // might throw an exception if something goes wrong during tasks
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.StackTrace);
            //    }

            //    Log(@"Finished downloading.", nameof(LogType.Success));

            //    // Update form controls when tasks are finished
            //    btnStartDownloading.Enabled = true;
            //    btnStopDownloading.Enabled = false;
            //}
        }

        /// <summary>
        /// Stops the download process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStopDownloading_Click(object sender, EventArgs e)
        {
            if (!InputValidation.ConfirmUserAction(btnStopDownloading)) return;

            _cancellationTokenSource.Cancel();
            btnStartDownloading.Enabled = true;
            btnStopDownloading.Enabled = false;
        }
        #endregion

        #region Logs
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logType"></param>
        public void Log(string message, string logType)
        {
            Invoke((MethodInvoker)delegate
            {
                txtLogs.AppendText($@"{DateTime.Now} - {logType}: {message}{Environment.NewLine}");
            });
        }

        /// <summary>
        /// Clears all the logs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClearLogs_Click(object sender, EventArgs e)
        {
            txtLogs.Clear();
        }

        /// <summary>
        /// Exports the logs to a file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExportLogs_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog
            {
                Filter = @"Text files (.txt)|*.txt",
                Title = @"Logs Export File",
                RestoreDirectory = true
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllLines(sfd.FileName, txtLogs.Lines);
                }
            }
        }
        #endregion

        #region Load from file methods
        /// <summary>
        /// Loads input to list view from file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLoadInputFromFile_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = @"Text Files (*.txt)|*.txt|CSV Files (.csv)|*.csv" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Log(@"Attempting to load input from file...", nameof(LogType.Info));
                    var lines = File.ReadAllLines(ofd.FileName);
                    var allowedType = new List<string>
                    {
                        "Url",
                        "MediaId",
                        "Username",
                        "UserId",
                        "Hashtag",
                        "Location"
                    };

                    foreach (var line in lines)
                    {
                        if (line.Split('|').Length == 2)
                        {
                            if (allowedType.Any(type => line.Split('|')[0].Contains(type)))
                            {
                                lvInput.Items.Add(new ListViewItem(new[] { line.Split('|')[0], line.Split('|')[1], line.Split('|')[0] == "Url" ? "1" : "0" }));
                                Log($@"Successfully added input '{line.Split('|')[1]}' of type '{line.Split('|')[0]}'", nameof(LogType.Success));
                            }
                            else
                            {
                                Log($@"Failed to load string '{line}' due to bad type...", nameof(LogType.Fail));
                            }
                        }
                        else if (line.Split('|').Length == 3)
                        {
                            if (allowedType.Any(type => line.Split('|')[0].Contains(type)))
                            {
                                lvInput.Items.Add(new ListViewItem(new[] { line.Split('|')[0], line.Split('|')[1], line.Split('|')[2] }));
                                Log($@"Successfully added input '{line.Split('|')[1]}' of type '{line.Split('|')[0]}'", nameof(LogType.Success));
                            }
                            else
                            {
                                Log($@"Failed to load string '{line}' due to bad type...", nameof(LogType.Fail));
                            }
                        }
                        else
                        {
                            Log($@"Failed to load line '{line}' due to bad format...", nameof(LogType.Fail));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Load description strings for filter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLoadDescriptionStrings_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd =
                new OpenFileDialog
                {
                    Filter = @"Text Files (*.txt)|*.txt|CSV Files (.csv)|*.csv"
                })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Log(@"Attempting to load input from file...", nameof(LogType.Info));
                    string[] lines = File.ReadAllLines(ofd.FileName);

                    foreach (string s in lines)
                    {
                        txtSkipDescriptionStrings.AppendText(s + Environment.NewLine);
                    }

                    // remove the last line after importing (because it'll be an empty cause of new line)
                    txtSkipDescriptionStrings.Text = txtSkipDescriptionStrings.Text.Remove(txtSkipDescriptionStrings.Text.Length - 1);
                    Log(@"Successfully loaded input from file...", nameof(LogType.Info));
                }
            }
        }
        #endregion

        #region RadioButton Events
        // Make sure that skip photos is unchecked if skip video is checked
        private void cbSkipVideos_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSkipVideos.Checked)
            {
                cbSkipPhotos.Checked = false;
            }
        }

        // Make sure that skip videos is unchecked if skip photos is checked
        private void cbSkipPhotos_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSkipPhotos.Checked)
            {
                cbSkipVideos.Checked = false;
            }
        }

        // Disable total downloads if url is checked, because it's only one anyways.
        private void rbUrl_CheckedChanged(object sender, EventArgs e)
        {
            if (rbUrl.Checked)
            {
                lblTotalDownloads.Enabled = false;
                numTotalDownloads.Enabled = false;
            }
            else
            {
                lblTotalDownloads.Enabled = true;
                numTotalDownloads.Enabled = true;
            }
        }

        // Disable total downloads if media Id is checked, because it's only one anyways.
        private void rbMediaId_CheckedChanged(object sender, EventArgs e)
        {
            if (rbMediaId.Checked)
            {
                lblTotalDownloads.Enabled = false;
                numTotalDownloads.Enabled = false;
            }
            else
            {
                lblTotalDownloads.Enabled = true;
                numTotalDownloads.Enabled = true;
            }
        }
        #endregion

        #region LOGIN
        /// <summary>
        /// Hide password characters if enabled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbHidePassword_CheckedChanged(object sender, EventArgs e)
        {
            txtAccountPassword.PasswordChar = cbHidePassword.Checked ? '*' : '\0';
        }

        /// <summary>
        /// Attempts to log in to an Instagram account.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnAccountLogin_Click(object sender, EventArgs e)
        {
            btnAccountLogin.Enabled = false;

            // Proxy initialization
            _proxy = new ProxyObject(txtProxy.Text, ':');

            if (!string.IsNullOrEmpty(txtProxy.Text))
            {
                _httpClientHandler.Proxy = _proxy.GetWebProxy();
            }

            _instaApi = InstaApiBuilder.CreateBuilder()
                .UseHttpClientHandler(_httpClientHandler)
                .SetUser(new UserSessionData
                {
                    UserName = txtAccountUsername.Text,
                    Password = txtAccountPassword.Text
                })
                .Build();

            lblAccountLoginStatus.Text = @"Status: Attempting to log in.";
            var login = await _instaApi.LoginAsync();

            if (login.Succeeded)
            {
                _isLogged = true;
                gbDownload.Enabled = true;
                lblAccountLoginStatus.Text = @"Status: Logged in.";
                lblAccountLoginStatus.ForeColor = Color.Green;
                Log($@"Successfully logged in as {txtAccountUsername.Text}.", nameof(LogType.Success));
            }
            else
            {
                lblAccountLoginStatus.Text = @"Status: Failed to log in.";
                lblAccountLoginStatus.ForeColor = Color.Red;
                Log($@"Failed to log in as {txtAccountUsername.Text}. Message: {login.Info.Message}", nameof(LogType.Fail));

                btnAccountLogin.Enabled = true;
                return;
            }

            btnAccountLogin.Enabled = false;
            btnAccountLogout.Enabled = true;
        }

        /// <summary>
        /// Logs out an account (sets cookies to null)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnAccountLogout_Click(object sender, EventArgs e)
        {
            var logout = await _instaApi.LogoutAsync();

            btnAccountLogout.Enabled = false;
            if (logout.Succeeded)
            {
                _isLogged = false;
                lblAccountLoginStatus.Text = @"Status: Successfully logged out.";
                lblAccountLoginStatus.ForeColor = Color.DodgerBlue;
                Log($@"Successfully logged out as {txtAccountUsername.Text}.", nameof(LogType.Fail));
                txtAccountUsername.Clear();
                txtAccountPassword.Clear();
                btnAccountLogin.Enabled = true;

                gbDownload.Enabled = false;
            }
            else
            {
                btnAccountLogout.Enabled = true;
                lblAccountLoginStatus.Text = @"Status: Failed to log out.";
                await Task.Delay(2000);
                lblAccountLoginStatus.Text = @"Status: Successfully logged in.";
                lblAccountLoginStatus.ForeColor = Color.Green;
            }
        }

        #endregion

        #region SETTINGS
        /// <summary>
        /// Generates a random UA and sets it to the UA textbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRandomizeUserAgent_Click(object sender, EventArgs e)
        {
            txtUserAgent.Text = UserAgentGenerator.Generate();
        }

        /// <summary>
        /// Sets the folder where downloaded images will be downloaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowseDownloadDirectory_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog()
            {
                ShowNewFolderButton = true,
                Description = @"Download folder for medias"
            })
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtDownloadFolder.Text = fbd.SelectedPath;
                    Log(@"Successfully initialized a download folder to save photos.", nameof(LogType.Success));
                }
            }
        }

        /// <summary>
        /// Main form load method - loads the settings from a file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void frmMain_Load(object sender, EventArgs e)
        {
            Log(@"Successfully initialized form components and loaded software.", nameof(LogType.Success));
            try
            {
                Log(@"Attempting to load application settings...", nameof(LogType.Info));

                var settings = SettingsSerialization.Load();

                txtUserAgent.Text = !String.IsNullOrEmpty(settings.UserAgent) ? settings.UserAgent : UserAgentGenerator.Generate();
                txtRequestTimeout.Text = !String.IsNullOrEmpty(settings.RequestTimeout) ? settings.RequestTimeout : "150";
                txtThreads.Text = !String.IsNullOrEmpty(settings.Threads) ? settings.Threads : "1";
                txtProxy.Text = settings.Proxy;
                txtDownloadFolder.Text = !String.IsNullOrEmpty(settings.DownloadFolder) ? settings.DownloadFolder : Application.StartupPath;
                cbCreateNewFolder.Checked = settings.CreateNewFolder;
                cbSaveStats.Checked = settings.SaveStats;
                txtDelimiter.Text = settings.Delimiter;
                cbSkipMediaDescription.Checked = settings.SkipDescription;
                cbSkipPhotos.Checked = settings.SkipPhotos;
                cbSkipVideos.Checked = settings.SkipVideos;
                cbSkipMediaLikes.Checked = settings.SkipLikes;
                cbSkipMediaLikesMoreLess.Text = settings.SkipLikesMoreLess;
                txtSkipMediaLikesCount.Text = settings.SkipLikesCount;
                cbSkipMediaComments.Checked = settings.SkipComments;
                cbSkipMediaCommentsMoreLess.Text = settings.SkipCommentsMoreLess;
                txtSkipMediaCommentsCount.Text = settings.SkipCommentsCount;
                cbSkipMediaUploadDate.Checked = settings.SkipUploadDate;
                cbTotalDownloads.Checked = settings.TotalDownloadsEnabled;
                txtTotalDownloads.Text = settings.TotalDownloads;
                cbSkipTopPosts.Checked = settings.SkipTopPosts;
                //txtAccountUsername.Text = settings.AccountUsername;
                //txtAccountPassword.Text = settings.AccountPassword;
                cbHidePassword.Checked = settings.HidePassword;

                if (!string.IsNullOrEmpty(settings.AccountPassword) || !string.IsNullOrEmpty(settings.AccountPassword))
                {
                    _instaApi = InstaApiBuilder.CreateBuilder()
                        .UseHttpClientHandler(_httpClientHandler)
                        .SetUser(new UserSessionData
                        {
                            UserName = settings.AccountUsername,
                            Password = settings.AccountPassword
                        })
                        .Build();

                    txtAccountUsername.Text = settings.AccountUsername;
                    txtAccountPassword.Text = settings.AccountPassword;

                    if (settings.StateData != null) _instaApi.LoadStateDataFromStream(settings.StateData);
                }

                if (_instaApi != null)
                {
                    gbDownload.Enabled = true;
                    _isLogged = true;

                    lblAccountLoginStatus.Text = @"Status: Successfully restored session.";
                    lblAccountLoginStatus.ForeColor = Color.Green;
                    Log($@"Successfully restored session as {settings.AccountUsername}.", nameof(LogType.Success));
                    btnAccountLogin.Enabled = false;
                    btnAccountLogout.Enabled = true;
                }
                else
                {
                    lblAccountLoginStatus.Text = @"Status: Failed to restore session.";
                    lblAccountLoginStatus.ForeColor = Color.Red;
                    Log($@"Failed to restore session.", nameof(LogType.Fail));
                    btnAccountLogin.Enabled = true;
                    btnAccountLogout.Enabled = false;
                }

                Log(@"Successfully loaded application settings.", nameof(LogType.Success));
            }
            catch (Exception ex)
            {
                txtUserAgent.Text = UserAgentGenerator.Generate();
                txtRequestTimeout.Text = @"150";
                txtThreads.Text = @"1";
                txtDownloadFolder.Text = Application.StartupPath;
                Log(ex.Message, nameof(LogType.Error));
            }

            try
            {
                using (var request = new Request(txtUserAgent.Text, null, double.Parse(txtRequestTimeout.Text)))
                {
                    var response =
                        await request.GetRequestResponseAsync("http://imristo.com/download/igdownloader/changelog.txt");
                    if (response.IsSuccessStatusCode)
                    {
                        txtChangelog.Text = await response.Content.ReadAsStringAsync();
                    }

                    response = await request.GetRequestResponseAsync(
                        "http://imristo.com/download/igdownloader/version.txt");
                    if (response.IsSuccessStatusCode)
                    {
                        lblLatestVersion.Text += await response.Content.ReadAsStringAsync();
                    }

                    lblCurrentVersion.Text += Application.ProductVersion;

                }
            }
            catch (HttpRequestException)
            {
                Log(@"Failed to obtain changelog and latest version. Please check your Internet connection.", nameof(LogType.Error));
            }
        }

        /// <summary>
        /// Main form closing - saves the settings to a file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            var settings = new Settings
            {
                UserAgent = txtUserAgent.Text,
                RequestTimeout = txtRequestTimeout.Text,
                Proxy = txtProxy.Text,
                Threads = txtThreads.Text,
                DownloadFolder = txtDownloadFolder.Text,
                CreateNewFolder = cbCreateNewFolder.Checked,
                SaveStats = cbSaveStats.Checked,
                Delimiter = txtDelimiter.Text,
                SkipDescription = cbSkipMediaDescription.Checked,
                SkipPhotos = cbSkipPhotos.Checked,
                SkipVideos = cbSkipVideos.Checked,
                SkipLikes = cbSkipMediaLikes.Checked,
                SkipLikesMoreLess = cbSkipMediaLikesMoreLess.Text,
                SkipLikesCount = txtSkipMediaLikesCount.Text,
                SkipComments = cbSkipMediaComments.Checked,
                SkipCommentsMoreLess = cbSkipMediaCommentsMoreLess.Text,
                SkipCommentsCount = txtSkipMediaCommentsCount.Text,
                SkipUploadDate = cbSkipMediaUploadDate.Checked,
                TotalDownloadsEnabled = cbTotalDownloads.Checked,
                TotalDownloads = txtTotalDownloads.Text,
                SkipTopPosts = cbSkipTopPosts.Checked,
                AccountUsername = _isLogged == false ? string.Empty : txtAccountUsername.Text,
                AccountPassword = _isLogged == false ? string.Empty : txtAccountPassword.Text,
                HidePassword = cbHidePassword.Checked,
                StateData = _instaApi?.GetStateDataAsStream()
            };
            SettingsSerialization.Save(settings);
        }

        // Credits
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://imristo.com");
        }

        // Credits
        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://smmnova.com");
        }

        #endregion

        private void helpSupportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new HowToUse())
            {
                frm.ShowDialog();
            }
        }

    }
}
