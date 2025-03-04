﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using AsyncAwaitBestPractices.MVVM;
using Cyanlabs.Syn3Updater.Helper;
using Cyanlabs.Syn3Updater.Model;
using Cyanlabs.Syn3Updater.Services;
using Cyanlabs.Updater.Common;
using ModernWpf.Controls;
using Nito.AsyncEx;
using Ookii.Dialogs.Wpf;

namespace Cyanlabs.Syn3Updater.UI.Tabs
{
    internal class HomeViewModel : LanguageAwareBaseViewModel
    {
        #region Constructors

        private AsyncCommand _startButton;
        private ActionCommand _refreshUSB;
        private ActionCommand _regionInfo;
        private AsyncCommand<string> _visitFeedbackThread;
        public ActionCommand RefreshUSB => _refreshUSB ??= new ActionCommand(RefreshUsb);
        public ActionCommand RegionInfo => _regionInfo ??= new ActionCommand(RegionInfoAction);
        public AsyncCommand StartButton => _startButton ??= new AsyncCommand(StartAction);
        
        public AsyncCommand<string> VisitFeedbackThread => _visitFeedbackThread ??= new AsyncCommand<string>(VisitFeedbackThreadAction);
        #endregion

        #region Properties & Fields

        private string _apiAppReleases, _apiMapReleases;
        private string _stringCompatibility;
        private Api.JsonReleases _jsonMapReleases, _jsonReleases;

        private string _driveLetter;

        public string DriveLetter
        {
            get => _driveLetter;
            set => SetProperty(ref _driveLetter, value);
        }

        private string _driveName;

        public string DriveName
        {
            get => _driveName;
            set => SetProperty(ref _driveName, value);
        }

        private string _driveFileSystem;

        public string DriveFileSystem
        {
            get => _driveFileSystem;
            set => SetProperty(ref _driveFileSystem, value);
        }

        private USBHelper.Drive _selectedDrive;

        public USBHelper.Drive SelectedDrive
        {
            get => _selectedDrive;
            set
            {
                SetProperty(ref _selectedDrive, value);
                UpdateDriveInfo();
            }
        }

        private SModel.SRegion _selectedRegion;

        public SModel.SRegion SelectedRegion
        {
            get => _selectedRegion;
            set
            {
                SetProperty(ref _selectedRegion, value);
                if (value != null)
                    //we don't need to run this code using the WPF dispatcher, so use Cleary's libary 
                    AsyncContext.Run(async () => await UpdateSelectedRegion());
            }
        }

        private string _selectedRelease;

        public string SelectedRelease
        {
            get => _selectedRelease;
            set
            {
                SetProperty(ref _selectedRelease, value);
                if (value != null) AsyncContext.Run(async () => await UpdateSelectedRelease());
            }
        }

        private string _selectedMapVersion;

        public string SelectedMapVersion
        {
            get => _selectedMapVersion;
            set
            {
                SetProperty(ref _selectedMapVersion, value);
                if (value != null) AsyncContext.Run(async () => await UpdateSelectedMapVersion());
            }
        }

        private int _selectedMapVersionIndex;

        public int SelectedMapVersionIndex
        {
            get => _selectedMapVersionIndex;
            set => SetProperty(ref _selectedMapVersionIndex, value);
        }

        private int _selectedReleaseIndex;

        public int SelectedReleaseIndex
        {
            get => _selectedReleaseIndex;
            set => SetProperty(ref _selectedReleaseIndex, value);
        }

        private int _selectedRegionIndex;

        public int SelectedRegionIndex
        {
            get => _selectedRegionIndex;
            set => SetProperty(ref _selectedRegionIndex, value);
        }

        private ObservableCollection<USBHelper.Drive> _driveList;

        public ObservableCollection<USBHelper.Drive> DriveList
        {
            get => _driveList;
            set => SetProperty(ref _driveList, value);
        }

        private ObservableCollection<SModel.SRegion> _sRegions;

        public ObservableCollection<SModel.SRegion> SRegions
        {
            get => _sRegions;
            set => SetProperty(ref _sRegions, value);
        }

        private ObservableCollection<string> _sVersion;

        public ObservableCollection<string> SVersion
        {
            get => _sVersion;
            set => SetProperty(ref _sVersion, value);
        }

