using Playnite.SDK.Controls;
using Playnite.SDK;
using System.Diagnostics;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using System.Linq;

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

            btnLargeMedia.ToolTip = string.Format("Adds a \"Large Media\" tag to every game whose directory is bigger than {0}KB", settings.LargeMediaThreshold);
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
            GlobalProgressOptions progressOptions = new GlobalProgressOptions("Misc Tools - Adding \"Missing Media\" tags", true);
            progressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress(progressBar =>
            {
                Guid tagGuid = GetTagId("Missing Media");
                progressBar.ProgressMaxValue = PlayniteApi.Database.Games.Count();
                string[] excludedCategories = Utilities.GetMissingMediaExcludedCategories(settings);

                foreach (Game game in PlayniteApi.Database.Games)
                {
                    if (string.IsNullOrWhiteSpace(game.Icon) || string.IsNullOrWhiteSpace(game.CoverImage) || string.IsNullOrWhiteSpace(game.BackgroundImage))
                    {
                        if (!Utilities.GetGameExcludedFromMissingMedia(game, excludedCategories))
                        {
                            if (AddGameTag(game, tagGuid))
                                updateCount++;
                        }
                    }

                    progressBar.CurrentProgressValue++;

                    if (progressBar.CancelToken.IsCancellationRequested)
                        break; // Stop adding tags
                }

            }, progressOptions);

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
            GlobalProgressOptions progressOptions = new GlobalProgressOptions("Misc Tools - Adding \"Large Media\" tags", true);
            progressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress(progressBar =>
            {
                Guid tagGuid = GetTagId("Large Media");
                string gamesPath = PlayniteApi.Database.DatabasePath + "\\files\\";
                progressBar.ProgressMaxValue = PlayniteApi.Database.Games.Count();

                foreach (Game game in PlayniteApi.Database.Games)
                {
                    long size = Utilities.DirectorySize(new DirectoryInfo(gamesPath + game.Id.ToString())) / 1024;

                    if (size > settings.LargeMediaThreshold)
                    {
                        if (AddGameTag(game, tagGuid))
                            updateCount++;
                    }

                    progressBar.CurrentProgressValue++;

                    if (progressBar.CancelToken.IsCancellationRequested)
                        break; // Stop adding tags
                }

            }, progressOptions);

            PlayniteApi.Dialogs.ShowMessage(string.Format("{0} game{1} been updated.", updateCount, updateCount != 1 ? "s have" : " has"), "Large Media Tags");
        }

        private void btnCleanTags_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            byte updateCount = 0;
            GlobalProgressOptions progressOptions = new GlobalProgressOptions("Misc Tools - Removing generated tags", true);
            progressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress(progressBar =>
            {
                progressBar.ProgressMaxValue = generatedTags.Length;

                foreach (string tagName in generatedTags)
                {
                    Guid tagGuid = FindTagId(tagName);
                
                    if (tagGuid != new Guid()) // Tag exists
                    {
                        PlayniteApi.Database.Tags.Remove(tagGuid);
                        updateCount++;
                    }

                    progressBar.CurrentProgressValue++;

                    if (progressBar.CancelToken.IsCancellationRequested)
                        break; // Stop removing tags
                }

            }, progressOptions);

            PlayniteApi.Dialogs.ShowMessage(string.Format("{0} tag{1} been deleted.", updateCount, updateCount != 1 ? "s have" : " has"), "Tag Cleaner");
        }
    }
}