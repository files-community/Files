using Files.Common;
using Files.DataModels;
using Files.Enums;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.Storage;
using Windows.System;

namespace Files.ViewModels
{
    [Obsolete("Do not use this class as Settings store anymore, settings have been merged to IUserSettingsService.")]
    public class SettingsViewModel : ObservableObject
    {
        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public SettingsViewModel()
        {
            DetectDateTimeFormat();

            // Load the supported languages
            var supportedLang = ApplicationLanguages.ManifestLanguages;
            DefaultLanguages = new ObservableCollection<DefaultLanguageModel> { new DefaultLanguageModel(null) };
            foreach (var lang in supportedLang)
            {
                DefaultLanguages.Add(new DefaultLanguageModel(lang));
            }
        }

        public static async void OpenLogLocation()
        {
            await Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder);
        }

        public static async void OpenThemesFolder()
        {
            await CoreApplication.MainView.Dispatcher.YieldAsync();
            await NavigationHelpers.OpenPathInNewTab(App.ExternalResourcesHelper.ThemeFolder.Path);
        }

        public static async void ReportIssueOnGitHub()
        {
            await Launcher.LaunchUriAsync(new Uri(@"https://github.com/files-community/Files/issues/new/choose"));
        }

        public bool AreRegistrySettingsMergedToJson
        {
            get => Get(false);
            set => Set(value);
        }

        public async Task DetectQuickLook()
        {
            // Detect QuickLook
            try
            {
                var connection = await AppServiceConnectionHelper.Instance;
                if (connection != null)
                {
                    var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                    {
                        { "Arguments", "DetectQuickLook" }
                    });
                    if (status == AppServiceResponseStatus.Success)
                    {
                        App.MainViewModel.IsQuickLookEnabled = response.Get("IsAvailable", false);
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, ex.Message);
            }
        }

        private void DetectDateTimeFormat()
        {
            if (localSettings.Values[Constants.LocalSettings.DateTimeFormat] != null)
            {
                if (localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString() == "Application")
                {
                    DisplayedTimeStyle = TimeStyle.Application;
                }
                else if (localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString() == "System")
                {
                    DisplayedTimeStyle = TimeStyle.System;
                }
            }
            else
            {
                localSettings.Values[Constants.LocalSettings.DateTimeFormat] = "Application";
            }
        }

        private TimeStyle displayedTimeStyle = TimeStyle.Application;

        public TimeStyle DisplayedTimeStyle
        {
            get => displayedTimeStyle;
            set
            {
                SetProperty(ref displayedTimeStyle, value);
                if (value.Equals(TimeStyle.Application))
                {
                    localSettings.Values[Constants.LocalSettings.DateTimeFormat] = "Application";
                }
                else if (value.Equals(TimeStyle.System))
                {
                    localSettings.Values[Constants.LocalSettings.DateTimeFormat] = "System";
                }
            }
        }

        #region Preferences

        /// <summary>
        /// Gets or sets a value indicating the application language.
        /// </summary>
        public DefaultLanguageModel CurrentLanguage { get; set; } = new DefaultLanguageModel(ApplicationLanguages.PrimaryLanguageOverride);

        /// <summary>
        /// Gets or sets an ObservableCollection of the support languages.
        /// </summary>
        public ObservableCollection<DefaultLanguageModel> DefaultLanguages { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating the default language.
        /// </summary>
        public DefaultLanguageModel DefaultLanguage
        {
            get
            {
                return DefaultLanguages.FirstOrDefault(dl => dl.ID == ApplicationLanguages.PrimaryLanguageOverride) ??
                           DefaultLanguages.FirstOrDefault();
            }
            set
            {
                ApplicationLanguages.PrimaryLanguageOverride = value.ID;
            }
        }

        #endregion Preferences

        #region Appearance

        /// <summary>
        /// Gets or sets the user's current selected skin
        /// </summary>
        public AppTheme SelectedTheme
        {
            get => Newtonsoft.Json.JsonConvert.DeserializeObject<AppTheme>(Get(System.Text.Json.JsonSerializer.Serialize(new AppTheme()
            {
                Name = "Default".GetLocalized()
            })));
            set => Set(Newtonsoft.Json.JsonConvert.SerializeObject(value));
        }

        #endregion Appearance

        /// <summary>
        /// Gets or sets a value indicating whether or not to show a teaching tip informing the user about the status center.
        /// </summary>
        public bool ShowOngoingTasksTeachingTip
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to restore tabs after restarting the app.
        /// </summary>
        public bool ResumeAfterRestart
        {
            get => Get(false);
            set => Set(value);
        }

        public event EventHandler ThemeModeChanged;

        public RelayCommand UpdateThemeElements => new RelayCommand(() =>
        {
            ThemeModeChanged?.Invoke(this, EventArgs.Empty);
        });

        #region ReadAndSaveSettings

        public bool Set<TValue>(TValue value, [CallerMemberName] string propertyName = null)
        {
            propertyName = propertyName != null && propertyName.StartsWith("set_", StringComparison.OrdinalIgnoreCase)
                ? propertyName.Substring(4)
                : propertyName;

            TValue originalValue = default;

            if (localSettings.Values.ContainsKey(propertyName))
            {
                originalValue = Get(originalValue, propertyName);

                localSettings.Values[propertyName] = value;
                if (!base.SetProperty(ref originalValue, value, propertyName))
                {
                    return false;
                }
            }
            else
            {
                localSettings.Values[propertyName] = value;
            }

            return true;
        }

        public TValue Get<TValue>(TValue defaultValue, [CallerMemberName] string propertyName = null)
        {
            var name = propertyName ??
                       throw new ArgumentNullException(nameof(propertyName), "Cannot store property of unnamed.");

            name = name.StartsWith("get_", StringComparison.OrdinalIgnoreCase)
                ? propertyName.Substring(4)
                : propertyName;

            if (localSettings.Values.ContainsKey(name))
            {
                var value = localSettings.Values[name];

                if (!(value is TValue tValue))
                {
                    if (value is IConvertible)
                    {
                        tValue = (TValue)Convert.ChangeType(value, typeof(TValue));
                    }
                    else
                    {
                        var valueType = value.GetType();
                        var tryParse = typeof(TValue).GetMethod("TryParse", BindingFlags.Instance | BindingFlags.Public);

                        if (tryParse == null)
                        {
                            return default;
                        }

                        var stringValue = value.ToString();
                        tValue = default;

                        var tryParseDelegate =
                            (TryParseDelegate<TValue>)Delegate.CreateDelegate(valueType, tryParse, false);

                        tValue = (tryParseDelegate?.Invoke(stringValue, out tValue) ?? false) ? tValue : default;
                    }

                    Set(tValue, propertyName); // Put the corrected value in settings.
                    return tValue;
                }
                return tValue;
            }

            localSettings.Values[propertyName] = defaultValue;

            return defaultValue;
        }

        private delegate bool TryParseDelegate<TValue>(string inValue, out TValue parsedValue);

        #endregion ReadAndSaveSettings
    }
}
