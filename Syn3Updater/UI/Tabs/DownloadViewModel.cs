﻿using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Cyanlabs.Syn3Updater.Helper;
using Cyanlabs.Syn3Updater.Model;
using Cyanlabs.Syn3Updater.Services;
using ModernWpf.Controls;

namespace Cyanlabs.Syn3Updater.UI.Tabs
{
    internal class DownloadViewModel : LanguageAwareBaseViewModel
    {
        #region Events

        public event EventHandler<EventArgs<int>> PercentageChanged;

        #endregion

        #region Constructors

        private ActionCommand _cancelButton;
        public ActionCommand CancelButton => _cancelButton ??= new ActionCommand(CancelAction);
        private CancellationTokenSource _tokenSource = new();

        #endregion

        #region Properties & Fields

        private int _currentProgress, _totalPercentage, _totalPercentageMax, _count;
        private string _downloadInfo, _downloadPercentage, _log, _selectedRelease, _selectedRegion, _selectedMapVersion, _progressBarSuffix, _installMode, _action, _my20Mode;
        private bool _cancelButtonEnabled;
        private Task _downloadTask;
        private FileHelper _fileHelper;
        private CancellationToken _ct;

        private ObservableCollection<string> _downloadQueueList;

        public ObservableCollection<string> DownloadQueueList
        {
            get => _downloadQueueList;
            set => SetProperty(ref _downloadQueueList, value);
        }

        public bool CancelButtonEnabled
        {
            get => _cancelButtonEnabled;
            set => SetProperty(ref _cancelButtonEnabled, value);
        }

        public string InstallMode
        {
            get => _installMode;
            set => SetProperty(ref _installMode, value);
        }

        private string _installModeForced;

        public string InstallModeForced
        {
            get => _installModeForced;
            set => SetProperty(ref _installModeForced, value);
        }

        public int CurrentProgress
        {
            get => _currentProgress;
            set => SetProperty(ref _currentProgress, value);
        }

        public string DownloadPercentage
        {
            get => _downloadPercentage;
            set => SetProperty(ref _downloadPercentage, value);
        }

        public int TotalPercentage
        {
            get => _totalPercentage;
            set => SetProperty(ref _totalPercentage, value);
        }

        public int TotalPercentageMax
        {
            get => _totalPercentageMax;
            set => SetProperty(ref _totalPercentageMax, value);
        }

        public string DownloadInfo
        {
            get => _downloadInfo;
            set => SetProperty(ref _downloadInfo, value);
        }

        public string Log
        {
            get => _log;
            set => SetProperty(ref _log, value);
        }

        public string My20Mode
        {
            get => _my20Mode;
            set => SetProperty(ref _my20Mode, value);
        }

        #endregion

        #region Methods

        public void Init()
        {
            if (!AppMan.App.IsDownloading || _downloadTask?.Status.Equals(TaskStatus.Running) == true) return;
            Log = string.Empty;
            _selectedRelease = AppMan.App.SelectedRelease;
            _selectedRegion = AppMan.App.SelectedRegion;
            _selectedMapVersion = AppMan.App.SelectedMapVersion;
            string text = $"Selected Region: {_selectedRegion} - Release: {_selectedRelease} - Map Version: {_selectedMapVersion}";
            Log += $"[{DateTime.Now}] {text} {Environment.NewLine}";

            InstallMode = AppMan.App.Settings.InstallMode;
            My20Mode = AppMan.App.Settings.My20 ? LM.GetValue("String.Enabled") : LM.GetValue("String.Disabled");
            InstallModeForced = AppMan.App.ModeForced ? LM.GetValue("String.Yes") : LM.GetValue("String.No");
            _action = AppMan.App.Action;

            text = $"Install Mode: {InstallMode} Forced: {AppMan.App.ModeForced}";
            Log += $"[{DateTime.Now}] {text} {Environment.NewLine}";
            AppMan.Logger.Info(text);

            text = $"MY20 Protection: {AppMan.App.Settings.My20}";
            Log += $"[{DateTime.Now}] {text} {Environment.NewLine}";
            AppMan.Logger.Info(text);

            CancelButtonEnabled = true;
            CurrentProgress = 0;

            PercentageChanged += DownloadPercentageChanged;

            DownloadQueueList = new ObservableCollection<string>();
            foreach (SModel.Ivsu item in AppMan.App.Ivsus)
                DownloadQueueList.Add(item.Url);

            _ct = _tokenSource.Token;

            _fileHelper = new FileHelper(PercentageChanged);

            _downloadTask = Task.Run(DoDownload, _tokenSource.Token).ContinueWith(async t =>
            {
                if (t.IsFaulted)
                {
                    bool userError = false;
                    if (t.Exception != null)
                    {
                        foreach (Exception exception in t.Exception.InnerExceptions)
                        {
                            if (exception.GetType().IsAssignableFrom(typeof(IOException))) userError = true;
                            break;
                        }

                        if (t.Exception != null && !userError)
                            Application.Current.Dispatcher.Invoke(() => AppMan.Logger.CrashWindow(t.Exception.InnerExceptions.FirstOrDefault()));
                    }

                    CancelAction();
                }

                if (t.IsCompleted && !t.IsFaulted)
                    await DownloadComplete();
            }, _tokenSource.Token);
        }

