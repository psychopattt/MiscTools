using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace MiscTools
{
    public class MiscToolsSettings : ObservableObject
    {
        private long largeMediaThreshold = 1024;
        private bool cleanNewGames = false;
        private bool clearCache = false;

        public long LargeMediaThreshold { get => largeMediaThreshold; set => SetValue(ref largeMediaThreshold, value); }
        public bool CleanNewGames { get => cleanNewGames; set => SetValue(ref cleanNewGames, value); }
        public bool ClearCache { get => clearCache; set => SetValue(ref clearCache, value); }
    }

    public class MiscToolsSettingsViewModel : ObservableObject, ISettings
    {
        private readonly MiscTools plugin;
        private MiscToolsSettings settings;

        private MiscToolsSettings editingClone { get; set; }

        public MiscToolsSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public MiscToolsSettingsViewModel(MiscTools plugin)
        {
            this.plugin = plugin;

            MiscToolsSettings savedSettings = plugin.LoadPluginSettings<MiscToolsSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
                Settings = savedSettings;
            else
                Settings = new MiscToolsSettings();
        }

        public void BeginEdit()
        {
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            Settings = editingClone;
        }

        public void EndEdit()
        {
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // No errors possible AFAIK
            errors = new List<string>();
            return true;
        }
    }
}