        private ObservableCollection<string> _sMapVersion;

        public ObservableCollection<string> SMapVersion
        {
            get => _sMapVersion;
            set => SetProperty(ref _sMapVersion, value);
        }

        private bool _sVersionsEnabled;

        public bool SVersionsEnabled
        {
            get => _sVersionsEnabled;
            set => SetProperty(ref _sVersionsEnabled, value);
        }

        private bool _sMapVersionsEnabled;

        public bool SMapVersionsEnabled
        {
            get => _sMapVersionsEnabled;
            set => SetProperty(ref _sMapVersionsEnabled, value);
        }

        private string _notes;

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        private bool _notesVisibility;

        public bool NotesVisibility
        {
            get => _notesVisibility;
            set => SetProperty(ref _notesVisibility, value);
        }

        private bool _feedbackVisibility;

        public bool FeedbackVisibility
        {
            get => _feedbackVisibility;
            set => SetProperty(ref _feedbackVisibility, value);
        }

        private ObservableCollection<SModel.Ivsu> _ivsuList;

        public ObservableCollection<SModel.Ivsu> IvsuList
        {
            get => _ivsuList;
            set => SetProperty(ref _ivsuList, value);
        }

        private string _currentRegion;

        public string CurrentRegion
        {
            get => _currentRegion;
            set
            {
                if (value == null) return;
                SetProperty(ref _currentRegion, value);
                AppMan.App.Settings.CurrentRegion = value;
            }
        }

        private string _currentVersion;

        public string CurrentVersion
        {
            get => _currentVersion;
            set => SetProperty(ref _currentVersion, value);
        }

        private string _currentNav;

        public string CurrentNav
        {
            get => _currentNav;
            set => SetProperty(ref _currentNav, value);
        }

        private string _currentProfile;

        public string CurrentProfile
        {
            get => _currentProfile;
            set => SetProperty(ref _currentProfile, value);
        }

        private string _downloadLocation;

        public string DownloadLocation
        {
            get => _downloadLocation;
            set
            {
                if (value != null) SetProperty(ref _downloadLocation, value);
            }
        }

        private string _installMode;

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

        private string _my20Mode;

        public string My20Mode
        {
            get => _my20Mode;
            set => SetProperty(ref _my20Mode, value);
        }

        private bool _startEnabled;

        public bool StartEnabled
        {
            get => _startEnabled;
            set => SetProperty(ref _startEnabled, value);
        }

        private string _feedbackUrl;

        public string FeedbackUrl
        {
            get => _feedbackUrl;
            set => SetProperty(ref _feedbackUrl, value);
        }

        private Dictionary<string, string> _magnetActions = new();

        #endregion

        #region Methods

        public void ReloadSettings()
        {
            CurrentNav = AppMan.App.Settings.CurrentNav ? LM.GetValue("String.Yes") : LM.GetValue("String.No");
            CurrentRegion = AppMan.App.Settings.CurrentRegion;
            CurrentVersion = AppMan.App.SVersion;
            DownloadLocation = AppMan.App.DownloadPath;
            My20Mode = AppMan.App.Settings.My20 ? LM.GetValue("String.Enabled") : LM.GetValue("String.Disabled");
            InstallModeForced = AppMan.App.ModeForced ? LM.GetValue("String.Yes") : LM.GetValue("String.No");
            StartEnabled = false;
            InstallMode = AppMan.App.Settings.InstallMode;
            CurrentProfile = AppMan.App.MainSettings.Profile;
            AppMan.Logger.Info($"Current Details - Region: {CurrentRegion} - Version: {CurrentVersion} - Navigation: {CurrentNav}");
            UpdateInstallMode();
        }