        private async Task DoDownload()
        {
            _count = 0;
            TotalPercentageMax = 100 * AppMan.App.Ivsus.Count * 4;

            foreach (SModel.Ivsu item in AppMan.App.Ivsus)
            {
                //debugging
                //item.Url = item.Url.Replace("https://ivsubinaries.azureedge.net/", "http://127.0.0.1/").Replace("https://ivsu.binaries.ford.com/", "http://127.0.0.1/");
                if (_ct.IsCancellationRequested)
                {
                    Log += $"[{DateTime.Now}] Process cancelled by user{Environment.NewLine}";
                    AppMan.Logger.Info("Process cancelled by user");
                    return;
                }

                if (await ValidateFile(item.Url, AppMan.App.DownloadPath + item.FileName, item.Md5, false, true))
                {
                    string text = $"Validated: {item.FileName} (Skipping Download)";
                    Log += $"[{DateTime.Now}] {text} {Environment.NewLine}";
                    AppMan.Logger.Info(text);

                    if (item.Source == @"naviextras")
                    {
                        FileHelper.OutputResult outputResult = await _fileHelper.ExtractMultiPackage(item, _ct);

                        text = $"Extracting: {item.FileName} (This may take some time!)";
                        Log += $"[{DateTime.Now}] {text} {Environment.NewLine}";
                        AppMan.Logger.Info(text);

                        if (outputResult.Result)
                        {
                            Log += "[" + DateTime.Now + "] " + outputResult.Message + Environment.NewLine;
                            AppMan.Logger.Info(outputResult.Message);
                        }
                    }

                    _count += 2;
                }
                else
                {
                    if (_ct.IsCancellationRequested) return;
                    DownloadInfo = $"{LM.GetValue("String.Downloading")}: {item.FileName}";

                    Log += "[" + DateTime.Now + "] " + $"Downloading: {item.FileName}" + Environment.NewLine;
                    AppMan.Logger.Info($"Downloading: {item.FileName}");

                    _progressBarSuffix = LM.GetValue("String.Downloaded");
                    try
                    {
                        for (int i = 1; i < 4; i++)
                        {
                            if (_ct.IsCancellationRequested) return;
                            string text;
                            if (i > 1)
                            {
                                DownloadInfo = $"{LM.GetValue("String.Downloading")} ({LM.GetValue("String.Attempt")} #{i}): {item.Url}";
                                Log += "[" + DateTime.Now + "] " + $"Downloading (Attempt #{i}): {item.FileName}" + Environment.NewLine;
                                AppMan.Logger.Info($"Downloading (Attempt #{i}): {item.FileName}");
                            }

                            if (await _fileHelper.DownloadFile(item.Url, AppMan.App.DownloadPath + item.FileName, _ct, AppMan.App.Settings.DownloadConnections))
                            {
                                _count++;
                            }
                            else
                            {
                                CancelAction();
                                break;
                            }

                            if (await ValidateFile(item.Url, AppMan.App.DownloadPath + item.FileName, item.Md5, false))
                            {
                                _count++;
                                text = $"Downloaded: {item.FileName}";
                                Log += $"[{DateTime.Now}] {text} {Environment.NewLine}";
                                AppMan.Logger.Info(text);
                                if (item.Source == @"naviextras")
                                {
                                    FileHelper.OutputResult outputResult = await _fileHelper.ExtractMultiPackage(item, _ct);

                                    text = $"Extracting: {item.FileName} (This may take some time!)";
                                    Log += $"[{DateTime.Now}] {text} {Environment.NewLine}";
                                    AppMan.Logger.Info(text);

                                    if (outputResult.Result)
                                    {
                                        Log += "[" + DateTime.Now + "] " + outputResult.Message + Environment.NewLine;
                                        AppMan.Logger.Info(outputResult.Message);
                                    }
                                }
                                break;
                            }

                            if (i == 3)
                            {
                                text = $"Unable to validate {item.FileName} after 3 attempts, ABORTING PROCESS!";
                                Log += $"[{DateTime.Now}] {text} {Environment.NewLine}";
                                AppMan.Logger.Info(text);

                                await Application.Current.Dispatcher.Invoke(() => UIHelper.ShowErrorDialog(LM.GetValue("MessageBox.FailedToValidate3")).ShowAsync());
                                CancelAction();
                                break;
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
                Application.Current.Dispatcher.Invoke(() => DownloadQueueList.Remove(item.Url));
            }
            Application.Current.Dispatcher.Invoke(() => DownloadQueueList.Clear());
        }

        private async Task DownloadComplete()
        {
            if (!_ct.IsCancellationRequested) await PrepareUsbAsync();
        }

        private async Task DoCopy()
        {
            foreach (SModel.Ivsu extraitem in AppMan.App.ExtraIvsus) AppMan.App.Ivsus.Add(extraitem);
            foreach (SModel.Ivsu item in AppMan.App.Ivsus)
            {
                if (_ct.IsCancellationRequested)
                {
                    Log += "[" + DateTime.Now + "] Process cancelled by user" + Environment.NewLine;
                    AppMan.Logger.Info("Process cancelled by user");
                    return;
                }

                if (item.Source == @"naviextras")
                {
                    _count++;
                    continue;
                }

                if (await ValidateFile(AppMan.App.DownloadPath + item.FileName, $@"{AppMan.App.DriveLetter}\SyncMyRide\{item.FileName}", item.Md5,
                    true, true))
                {
                    string text = $"{item.FileName} exists and validated successfully, skipping copy";
                    Log += $"[{DateTime.Now}] {text} {Environment.NewLine}";
                    AppMan.Logger.Info(text);

                    _count += 2;
                }
                else
                {
                    if (_ct.IsCancellationRequested) return;

                    DownloadInfo = $"{LM.GetValue("String.Copying")}: {item.FileName}";

                    Log += $"[{DateTime.Now}] Copying: {item.FileName} {Environment.NewLine}";
                    AppMan.Logger.Info($"Copying: {item.FileName}");

                    _progressBarSuffix = LM.GetValue("String.Copied");

                    for (int i = 1; i < 4; i++)
                    {
                        if (_ct.IsCancellationRequested) return;
                        if (i > 1)
                        {
                            DownloadInfo = $"{LM.GetValue("String.Copying")} ({LM.GetValue("String.Attempt")} #{i}): {item.FileName}";
                            Log += $"[{DateTime.Now}] Copying (Attempt #{i}): {item.FileName} {Environment.NewLine}";
                            AppMan.Logger.Info($"Copying (Attempt #{i}): {item.FileName}");
                        }


                        if (await _fileHelper.CopyFileAsync(AppMan.App.DownloadPath + item.FileName, $@"{AppMan.App.DriveLetter}\SyncMyRide\{item.FileName}", _ct))
                        {
                            _count ++;
                        }
                        else
                        {
                            CancelAction();
                            break;
                        }
                        

                        if (await ValidateFile(AppMan.App.DownloadPath + item.FileName,
                            $@"{AppMan.App.DriveLetter}\SyncMyRide\{item.FileName}", item.Md5, true))
                        {
                            string text = $"Copied: {item.FileName}";
                            Log += $"[{DateTime.Now}] {text} {Environment.NewLine}";
                            AppMan.Logger.Info(text);
                            _count++;
                            break;
                        }

                        if (i == 3)
                        {
                            string text = $"unable to validate {item.FileName} after 3 tries, ABORTING PROCESS!";
                            Log += $"[{DateTime.Now}] {text} {Environment.NewLine}";
                            AppMan.Logger.Info(text);

                            await Application.Current.Dispatcher.Invoke(() => UIHelper.ShowErrorDialog(LM.GetValue("MessageBox.FailedToValidate3")).ShowAsync());
                            CancelAction();
                            break;
                        }
                    }
                }

                Application.Current.Dispatcher.Invoke(() => DownloadQueueList.Remove(AppMan.App.DownloadPath + item.FileName));
                PercentageChanged.Raise(this, 100, 0);
            }
        }

        private async void CopyComplete()
        {
            switch (_action)
            {
                case @"main":
                    switch (InstallMode)
                    {
                        case @"autoinstall":
                            CreateAutoInstall();
                            break;
                        case @"downgrade":
                        case @"reformat":
                            CreateReformat();
                            break;
                    }

                    break;
                case @"logutility":
                case @"logutilitymy20":
                case @"gracenotesremoval":
                case @"voiceshrinker":
                case @"downgrade":
                    CreateAutoInstall();
                    break;
            }

            CancelButtonEnabled = false;
            string text = AppMan.App.DownloadToFolder
                ? "ALL FILES DOWNLOADED AND COPIED TO THE SELECTED FOLDER SUCCESSFULLY!"
                : "ALL FILES DOWNLOADED AND COPIED TO THE USB DRIVE SUCCESSFULLY!";

            Log += $"[{DateTime.Now}] {text} {Environment.NewLine}";
            AppMan.Logger.Info(text);

            DownloadInfo = LM.GetValue("String.Completed");
            AppMan.App.IsDownloading = false;
            
            if (_action != "logutilitymy20")
            {
                ContentDialogResult result = await Application.Current.Dispatcher.Invoke(() => UIHelper
                    .ShowDialog(LM.GetValue("MessageBox.UploadLog"), "Syn3 Updater", LM.GetValue("Download.CancelButton"), LM.GetValue("String.Upload"))
                    .ShowAsync());
                USBHelper.GenerateLog(Log,result == ContentDialogResult.Primary);
            }
            
            if (_action == "main")
            {
                ContentDialogResult result = await Application.Current.Dispatcher.Invoke(() => UIHelper
                    .ShowDialog(string.Format(LM.GetValue("MessageBox.UpdateCurrentversion"), AppMan.App.SVersion, AppMan.App.SelectedRelease.Replace("Sync ", "")), "Syn3 Updater", LM.GetValue("String.No"), LM.GetValue("String.Yes"))
                    .ShowAsync());
                if (result == ContentDialogResult.Primary)
                {
                    AppMan.App.Settings.CurrentVersion =
                        Convert.ToInt32(AppMan.App.SelectedRelease.Replace(".", "").Replace("Sync ", ""));
                    AppMan.App.SVersion = AppMan.App.SelectedRelease.Replace("Sync ", "");
                }

                if (AppMan.App.DownloadToFolder)
                {
                    await Application.Current.Dispatcher.Invoke(() => UIHelper.ShowDialog(LM.GetValue("MessageBox.CompletedFolder"), "Syn3 Updater", LM.GetValue("String.OK")).ShowAsync());
                    Process.Start(AppMan.App.DriveLetter);
                }
                else
                {
                    await Application.Current.Dispatcher.Invoke(() => UIHelper.ShowDialog(LM.GetValue("MessageBox.Completed"), "Syn3 Updater", LM.GetValue("String.OK")).ShowAsync());
                    Process.Start($"https://cyanlabs.net/tutorials/windows-automated-method-update-to-3-4/#{InstallMode}");
                }

                AppMan.App.FireHomeTabEvent();
            }
            else if (_action == "logutility")
            {
                await Application.Current.Dispatcher.Invoke(() => UIHelper.ShowDialog(LM.GetValue("MessageBox.LogUtilityComplete"), "Syn3 Updater", LM.GetValue("String.OK")).ShowAsync());
                AppMan.App.UtilityCreateLogStep1Complete = true;
                AppMan.App.FireUtilityTabEvent();
            }
            else if (_action == "logutilitymy20")
            {
                await Application.Current.Dispatcher.Invoke(() => UIHelper.ShowDialog(LM.GetValue("MessageBox.LogUtilityCompleteMy20"), "Syn3 Updater", LM.GetValue("String.OK")).ShowAsync());
                USBHelper usbHelper = new();
                await usbHelper.LogParseXmlAction().ConfigureAwait(false);
                AppMan.App.UtilityCreateLogStep1Complete = true;
                if (!AppMan.App.Cancelled)
                {
                    if (AppMan.App.Settings.My20)
                    {
                        AppMan.App.Settings.My20 = true;
                        await Application.Current.Dispatcher.Invoke(() => UIHelper.ShowDialog(LM.GetValue("MessageBox.My20Found"), "Syn3 Updater", LM.GetValue("String.OK")).ShowAsync());
                    }
                    else
                    {
                        AppMan.App.Settings.My20 = false;
                        await Application.Current.Dispatcher.Invoke(() => UIHelper.ShowDialog(LM.GetValue("MessageBox.My20NotFound"), "Syn3 Updater", LM.GetValue("String.OK")).ShowAsync());
                    }
                }
                else
                {
                    await Application.Current.Dispatcher.Invoke(() => UIHelper.ShowDialog(LM.GetValue("MessageBox.My20CheckCancelled"), "Syn3 Updater", LM.GetValue("String.OK")).ShowAsync());
                }

                AppMan.App.FireHomeTabEvent();
            }
            else if (_action == "gracenotesremoval" || _action == "voiceshrinker" || _action == "downgrade")
            {
                await Application.Current.Dispatcher.Invoke(() => UIHelper.ShowDialog(LM.GetValue("MessageBox.GenericUtilityComplete"), "Syn3 Updater", LM.GetValue("String.OK")).ShowAsync());
                AppMan.App.FireUtilityTabEvent();
            }
            Reset();
        }

        private void CancelAction()
        {
            CancelButtonEnabled = false;
            AppMan.App.IsDownloading = false;
            _tokenSource.Cancel();
            TotalPercentage = 0;
            CurrentProgress = 0;
            DownloadInfo = "";
            DownloadPercentage = "";
            Application.Current.Dispatcher.Invoke(() => DownloadQueueList.Clear());
            AppMan.App.AppsSelected = false;
            AppMan.App.SkipFormat = false;
            _tokenSource.Dispose();
            _tokenSource = new CancellationTokenSource();
            AppMan.App.FireHomeTabEvent();
        }

        private void Reset()
        {
            CancelButtonEnabled = false;
            AppMan.App.IsDownloading = false;
            _tokenSource.Cancel();
            TotalPercentage = 0;
            CurrentProgress = 0;
            DownloadInfo = "";
            DownloadPercentage = "";
            Application.Current.Dispatcher.Invoke(() => DownloadQueueList.Clear());
            AppMan.App.AppsSelected = false;
            AppMan.App.SkipFormat = false;
            _tokenSource.Dispose();
            _tokenSource = new CancellationTokenSource();
        }

        private readonly ConcurrentDictionary<int, int> _parts = new();

        private void DownloadPercentageChanged(object sender, EventArgs<int> e)
        {
            if (e.Part == 0)
            { 
                CurrentProgress = e.Value;
                DownloadPercentage = $"{e.Value}% {_progressBarSuffix}";
                TotalPercentage = _count * 100 + e.Value;
            }
            else
            {
                if (_parts.TryGetValue(e.Part, out int _))
                    _parts[e.Part] = e.Value;
                else
                    _parts.TryAdd(e.Part, e.Value);

                double downloadPercentage = Convert.ToInt32(_parts.Sum(part => part.Value) * 1d / (AppMan.App.Settings.DownloadConnections * 1d));
                int value = Convert.ToInt32(downloadPercentage);
                CurrentProgress = value;
                DownloadPercentage = $"{CurrentProgress}% {_progressBarSuffix}";
                TotalPercentage = _count * 100 + CurrentProgress;
            }
        }

        private async Task PrepareUsbAsync()
        {
            if (AppMan.App.DownloadToFolder)
            {
                Log += "[" + DateTime.Now + "] Preparing selected directory (No USB Drive Selected)" + Environment.NewLine;
                AppMan.Logger.Info("Preparing selected directory  (No USB Drive Selected)");
            }
            else
            {
                Log += "[" + DateTime.Now + "] Preparing USB drive" + Environment.NewLine;
                AppMan.Logger.Info("Preparing USB drive");
            }

            if (!AppMan.App.SkipFormat)
            {
                if (AppMan.App.DownloadToFolder)
                {
                    Log += "[" + DateTime.Now + "] Clearing Selected Folder" + Environment.NewLine;
                    try
                    {
                        foreach (string file in Directory.GetFiles(AppMan.App.DriveLetter))
                            File.Delete(file);
                        foreach (string dir in Directory.GetDirectories(AppMan.App.DriveLetter))
                            Directory.Delete(dir, true);
                    }
                    catch (IOException)
                    {
                        Log += "[" + DateTime.Now + "] Unable to clear folder, continuing anyway" + Environment.NewLine;
                        AppMan.Logger.Info("Unable to clear folder, continuing anyway");
                    }
                }
                else
                {
                    Log += "[" + DateTime.Now + "] Formatting USB drive" + Environment.NewLine;
                    AppMan.Logger.Info("Formatting USB drive");
                    using (Process p = new())
                    {
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardInput = true;
                        p.StartInfo.FileName = "diskpart.exe";
                        p.StartInfo.CreateNoWindow = true;

                        Log += "[" + DateTime.Now + "] Re-creating partition table as MBR and formatting as ExFat on selected USB drive" + Environment.NewLine;
                        AppMan.Logger.Info("Re-creating partition table as MBR and formatting as ExFat on selected USB drive");

                        p.Start();
                        await p.StandardInput.WriteLineAsync($"SELECT DISK={AppMan.App.DriveNumber}");
                        await p.StandardInput.WriteLineAsync("CLEAN");
                        await p.StandardInput.WriteLineAsync("CONVERT MBR");
                        await p.StandardInput.WriteLineAsync("CREATE PARTITION PRIMARY");
                        await p.StandardInput.WriteLineAsync("FORMAT FS=EXFAT LABEL=\"CYANLABS\" QUICK");
                        await p.StandardInput.WriteLineAsync($"ASSIGN LETTER={AppMan.App.DriveLetter.Replace(":", "")}");
                        await p.StandardInput.WriteLineAsync("EXIT");

                        p.WaitForExit();
                    }

                    Thread.Sleep(5000);
                }
            }

            foreach (SModel.Ivsu item in AppMan.App.Ivsus)
                Application.Current.Dispatcher.Invoke(() => DownloadQueueList.Add(AppMan.App.DownloadPath + item.FileName));

            Directory.CreateDirectory($@"{AppMan.App.DriveLetter}\SyncMyRide\");
            await DoCopy().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    if (t.Exception != null)
                        Application.Current.Dispatcher.Invoke(() => AppMan.Logger.CrashWindow(t.Exception.InnerExceptions.FirstOrDefault()));
                    CancelAction();
                }

                if (t.IsCompleted && !t.IsFaulted)
                    CopyComplete();
            }, _tokenSource.Token);
        }

        private void CreateAutoInstall()
        {
            Log += "[" + DateTime.Now + "] Generating Autoinstall.lst" + Environment.NewLine;
            AppMan.Logger.Info("Generating Autoinstall.lst");
            StringBuilder autoinstalllst = DownloadViewModelService.CreateAutoInstallFile(_selectedRelease, _selectedRegion);
            File.WriteAllText($@"{AppMan.App.DriveLetter}\autoinstall.lst", autoinstalllst.ToString());
            File.Create($@"{AppMan.App.DriveLetter}\DONTINDX.MSA");
        }


        private void CreateReformat()
        {
            Log += "[" + DateTime.Now + "] Generating reformat.lst" + Environment.NewLine;
            AppMan.Logger.Info("Generating reformat.lst");

            string reformatlst = "";
            int i = 0;
            foreach (SModel.Ivsu item in AppMan.App.Ivsus)
            {
                if (item.Source == "naviextras") continue;
                if (InstallMode == "reformat")
                {
                    if (item.Md5 == Api.ReformatTool.Md5) continue;
                }
                else if (InstallMode == "downgrade")
                {
                    if (item.Md5 == Api.ReformatTool.Md5 || item.Md5 == Api.DowngradeApp.Md5 && _selectedRelease != "Sync 3.3.19052" || item.Md5 == Api.DowngradeTool.Md5)
                        continue;
                }

                i++;
                reformatlst += $"{item.Type}={item.FileName}";
                if (i != AppMan.App.Ivsus.Count)
                    reformatlst += Environment.NewLine;
            }

            File.WriteAllText($@"{AppMan.App.DriveLetter}\reformat.lst", reformatlst);

            Log += "[" + DateTime.Now + "] Generating autoinstall.lst" + Environment.NewLine;
            AppMan.Logger.Info("Generating autoinstall.lst");

            StringBuilder autoinstalllst = new StringBuilder(
                $@"; CyanLabs Syn3Updater {Assembly.GetEntryAssembly()?.GetName().Version} {AppMan.App.LauncherPrefs.ReleaseTypeInstalled} - {InstallMode} {(AppMan.App.ModeForced ? "FORCED " : "")} Mode - {_selectedRelease} {_selectedRegion}{Environment.NewLine}{Environment.NewLine}[SYNCGen3.0_ALL_PRODUCT]{Environment.NewLine}");
            if (InstallMode == "downgrade")
            {
                autoinstalllst.Append(
                    $@"Item1 = TOOL - {Api.DowngradeTool.FileName}\rOpen1 = SyncMyRide\{Api.DowngradeTool.FileName}\r").Replace(@"\r",
                    Environment.NewLine);
                autoinstalllst.Append(
                    $@"Item2 = APP - {Api.DowngradeApp.FileName}\rOpen2 = SyncMyRide\{Api.DowngradeApp.FileName}\r").Replace(@"\r",
                    Environment.NewLine);
                autoinstalllst.Append($@"Options = AutoInstall{Environment.NewLine}[SYNCGen3.0_ALL]{Environment.NewLine}");
                autoinstalllst.Append($@"Item1 = REFORMAT TOOL - {Api.ReformatTool.FileName}\rOpen1 = SyncMyRide\{Api.ReformatTool.FileName}\r")
                    .Replace(@"\r", Environment.NewLine);
                autoinstalllst.Append("Options = AutoInstall,Include,Transaction").Append(Environment.NewLine);
            }
            else if (InstallMode == "reformat")
            {
                autoinstalllst.Append($@"Item1 = REFORMAT TOOL  - {Api.ReformatTool.FileName}\rOpen1 = SyncMyRide\{Api.ReformatTool.FileName}\r")
                    .Replace(@"\r", Environment.NewLine);
                autoinstalllst.Append("Options = AutoInstall");
            }

            File.WriteAllText($@"{AppMan.App.DriveLetter}\autoinstall.lst", autoinstalllst.ToString());
            File.Create($@"{AppMan.App.DriveLetter}\DONTINDX.MSA");
        }

        private async Task<bool> ValidateFile(string srcfile, string localfile, string md5, bool copy, bool existing = false)
        {
            if (existing)
            {
                DownloadInfo = $"{LM.GetValue("String.CheckingExistingFile")}: {Path.GetFileName(localfile)}";
                Log += $"[{DateTime.Now}] Checking Existing File: {Path.GetFileName(localfile)} {Environment.NewLine}";
                AppMan.Logger.Info($"Checking Existing File: {Path.GetFileName(localfile)}");
            }
            else
            {
                DownloadInfo = $"{LM.GetValue("String.Validating")}: {Path.GetFileName(localfile)}";
                Log += $"[{DateTime.Now}] Validating: {Path.GetFileName(localfile)} {Environment.NewLine}";
                AppMan.Logger.Info($"Validating: {Path.GetFileName(localfile)}");
            }

            _progressBarSuffix = LM.GetValue("String.Validated");
            FileHelper.OutputResult outputResult = await _fileHelper.ValidateFile(srcfile, localfile, md5, copy, _ct);

            if (outputResult.Message != "")
            {
                Log += "[" + DateTime.Now + "] " + outputResult.Message + Environment.NewLine;
                AppMan.Logger.Info(outputResult.Message);
            }

            return outputResult.Result;
        }

        #endregion
    }
}