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

        // TODO maybe put some things in other functions (tag exist, create tag, ...)
        private void btnMissingMedia_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            bool tagExists = false;
            Guid tagGuid = new Guid();

            // Check if the tag already exists
            foreach (Tag tag in PlayniteApi.Database.Tags)
            {
                if (tag.Name == "Missing Media")
                {
                    tagExists = true;
                    tagGuid = tag.Id;
                }
            }

            if (!tagExists)
            {
                Tag tag = new Tag("Missing Media");
                PlayniteApi.Database.Tags.Add(tag);
                tagGuid = tag.Id;
            }

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
    }
}
