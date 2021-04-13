using NUnit.Framework;
using Cyanlabs.Updater.Services;
using Cyanlabs.Syn3Updater;
using System;
using System.Reflection;
using System.Collections.ObjectModel;
using System.IO;

namespace Syn3Updater.Tests.Common.Services
{
    [TestFixture]
    public class DownloadViewModelServiceTests
    {
        //private string expectedString = $@"; CyanLabs Syn3Updater 2.x - Autoinstall Mode - Sync 3.3.19052 NA{Environment.NewLine}{Environment.NewLine}[SYNCGen3.0_ALL_PRODUCT]{Environment.NewLine}Item1 = APPS - 4U5T-14G381-AN_1552583626000.TAR.GZ{Environment.NewLine}Open1 = SyncMyRide\4U5T-14G381-AN_1552583626000.TAR.GZ{Environment.NewLine}Options = AutoInstall{Environment.NewLine}";
        private string expectedString = $@"; CyanLabs Syn3Updater {Assembly.GetEntryAssembly()?.GetName().Version} {AppMan.App.LauncherPrefs.ReleaseTypeInstalled} - Autoinstall {(AppMan.App.ModeForced ? "FORCED " : "")}Mode - Sync 3.3.19052 NA{Environment.NewLine}{Environment.NewLine}[SYNCGen3.0_ALL_PRODUCT]{Environment.NewLine}Item1 = APPS - 4U5T-14G381-AN_1552583626000.TAR.GZ{Environment.NewLine}Open1 = SyncMyRide\4U5T-14G381-AN_1552583626000.TAR.GZ{Environment.NewLine}Options = AutoInstall{Environment.NewLine}";

        //called before each [Test] 
        [SetUp]
        public void SetUp()
        {
            //so we know that this list is clear before every run 
            AppMan.App.Ivsus.Clear();
            AppMan.App.SVersion = string.Empty;
        }
        //A real unit test(since this method is being tested in total isolation)! 
        [Test]
        public void Service_Generates_Proper_AutoInstallFile()
        {
            AppMan.App.SVersion = "3.3.19052";
            AppMan.App.Ivsus.Add(new Cyanlabs.Syn3Updater.Model.SModel.Ivsu
            {
                Selected = true,
                Type = "APPS",
                Name = "4U5T-14G381-AN",
                Version = "3.3.19052",
                Notes = "Some Notes", //convention to put random values for values that aren't relevant to the unit test 
                Url = "Some Url",
                Md5 = "Some hash",
                FileName = "4U5T-14G381-AN_1552583626000.TAR.GZ",
                FileSize = 502997338
            });
            var testedString = DownloadViewModelService.CreateAutoInstallFile("Sync 3.3.19052", "NA");
            Assert.AreEqual(testedString.ToString(), expectedString);
        }