        public void Init()
        {
            NotesVisibility = false;
            if (!string.IsNullOrEmpty(AppMan.App.Magnet))
                _magnetActions = AppMan.App.Magnet
                    .Split(';')
                    .Select(part => part.Split('='))
                    .Where(part => part.Length == 2)
                    .ToDictionary(sp => sp[0], sp => sp[1]);
            AppMan.App.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiSecret.Token);
            SRegions = new ObservableCollection<SModel.SRegion>
            {
                new() {Code = "EU", Name = "Europe"},
                new() {Code = "NA", Name = "United States, Canada & Mexico"},
                new() {Code = "CN", Name = "China"},
                new() {Code = "ANZ", Name = "Australia, New Zealand, South America, Turkey & Taiwan"},
                new() {Code = "ROW", Name = "Middle East, Africa, India, Sri Lanka, Israel, South East Asia, Caribbean & Central America"}
            };
            SVersion = new ObservableCollection<string>();
            SMapVersion = new ObservableCollection<string>();
            IvsuList = new ObservableCollection<SModel.Ivsu>();
            SVersionsEnabled = false;

            if (_magnetActions?.Count != 0)
            {
                bool exists = SRegions.Any(x => x.Code == _magnetActions["Region"]);
                if (exists) SelectedRegion = SRegions.FirstOrDefault(x => x.Code == _magnetActions["Region"]);
            }
            else
            {
                SelectedRegionIndex = -1;
                SelectedReleaseIndex = -1;
                SelectedMapVersionIndex = -1;
            }

            RefreshUsb();
        }

        private static void RegionInfoAction()
        {
            Process.Start(Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "en"
                ? "https://cyanlabs.net/api/Syn3Updater/region.php"
                : $"https://translate.google.co.uk/translate?hl=&sl=en&tl={Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName}&u=https%3A%2F%2Fcyanlabs.net%2Fapi%2FSyn3Updater%2Fregion.php");
        }

        private async void RefreshUsb()
        {
            try
            {
                DriveLetter = null;
                ObservableCollection<USBHelper.Drive> tmpDriveList = USBHelper.RefreshDevices(true);
                if (tmpDriveList != null && tmpDriveList.Count > 0) DriveList = tmpDriveList;
            }
            catch (XamlParseException e)
            {
                await UIHelper.ShowErrorDialog(e.GetFullMessage()).ShowAsync();
                AppMan.Logger.Info("ERROR: " + e.GetFullMessage());
            }
            catch (UnauthorizedAccessException e)
            {
                await UIHelper.ShowErrorDialog(e.GetFullMessage()).ShowAsync();
                AppMan.Logger.Info("ERROR: " + e.GetFullMessage());
            }
            catch (NullReferenceException e)
            {
                await UIHelper.ShowErrorDialog(e.GetFullMessage()).ShowAsync();
                AppMan.Logger.Info("ERROR: " + e.GetFullMessage());
            }
        }

        private async void UpdateDriveInfo()
        {
            StartEnabled = SelectedRelease != null && SelectedRegion != null && SelectedMapVersion != null && SelectedDrive != null;
            if (SelectedDrive?.Name == LM.GetValue("Home.NoUSBDir"))
            {
                VistaFolderBrowserDialog dialog = new();
                if (dialog.ShowDialog().GetValueOrDefault())
                {
                    string destination = dialog.SelectedPath;
                    DriveInfo driveInfo = new(destination);
                    if (driveInfo.DriveType != DriveType.Fixed)
                        if (await UIHelper.ShowDialog(LM.GetValue("MessageBox.RemovableDriveFolder"), "Syn3 Updater", LM.GetValue("String.No"),LM.GetValue("String.Yes")).ShowAsync() ==
                            ContentDialogResult.None)
                            return;
                    if (AppMan.App.DownloadPath.Contains(destination))
                    {
                        await UIHelper.ShowErrorDialog(LM.GetValue("MessageBox.CancelDownloadIsFolder")).ShowAsync();
                        ReloadSettings();
                    }
                    else
                    {
                        DriveLetter = destination + "\\";
                        DriveFileSystem = "";
                        DriveName = "";
                        AppMan.App.DriveName = SelectedDrive?.Name;
                    }
                }
                else
                {
                    RefreshUsb();
                }
            }
            else if (SelectedDrive?.Name != null)
            {
                // Update app level vars
                AppMan.App.DriveFileSystem = SelectedDrive?.FileSystem;
                AppMan.App.DrivePartitionType = SelectedDrive?.PartitionType;
                AppMan.App.DriveName = SelectedDrive?.Name;
                AppMan.App.SkipFormat = SelectedDrive.SkipFormat;

                // Update local level vars
                DriveFileSystem = SelectedDrive?.PartitionType + " " + SelectedDrive?.FileSystem;
                DriveName = SelectedDrive?.Name;
                DriveLetter = SelectedDrive?.Letter;
            }
        }

