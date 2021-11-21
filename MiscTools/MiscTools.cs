using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            if (settings.Settings.CleanNewGames) // Clean game descriptions on library update
            {
                // Only clean descriptions for games added in the last 20 minutes
                var recentlyAddedGames = PlayniteApi.Database.Games.Where(x => x.Added != null && x.Added > DateTime.Now.AddMinutes(-20));
                Utilities.CleanDescriptions(PlayniteApi, recentlyAddedGames);
            }
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            if (settings.Settings.ClearCache)
            {
                string databasePath = PlayniteApi.Database.DatabasePath;
                string cachePath = databasePath.Remove(databasePath.LastIndexOf('\\') + 1) + "cache";
                Directory.Delete(cachePath, true);
            }
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

        private string[] GetFolderSizes()
        {
            string databasePath = PlayniteApi.Database.DatabasePath;
            string cachePath = databasePath.Remove(databasePath.LastIndexOf('\\') + 1) + "cache";

            DirectoryInfo cacheInfo = new DirectoryInfo(cachePath);
            DirectoryInfo databaseInfo = new DirectoryInfo(databasePath);

            long cacheSize = Utilities.DirectorySize(cacheInfo);
            long databaseSize = Utilities.DirectorySize(databaseInfo);

            string[] folderSizes = new string[2];
            folderSizes[0] = cacheSize < 1074000000 ? Math.Round((double)cacheSize / 1049000).ToString() + "Mb" : Math.Round((double)cacheSize / 1074000000, 2).ToString() + "Gb";
            folderSizes[1] = databaseSize < 1074000000 ? Math.Round((double)databaseSize / 1049000).ToString() + "Mb" : Math.Round((double)databaseSize / 1074000000, 2).ToString() + "Gb";

            return folderSizes;
        }

        private void ShowWindow()
        {
            Window window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowMinimizeButton = false,
                ShowMaximizeButton = false
            });

            window.Width = 400;
            window.Height = 300;
            window.Title = "Misc Tools";
            window.ResizeMode = ResizeMode.NoResize;
            
            string[] data = new string[0];
            GlobalProgressOptions progressOptions = new GlobalProgressOptions("Misc Tools - Gathering data", false);
            progressOptions.IsIndeterminate = true;

            PlayniteApi.Dialogs.ActivateGlobalProgress((progressBar) =>
            {
                uint[] missingData = GetMissingData();
                string[] folderSizes = GetFolderSizes();
                data = new string[] { missingData[0].ToString(), missingData[1].ToString(), missingData[2].ToString(), folderSizes[0], folderSizes[1] };
            }, progressOptions);

            // Set window content
            window.Content = new MiscToolsMainWindow(PlayniteApi, settings.Settings, data);

            // Set owner if you need to create modal dialog window
            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Close window when "esc" is pressed
            window.PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) { window.Close(); } };

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