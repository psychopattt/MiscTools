using Playnite.SDK.Controls;
using Playnite.SDK;
using System.Diagnostics;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;

namespace MiscTools
{
    public partial class MiscToolsMainWindow : PluginUserControl
    {
        MiscToolsSettings settings;
        private readonly string[] generatedTags = new string[] { "Missing Media", "Large Media" };

        private static readonly ILogger logger = LogManager.GetLogger();
        private IPlayniteAPI PlayniteApi;

        public MiscToolsMainWindow(IPlayniteAPI playniteApi, MiscToolsSettings settings, string[] data)
        {
            InitializeComponent();

            PlayniteApi = playniteApi;
            this.settings = settings;

            txtMissingIcons.Content = data[0];
            txtMissingCovers.Content = data[1];
            txtMissingBackgrounds.Content = data[2];

            txtCacheSize.Content = data[3];
            txtDatabaseSize.Content = data[4];

            btnLargeMedia.ToolTip = string.Format("Adds a \"Large Media\" tag to every game whose directory is bigger than {0}kb", settings.LargeMediaThreshold);
        }

        private void ShowCacheDirectory()
        {
            string databasePath = PlayniteApi.Database.DatabasePath;
            string cachePath = databasePath.Remove(databasePath.LastIndexOf('\\') + 1) + "cache";
            Process.Start(cachePath);
        }

        private void ShowDatabaseDirectory()
        {
            string databasePath = PlayniteApi.Database.DatabasePath;
            Process.Start(databasePath);
        }

        private void lblCacheSize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShowCacheDirectory();
        }

        private void txtCacheSize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShowCacheDirectory();
        }

        private void lblDatabaseSize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShowDatabaseDirectory();
        }

        private void txtDatabaseSize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShowDatabaseDirectory();
        }

        // Find and return Guid
        private Guid FindTagId(string tagName)
        {
            Guid tagGuid = new Guid();

            // Check if the tag already exists
            foreach (Tag tag in PlayniteApi.Database.Tags)
            {
                if (tag.Name == tagName)
                    tagGuid = tag.Id;
            }

            return tagGuid;
        }

        // Find or create tag and return Guid
        private Guid GetTagId(string tagName)
        {
            Guid tagGuid = FindTagId(tagName);

            if (tagGuid == new Guid()) // Tag does not exist
            {
                Tag tag = new Tag(tagName);
                PlayniteApi.Database.Tags.Add(tag);
                tagGuid = tag.Id;
            }

            return tagGuid;
        }

        // Returns true if the tag has been added
        private bool AddGameTag(Game game, Guid newTagGuid)
        {
            if (game.TagIds == null) // No tags
            {
                game.TagIds = new List<Guid> { newTagGuid };
                PlayniteApi.Database.Games.Update(game);
                return true;
            }
            else if (!game.TagIds.Contains(newTagGuid)) // Does not have the tag
            {
                game.TagIds.Add(newTagGuid);
                PlayniteApi.Database.Games.Update(game);
                return true;
            }

            return false; // Game already has tag
        }

        private void btnMissingMedia_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            uint updateCount = 0;
            Guid tagGuid = GetTagId("Missing Media");

            foreach (Game game in PlayniteApi.Database.Games)
            {
                if (string.IsNullOrWhiteSpace(game.Icon) || string.IsNullOrWhiteSpace(game.CoverImage) || string.IsNullOrWhiteSpace(game.BackgroundImage))
                {
                    if (AddGameTag(game, tagGuid))
                        updateCount++;
                }
            }

            PlayniteApi.Dialogs.ShowMessage(string.Format("{0} game{1} been updated.", updateCount, updateCount != 1 ? "s have" : " has"), "Missing Media Tags");
        }

        private void btnCleanDesc_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            uint updateCount = Utilities.CleanDescriptions(PlayniteApi, PlayniteApi.Database.Games);
            PlayniteApi.Dialogs.ShowMessage(string.Format("{0} game description{1} been updated.", updateCount, updateCount != 1 ? "s have" : " has"), "Description Cleanup");
        }

        private void btnLargeMedia_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            uint updateCount = 0;
            Guid tagGuid = GetTagId("Large Media");

            foreach (Game game in PlayniteApi.Database.Games)
            {
                string gamePath = PlayniteApi.Database.DatabasePath + "\\files\\" + game.Id.ToString();
                long size = Utilities.DirectorySize(new DirectoryInfo(gamePath)) / 1024;

                if (size > settings.LargeMediaThreshold)
                {
                    if (AddGameTag(game, tagGuid))
                        updateCount++;
                }
            }

            PlayniteApi.Dialogs.ShowMessage(string.Format("{0} game{1} been updated.", updateCount, updateCount != 1 ? "s have" : " has"), "Large Media Tags");
        }

        private void btnCleanTags_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            byte updateCount = 0;

            foreach (string tagName in generatedTags)
            {
                Guid tagGuid = FindTagId(tagName);
                
                if (tagGuid != new Guid()) // Tag exists
                {
                    PlayniteApi.Database.Tags.Remove(tagGuid);
                    updateCount++;
                }
            }

            PlayniteApi.Dialogs.ShowMessage(string.Format("{0} tag{1} been deleted.", updateCount, updateCount != 1 ? "s have" : " has"), "Tag Cleaner");
        }
    }
}