        private async Task UpdateSelectedRegion()
        {
            NotesVisibility = false;
            SVersion?.Clear();
            SMapVersion?.Clear();
            if (SelectedRegion.Code != "")
            {
                IvsuList?.Clear();
                SelectedMapVersion = null;
                SelectedRelease = null;
                _apiAppReleases = Api.AppReleasesConst.Replace("[published]",
                    $"filter[status][_in]=published,private&filter[key][_in]=public,v2,{AppMan.App.MainSettings.LicenseKey}");

                Stream stringReleasesJson;
                try
                {
                    // however don't call ConfigureAwait(false) here or on any of it's friends below as you need this code to run on the previous context (ie the UI context)
                    // https://blog.stephencleary.com/2012/02/async-and-await.html#context
                    HttpResponseMessage response = await AppMan.App.Client.GetAsync(_apiAppReleases);
                    stringReleasesJson = await response.Content.ReadAsStreamAsync();
                }
                // ReSharper disable once RedundantCatchClause
                catch (WebException)
                {
                    throw;
                    //TODO Exception handling
                }

                if (AppMan.App.Settings.CurrentNav && AppMan.App.Settings.CurrentVersion >= Api.ReformatVersion) SVersion?.Add(LM.GetValue("String.OnlyMaps"));

                _jsonReleases = JsonHelpers.Deserialize<Api.JsonReleases>(stringReleasesJson);

                foreach (Api.Data item in _jsonReleases.Releases)
                    if (item.Regions.Contains(SelectedRegion.Code))
                        SVersion?.Add(item.Name);

                SVersionsEnabled = true;
                StartEnabled = SelectedRelease != null && SelectedRegion != null && SelectedMapVersion != null && SelectedDrive != null;

                if (SVersion != null && _magnetActions?.Count != 0 && SVersion.Contains("Sync " + _magnetActions?["Release"]))
                    SelectedRelease = "Sync " + _magnetActions?["Release"];
            }
        }

        private async Task UpdateSelectedRelease()
        {
            SMapVersion?.Clear();
            if (!AppMan.App.Settings.CurrentNav)
                SMapVersion?.Add(LM.GetValue("String.NonNavAPIM"));
            else if (AppMan.App.Settings.CurrentVersion >= Api.ReformatVersion && _selectedRelease != LM.GetValue("String.OnlyMaps"))
                SMapVersion?.Add(LM.GetValue("String.KeepExistingMaps"));

            string license = "";
            if (AppMan.App.MainSettings.LicenseKey?.Length > 10) license = "{\"licensekeys\":{\"_contains\":\"" + AppMan.App.MainSettings.LicenseKey + "\"}},";
            _apiMapReleases = Api.MapReleasesConst.Replace("[published]",
                "filter={\"_and\":[{\"_or\":[" + license +
                "{\"licensekeys\":{\"_empty\":true}}]},{\"status\":{\"_in\":[\"private\",\"published\"]}},{\"regions\":{\"_in\":\"[regionplaceholder]\"}},[esn]{\"compatibility\":{\"_contains\":\"[compat]\"}}]}");

            if (SelectedRelease != "")
            {
                SelectedMapVersion = null;
                IvsuList?.Clear();

                foreach (Api.Data item in _jsonReleases.Releases)
                    if (item.Name == SelectedRelease)
                    {
                        _stringCompatibility = item.Version.Substring(0, 3);
                        if (item.Notes == null)
                        {
                            NotesVisibility = false;
                            continue;
                        }

                        NotesVisibility = true;
                        Notes = item.Notes.Replace("\n", Environment.NewLine);

                        if (item.FeedbackUrl == null)
                        {             
                            FeedbackVisibility = false;
                            continue;
                        }
                        FeedbackUrl = item.FeedbackUrl;
                        FeedbackVisibility = true;
                    }

                if (SelectedRelease == LM.GetValue("String.OnlyMaps") || AppMan.App.Settings.My20)
                {
                    _apiMapReleases = _apiMapReleases.Replace("[compat]", "3.4");
                    _apiMapReleases = _apiMapReleases.Replace("[esn]", "{\"esn\": {\"_eq\": \"false\"}},");
                }
                else
                {
                    _apiMapReleases = _apiMapReleases.Replace("[compat]", _stringCompatibility);
                    _apiMapReleases = _apiMapReleases.Replace("[esn]", "");
                }

                _apiMapReleases = _apiMapReleases.Replace("[regionplaceholder]", SelectedRegion.Code);

                HttpResponseMessage response = await AppMan.App.Client.GetAsync(_apiMapReleases);
                Stream stringMapReleasesJson = await response.Content.ReadAsStreamAsync();

                if (AppMan.App.Settings.CurrentNav)
                {
                    _jsonMapReleases = JsonHelpers.Deserialize<Api.JsonReleases>(stringMapReleasesJson);
                    foreach (Api.Data item in _jsonMapReleases.Releases) SMapVersion?.Add(item.Name);
                }

                SMapVersionsEnabled = true;
                StartEnabled = SelectedRelease != null && SelectedRegion != null && SelectedMapVersion != null && SelectedDrive != null;

                string mapReleaseTmp = _magnetActions?["Maps"].Replace("_", " ");
                if (SMapVersion != null && _magnetActions?.Count != 0 && SMapVersion.Any(x => x == mapReleaseTmp)) SelectedMapVersion = mapReleaseTmp;
            }
        }

