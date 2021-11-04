using Playnite.SDK.Controls;
using Playnite.SDK;
using System.Diagnostics;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;

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

        private void btnMissingMedia_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Guid tagGuid = GetTagId("Missing Media");

            foreach (Game game in PlayniteApi.Database.Games)
            {
                if (string.IsNullOrWhiteSpace(game.Icon) || string.IsNullOrWhiteSpace(game.CoverImage) || string.IsNullOrWhiteSpace(game.BackgroundImage))
                {
                    if (game.TagIds == null) // No tags
                    {
                        game.TagIds = new List<Guid> { tagGuid };
                        PlayniteApi.Database.Games.Update(game);
                    }
                    else if (!game.TagIds.Contains(tagGuid)) // No "Missing Media" tag
                    {
                        game.TagIds.Add(tagGuid);
                        PlayniteApi.Database.Games.Update(game);
                    }
                }
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