using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MiscTools
{
    public static class Utilities
    {
        private static readonly uint progressBarThreshold = 300;
        private static readonly Regex aboutGameRegex = new Regex(@"<h1>About the Game<\/h1>([\s\S]+)", RegexOptions.Compiled);
        private static readonly Regex imgRegex = new Regex(@"(<[ap][^>]*>)?(<br>|<br\/>)?<img[^>]*>(<br>|<br\/>)?(<\/[ap]>)?", RegexOptions.Compiled);

        public static long DirectorySize(DirectoryInfo dirInfo)
        {
            if (!dirInfo.Exists)
                return 0;

            long size = 0;

            // Add file sizes
            foreach (FileInfo fileInfo in dirInfo.GetFiles())
                size += fileInfo.Length;

            // Add subdirectory sizes
            foreach (DirectoryInfo directoryInfo in dirInfo.GetDirectories())
                size += DirectorySize(directoryInfo);

            return size;
        }

        public static string[] GetMissingMediaExcludedCategories(MiscToolsSettings settings)
        {
            return settings.MissingMediaExclusions
                .Split('\n')
                .Select(x => x.Trim().ToLower())
                .Where(x => x != "")
                .ToArray();
        }

        public static bool GetGameExcludedFromMissingMedia(Game game, string[] excludedCategories)
        {
            return game.Categories != null &&
                game.Categories.Exists(category => excludedCategories.Contains(category.Name.Trim().ToLower()));
        }

        public static uint CleanDescriptions(IPlayniteAPI playniteApi, IEnumerable<Game> games)
        {
            if (games.Count() < progressBarThreshold) // Don't show progress bar if there's less than X games to update
                return CleanDescriptionsSilent(playniteApi, games);
            else
                return CleanDescriptionsProgress(playniteApi, games);
        }

        private static uint CleanDescriptionsSilent(IPlayniteAPI playniteApi, IEnumerable<Game> games)
        {
            uint updateCount = 0;

            foreach (Game game in games)
            {
                if (CleanDescription(playniteApi, game))
                    updateCount++;
            }

            return updateCount;
        }

        private static uint CleanDescriptionsProgress(IPlayniteAPI playniteApi, IEnumerable<Game> games)
        {
            uint updateCount = 0;
            GlobalProgressOptions progressOptions = new GlobalProgressOptions("Misc Tools - Cleaning Descriptions", true);
            progressOptions.IsIndeterminate = false;

            playniteApi.Dialogs.ActivateGlobalProgress(progressBar =>
            {
                progressBar.ProgressMaxValue = games.Count();

                foreach (Game game in games)
                {
                    if (CleanDescription(playniteApi, game))
                        updateCount++;

                    progressBar.CurrentProgressValue++;

                    if (progressBar.CancelToken.IsCancellationRequested)
                        break; // Stop cleaning games
                }

            }, progressOptions);

            return updateCount;
        }

        private static bool CleanDescription(IPlayniteAPI playniteApi, Game game)
        {
            bool updated = false;

            if (!string.IsNullOrWhiteSpace(game.Description))
            {
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
                    playniteApi.Database.Games.Update(game);
                }
            }

            return updated;
        }
    }
}