        private async Task UpdateSelectedMapVersion()
        {
            if (!string.IsNullOrWhiteSpace(SelectedMapVersion))
            {
                IvsuList?.Clear();

                UpdateInstallMode();

                HttpResponseMessage response;
                
                string appReleaseSingle = AppMan.App.Settings.CurrentNav
                    ? Api.AppReleaseSingle.Replace("[navplaceholder]", "nav") + SelectedRelease
                    : Api.AppReleaseSingle.Replace("[navplaceholder]", "nonnav") + SelectedRelease;
                
                if (SelectedRelease != LM.GetValue("String.OnlyMaps"))
                {
                    response = await AppMan.App.Client.GetAsync(appReleaseSingle);
                    Stream stringDownloadJson = await response.Content.ReadAsStreamAsync();
                    Api.JsonReleases jsonIvsUs = JsonHelpers.Deserialize<Api.JsonReleases>(stringDownloadJson);

                    foreach (Api.Ivsus item in jsonIvsUs.Releases[0].IvsusList)
                        if (item.Ivsu.Regions.Contains("ALL") || item.Ivsu.Regions.Contains(SelectedRegion.Code))
                            IvsuList?.Add(new SModel.Ivsu
                            {
                                Type = item.Ivsu.Type,
                                Name = item.Ivsu.Name,
                                Version = item.Ivsu.Version,
                                Notes = item.Ivsu.Notes,
                                Url = item.Ivsu.Url,
                                Md5 = item.Ivsu.Md5,
                                Selected = true,
                                FileName = FileHelper.url_to_filename(item.Ivsu.Url),
                                FileSize = item.Ivsu.FileSize
                            });
                }
                else
                {
                    InstallMode = "autoinstall";
                    AppMan.App.Settings.InstallMode = InstallMode;
                }

                response = await AppMan.App.Client.GetAsync(Api.MapReleaseSingle + SelectedMapVersion);
                Stream stringMapDownloadJson = await response.Content.ReadAsStreamAsync();
                Api.JsonReleases jsonMapIvsUs = JsonHelpers.Deserialize<Api.JsonReleases>(stringMapDownloadJson);

                if (SelectedMapVersion != LM.GetValue("String.NoMaps") && SelectedMapVersion != LM.GetValue("String.NonNavAPIM") &&
                    SelectedMapVersion != LM.GetValue("String.KeepExistingMaps"))
                    foreach (Api.Ivsus item in jsonMapIvsUs.Releases[0].IvsusList)
                        if (item.MapIvsu.Regions.Contains("ALL") || item.MapIvsu.Regions.Contains(SelectedRegion.Code))
                            IvsuList?.Add(new SModel.Ivsu
                            {
                                Type = item.MapIvsu.Type,
                                Name = item.MapIvsu.Name,
                                Version = item.MapIvsu.Version,
                                Notes = item.MapIvsu.Notes,
                                Url = item.MapIvsu.Url,
                                Md5 = item.MapIvsu.Md5,
                                Selected = true,
                                FileName = FileHelper.url_to_filename(item.MapIvsu.Url),
                                FileSize = item.MapIvsu.FileSize,
                                Source = item.MapIvsu.Source
                            });
            }
        }

