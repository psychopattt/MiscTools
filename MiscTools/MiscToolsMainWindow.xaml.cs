using Playnite.SDK.Controls;
using Playnite.SDK;
using System.Diagnostics;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

namespace MiscTools
{
    public partial class MiscToolsMainWindow : PluginUserControl
    {
        private readonly string[] generatedTags = new string[] { "Missing Media", "Large Media" };
        private readonly Regex aboutGameRegex = new Regex(@"<h1>About the Game<\/h1>([\s\S]+)", RegexOptions.Compiled);
        private readonly Regex imgRegex = new Regex(@"(<[ap][^>]*>)?(<br>|<br\/>)?<img[^>]*>(<br>|<br\/>)?(<\/[ap]>)?", RegexOptions.Compiled);

        private static readonly ILogger logger = LogManager.GetLogger();
        private IPlayniteAPI PlayniteApi;

        public MiscToolsMainWindow(IPlayniteAPI playniteApi, string[] data)
        {
            InitializeComponent();

            PlayniteApi = playniteApi;

            txtMissingIcons.Content = data[0];
            txtMissingCovers.Content = data[1];
            txtMissingBackgrounds.Content = data[2];

            txtCacheSize.Content = data[3];
            txtDatabaseSize.Content = data[4];
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

        private void lblCacheSize_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowCacheDirectory();
        }

        private void txtCacheSize_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowCacheDirectory();
        }

        private void lblDatabaseSize_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowDatabaseDirectory();
        }

        private void txtDatabaseSize_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
            uint updateCount = 0;

            foreach (Game game in PlayniteApi.Database.Games)
            {
                if (!string.IsNullOrWhiteSpace(game.Description))
                {
                    bool updated = false;
                    string description = game.Description;

                    Match aboutGameMatch = aboutGameRegex.Match(description);

                    if (aboutGameMatch.Success)
                    {
                        description = aboutGameMatch.Groups[1].Value;
                        updated = true;
                    }

                    Match imgMatch = imgRegex.Match(description);

                    if (imgMatch.Success)
                    {
                        int startIndex = 0;
                        StringBuilder descBuilder = new StringBuilder();

                        while (imgMatch.Success)
                        {
                            int length = imgMatch.Index - startIndex;
                            descBuilder.Append(description.Substring(startIndex, length)); // Add everything between the last match and the current match
                            startIndex += length + imgMatch.Value.Length;
                            imgMatch = imgMatch.NextMatch();
                        }

                        descBuilder.Append(description.Substring(startIndex)); // Add everything after the last match
                        description = descBuilder.ToString();
                        updated = true;
                    }

                    if (updated)
                    {
                        game.Description = description;
                        PlayniteApi.Database.Games.Update(game);
                        updateCount++;
                    }
                }
            }

            PlayniteApi.Dialogs.ShowMessage(string.Format("{0} game description{1} been updated.", updateCount, updateCount != 1 ? "s have" : " has"), "Description Cleanup");
        }

        private void btnLargeMedia_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            uint updateCount = 0;
            Guid tagGuid = GetTagId("Large Media");

            foreach (Game game in PlayniteApi.Database.Games)
            {
                long maxSize = 1000; // kb

                string gamePath = PlayniteApi.Database.DatabasePath + "\\files\\" + game.Id.ToString();
                long size = Utilities.DirectorySize(new DirectoryInfo(gamePath)) / 1024;

                if (size > maxSize)
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