using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace MiscTools
{
    public class MiscTools : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private MiscToolsSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("dea0780b-3c2c-4aed-b387-8cb3a34e1562");

        public MiscTools(IPlayniteAPI api) : base(api)
        {
            settings = new MiscToolsSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new MiscToolsSettingsView();
        }

        private uint[] GetMissingData()
        {
            uint missingIcons = 0;
            uint missingCovers = 0;
            uint missingBackgrounds = 0;

            foreach (Game game in PlayniteApi.Database.Games)
            {
                if (string.IsNullOrWhiteSpace(game.Icon))
                    missingIcons++;

                if (string.IsNullOrWhiteSpace(game.CoverImage))
                    missingCovers++;

                if (string.IsNullOrWhiteSpace(game.BackgroundImage))
                    missingBackgrounds++;
            }

            return new uint[] { missingIcons, missingCovers, missingBackgrounds };
        }

        public long DirectorySize(DirectoryInfo dirInfo)
        {
            long size = 0;

            // Add file sizes
            foreach (FileInfo fileInfo in dirInfo.GetFiles())
                size += fileInfo.Length;

            // Add subdirectory sizes
            foreach (DirectoryInfo directoryInfo in dirInfo.GetDirectories())
                size += DirectorySize(directoryInfo);

            return size;
        }

        private string[] GetFolderSizes()
        {
            string databasePath = PlayniteApi.Database.DatabasePath;
            string cachePath = databasePath.Remove(databasePath.LastIndexOf('\\') + 1) + "cache";

            DirectoryInfo cacheInfo = new DirectoryInfo(cachePath);
            DirectoryInfo databaseInfo = new DirectoryInfo(databasePath);

            long cacheSize = DirectorySize(cacheInfo);
            long databaseSize = DirectorySize(databaseInfo);

            string[] folderSizes = new string[2];
            folderSizes[0] = cacheSize < 1074000000 ? Math.Round((double)cacheSize / 1049000).ToString() + "Mb" : Math.Round((double)cacheSize / 1074000000, 2).ToString() + "Gb";
            folderSizes[1] = databaseSize < 1074000000 ? Math.Round((double)databaseSize / 1049000).ToString() + "Mb" : Math.Round((double)databaseSize / 1074000000, 2).ToString() + "Gb";

            return folderSizes;
        }

        private void ShowWindow()
        {
            uint[] missingData = GetMissingData();
            string[] folderSizes = GetFolderSizes();

            Window window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowMinimizeButton = false,
                ShowMaximizeButton = false
            });

            window.Width = 400;
            window.Height = 300;
            window.Title = "Misc Tools";
            window.ResizeMode = ResizeMode.NoResize;

            string[] data = new string[] { missingData[0].ToString(), missingData[1].ToString(), missingData[2].ToString(), folderSizes[0], folderSizes[1] };

            // Set window content
            window.Content = new MiscToolsMainWindow(PlayniteApi, data);

            // Set owner if you need to create modal dialog window
            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Use Show or ShowDialog to show the window
            window.ShowDialog();
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            return new List<SidebarItem>
            {
                new SidebarItem
                {
                    Title = "Tools",            
                    // Loads icon from plugin's installation path
                    Icon = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "icon.png"),
                    ProgressValue = 0,
                    Type = SiderbarItemType.Button,
                    Activated = () => ShowWindow()
                }
            };
        }
    }
}