        public void UpdateInstallMode()
        {
            if (!string.IsNullOrWhiteSpace(SelectedMapVersion) && !string.IsNullOrWhiteSpace(SelectedRelease) && !string.IsNullOrWhiteSpace(SelectedRegion.Code))
            {
                //LESS THAN 3.2
                if (AppMan.App.Settings.CurrentVersion < Api.ReformatVersion)
                {
                    if (AppMan.App.Settings.My20) InstallMode = "autoinstall";
                    if (!AppMan.App.ModeForced)
                        InstallMode = "reformat";
                }

                //Above 3.2 and  Below 3.4.19274
                else if (AppMan.App.Settings.CurrentVersion >= Api.ReformatVersion &&
                         AppMan.App.Settings.CurrentVersion < Api.BlacklistedVersion)
                {
                    //Update Nav?
                    if (SelectedMapVersion == LM.GetValue("String.NoMaps") || SelectedMapVersion == LM.GetValue("String.NonNavAPIM") ||
                        SelectedMapVersion == LM.GetValue("String.KeepExistingMaps"))
                    {
                        if (!AppMan.App.ModeForced) InstallMode = "autoinstall";
                    }
                    else if (AppMan.App.Settings.My20)
                    {
                        InstallMode = "autoinstall";
                    }
                    else if (!AppMan.App.ModeForced)
                    {
                        InstallMode = "reformat";
                    }
                }

                //3.4.19274 or above
                else if (AppMan.App.Settings.CurrentVersion >= Api.BlacklistedVersion)
                {
                    //Update Nav?
                    if (SelectedMapVersion == LM.GetValue("String.NoMaps") || SelectedMapVersion == LM.GetValue("String.NonNavAPIM") ||
                        SelectedMapVersion == LM.GetValue("String.KeepExistingMaps"))
                    {
                        if (!AppMan.App.ModeForced)
                            InstallMode = "autoinstall";
                    }
                    else if (AppMan.App.Settings.My20)
                    {
                        InstallMode = "autoinstall";
                    }
                    else if (!AppMan.App.ModeForced)
                    {
                        InstallMode = "downgrade";
                    }
                }

                AppMan.App.Action = "main";
                AppMan.App.Settings.InstallMode = InstallMode;
                StartEnabled = SelectedRelease != null && SelectedRegion != null && SelectedMapVersion != null && SelectedDrive != null;
            }
        }

        private async Task StartAction()
        {
            if (_selectedRegion.Code == "NA" && AppMan.App.Settings.My20)
                if (await UIHelper.ShowDialog("WARNING, FROM OUR TESTING SOME VOICES MAY BE MISSED WHEN INSTALLING NA MAPS VIA AUTOINSTALL (MY20), FROM OUR TESTING IT SEEMS ENGLISH (AMERICAN) IS INSTALLED WITHOUT ISSUE BUT THE OTHERS ARE STILL BEING INVESTIGATED. FOR FURTHER INFORMATION AND TO HELP CYANLABS TROUBLESHOOT THIS ISSUE PLEASE CLICK 'NO' TO VISIT OUR FORUM THREAD.\n\nIf you understand the risks and wish to continue anyway click 'YES'",
                                                  "Syn3 Updater", LM.GetValue("String.No"),LM.GetValue("String.Yes")).ShowAsync() == ContentDialogResult.None)
                {
                    Process.Start("https://community.cyanlabs.net/t/placeholder-my20-discussion/3203");
                    return;
                }

            await HomeViewModelService.Download(InstallMode, IvsuList, SelectedRegion, SelectedRelease, SelectedMapVersion, DriveLetter, SelectedDrive);
            StartEnabled = false;
        }

        public async Task VisitFeedbackThreadAction(string url)
        {
           if(!string.IsNullOrEmpty(url))
            Process.Start(url);
        }

        #endregion
    }
}