        [Test]
        public void Service_Generates_Proper_Autoinstall_With_Maps()
        {
            AppMan.App.SVersion = "3.3.19052";
            AppMan.App.Ivsus = new ObservableCollection<Cyanlabs.Syn3Updater.Model.SModel.Ivsu>
            {
                new Cyanlabs.Syn3Updater.Model.SModel.Ivsu
            {
                Selected = true,
                Type = "MAP_LICENSE",
                Name = "4U5T-14G424-CD",
                Version = "Some Version",
                Notes = "Some Notes",
                Url = "Some Url",
                Md5 = "Some hash",
                FileName = "4U5T-14G424-CD_1616778089000.TAR.GZ" ,
                FileSize = 1700
            },
                     new Cyanlabs.Syn3Updater.Model.SModel.Ivsu
            {
                Selected = true,
                Type = "MAP",
                Name = "4U5T-14G421-CAD",
                Version = "Some Version",
                Notes = "Some Notes",
                Url = "Some Url",
                Md5 = "Some hash",
                FileName = "4U5T-14G421-CAD_1615428059000.TAR.GZ" ,
                FileSize = 1048576000
            },
             new Cyanlabs.Syn3Updater.Model.SModel.Ivsu
            {
                Selected = true,
                Type = "MAP",
                Name = "4U5T-14G421-CBD",
                Version = "Some Version",
                Notes = "Some Notes",
                Url = "Some Url",
                Md5 = "Some hash",
                FileName = "4U5T-14G421-CBD_1615428261000.TAR.GZ" ,
                FileSize = 1153433600
            },
            new Cyanlabs.Syn3Updater.Model.SModel.Ivsu
            {
                Selected = true,
                Type = "MAP",
                Name = "4U5T-14G421-CCD",
                Version = "Some Version",
                Notes = "Some Notes",
                Url = "Some Url",
                Md5 = "Some hash",
                FileName = "4U5T-14G421-CCD_1615428361000.TAR.GZ" ,
                FileSize = 1048576000
            },
              new Cyanlabs.Syn3Updater.Model.SModel.Ivsu
            {
                Selected = true,
                Type = "MAP",
                Name = "4U5T-14G421-CDD",
                Version = "Some Version",
                Notes = "Some Notes",
                Url = "Some Url",
                Md5 = "Some hash",
                FileName = "4U5T-14G421-CDD_1615428467000.TAR.GZ" ,
                FileSize = 1310720000
            },
            new Cyanlabs.Syn3Updater.Model.SModel.Ivsu
            {
                Selected = true,
                Type = "MAP",
                Name = "4U5T-14G421-CFD",
                Version = "Some Version",
                Notes = "Some Notes",
                Url = "Some Url",
                Md5 = "Some hash",
                FileName = "4U5T-14G421-CFD_1615428639000.TAR.GZ" ,
                FileSize = 1048576000
            },
              new Cyanlabs.Syn3Updater.Model.SModel.Ivsu
            {
                Selected = true,
                Type = "MAP",
                Name = "4U5T-14G421-CGD",
                Version = "Some Version",
                Notes = "Some Notes",
                Url = "Some Url",
                Md5 = "Some hash",
                FileName = "4U5T-14G421-CGD_1615428721000.TAR.GZ" ,
                FileSize = 1100000000
            },
                new Cyanlabs.Syn3Updater.Model.SModel.Ivsu
            {
                Selected = true,
                Type = "MAP",
                Name = "4U5T-14G421-CHD",
                Version = "Some Version",
                Notes = "Some Notes",
                Url = "Some Url",
                Md5 = "Some hash",
                FileName = "4U5T-14G421-CHD_1615428793000.TAR.GZ"  ,
                FileSize = 1000000000
            },
                  new Cyanlabs.Syn3Updater.Model.SModel.Ivsu
            {
                Selected = true,
                Type = "MAP",
                Name = "4U5T-14G421-CJD",
                Version = "Some Version",
                Notes = "Some Notes",
                Url = "Some Url",
                Md5 = "Some hash",
                FileName = "4U5T-14G421-CJD_1615428865000.TAR.GZ" ,
                FileSize = 700000000
            },
                    new Cyanlabs.Syn3Updater.Model.SModel.Ivsu
            {
                Selected = true,
                Type = "MAP",
                Name = "4U5T-14G421-CED",
                Version = "Some Version",
                Notes = "Some Notes",
                Url = "Some Url",
                Md5 = "Some hash",
                FileName = "4U5T-14G421-CED_1615428554000.TAR.GZ" ,
                FileSize = 1048576000
            },
                      new Cyanlabs.Syn3Updater.Model.SModel.Ivsu
            {
                Selected = true,
                Type = "VOICE_NAV",
                Name = "4U5T-14G422-CAH",
                Version = "Some Version",
                Notes = "Some Notes",
                Url = "Some Url",
                Md5 = "Some hash",
                FileName = "4U5T-14G421-CAD_1615428059000.TAR.GZ" ,
                FileSize = 1300000000
            },
             new Cyanlabs.Syn3Updater.Model.SModel.Ivsu
            {
                Selected = true,
                Type = "VOICE_NAV",
                Name = "4U5T-14G422-CBH",
                Version = "Some Version",
                Notes = "Some Notes",
                Url = "Some Url",
                Md5 = "Some hash",
                FileName = "4U5T-14G422-CBH_1615429039000.TAR.GZ" ,
                FileSize = 800000000
            },
             new Cyanlabs.Syn3Updater.Model.SModel.Ivsu
            {
                Selected = true,
                Type = "VOICE_NAV",
                Name = "4U5T-14G422-CCH",
                Version = "Some Version",
                Notes = "Some Notes",
                Url = "Some Url",
                Md5 = "Some hash",
                FileName = "4U5T-14G422-CCH_1615429127000.TAR.GZ" ,
                FileSize = 550000000
            },
            };
            var testedString = DownloadViewModelService.CreateAutoInstallFile("Sync 3.3.19052", "NA");
            //Assert.AreEqual(testedString.ToString(), File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Services/TestAutoInstalls/mapautoinstall.lst")));
        }   
    }
}