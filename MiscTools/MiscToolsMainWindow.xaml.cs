using Playnite.SDK.Controls;
using Playnite.SDK;
using System.Diagnostics;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace MiscTools
{
    public partial class MiscToolsMainWindow : PluginUserControl
    {
        private readonly string[] generatedTags = new string[] { "Missing Media", "Large Media" };
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
                if (tag.Name == tagName)
                    tagGuid = tag.Id;

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

        private void AddGameTag(Game game, Guid newTagGuid)
        {
            if (game.TagIds == null) // No tags
            {
                game.TagIds = new List<Guid> { newTagGuid };
                PlayniteApi.Database.Games.Update(game);
            }
            else if (!game.TagIds.Contains(newTagGuid)) // No "Missing Media" tag
            {
                game.TagIds.Add(newTagGuid);
                PlayniteApi.Database.Games.Update(game);
            }
        }

        private void btnMissingMedia_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Guid tagGuid = GetTagId("Missing Media");

            foreach (Game game in PlayniteApi.Database.Games)
            {
                if (string.IsNullOrWhiteSpace(game.Icon) || string.IsNullOrWhiteSpace(game.CoverImage) || string.IsNullOrWhiteSpace(game.BackgroundImage))
                {
                    AddGameTag(game, tagGuid);
                }
            }
        }

        private void btnLargeMedia_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Guid tagGuid = GetTagId("Large Media");

            foreach (Game game in PlayniteApi.Database.Games)
            {
                long maxSize = 1000; // kb

                string gamePath = PlayniteApi.Database.DatabasePath + "\\files\\" + game.Id.ToString();
                long size = Utilities.DirectorySize(new DirectoryInfo(gamePath)) / 1024;

                if (size > maxSize)
                    AddGameTag(game, tagGuid);
            }
        }

        private void btnCleanTags_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (string tagName in generatedTags)
            {
                Guid tagGuid = FindTagId(tagName);
                
                if (tagGuid != new Guid()) // Tag exists
                    PlayniteApi.Database.Tags.Remove(tagGuid);
            }
        }
    }
}