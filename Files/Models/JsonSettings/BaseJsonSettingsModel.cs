using Files.Helpers;
using Files.Models.JsonSettings.Implementation;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;

namespace Files.Models.JsonSettings
{
    /// <summary>
    /// Clipboard Canvas
    /// A base class to easily manage all application's settings.
    /// </summary>
    public abstract class BaseJsonSettingsModel : ISettingsSharingContext
    {
        #region Protected Members

        protected int registeredMembers = 0;

        protected ISettingsSharingContext settingsSharingContext;

        protected readonly IJsonSettingsSerializer jsonSettingsSerializer;

        protected readonly ISettingsSerializer settingsSerializer;

        #endregion Protected Members

        #region Properties

        private string _FilePath;
        public string FilePath
        {
            get => settingsSharingContext?.FilePath ?? _FilePath;
            protected set => _FilePath = value;
        }

        private IJsonSettingsDatabase _JsonSettingsDatabase;
        public IJsonSettingsDatabase JsonSettingsDatabase
        {
            get => settingsSharingContext?.JsonSettingsDatabase ?? _JsonSettingsDatabase;
            protected set => _JsonSettingsDatabase = value;
        }

        #endregion Properties

        #region Constructor

        public BaseJsonSettingsModel()
        {
        }

        public BaseJsonSettingsModel(string filePath)
            : this (filePath, null, null, null)
        {
        }

        public BaseJsonSettingsModel(ISettingsSharingContext settingsSharingContext)
        {
            RegisterSettingsContext(settingsSharingContext);
            Initialize();
        }

        public BaseJsonSettingsModel(string filePath, bool isCachingEnabled,
            IJsonSettingsSerializer jsonSettingsSerializer = null,
            ISettingsSerializer settingsSerializer = null)
        {
            this.FilePath = filePath;
            Initialize();

            this.jsonSettingsSerializer = jsonSettingsSerializer;
            this.settingsSerializer = settingsSerializer;

            // Fallback
            this.jsonSettingsSerializer ??= new DefaultJsonSettingsSerializer();
            this.settingsSerializer ??= new DefaultSettingsSerializer(this.FilePath);

            if (isCachingEnabled)
            {
                this.JsonSettingsDatabase = new CachingJsonSettingsDatabase(this.jsonSettingsSerializer, this.settingsSerializer);
            }
            else
            {
                this.JsonSettingsDatabase = new DefaultJsonSettingsDatabase(this.jsonSettingsSerializer, this.settingsSerializer);
            }
        }

        public BaseJsonSettingsModel(string filePath,
            IJsonSettingsSerializer jsonSettingsSerializer,
            ISettingsSerializer settingsSerializer,
            IJsonSettingsDatabase jsonSettingsDatabase)
        {
            this.FilePath = filePath;
            Initialize();

            this.jsonSettingsSerializer = jsonSettingsSerializer;
            this.settingsSerializer = settingsSerializer;
            this.JsonSettingsDatabase = jsonSettingsDatabase;

            // Fallback
            this.jsonSettingsSerializer ??= new DefaultJsonSettingsSerializer();
            this.settingsSerializer ??= new DefaultSettingsSerializer(this.FilePath);
            this.JsonSettingsDatabase ??= new DefaultJsonSettingsDatabase(this.jsonSettingsSerializer, this.settingsSerializer);
        }

        #endregion Constructor

        #region Helpers

        protected virtual void Initialize()
        {
            // Create the file
            NativeFileOperationsHelper.CreateFileForWrite((settingsSharingContext?.FilePath ?? FilePath), false).Dispose();
        }

        public virtual object ExportSettings()
        {
            return (settingsSharingContext?.JsonSettingsDatabase ?? JsonSettingsDatabase)?.ExportSettings();
        }

        public virtual void ImportSettings(object import)
        {
            (settingsSharingContext?.JsonSettingsDatabase ?? JsonSettingsDatabase)?.ImportSettings(import);
        }

        public bool RegisterSettingsContext(ISettingsSharingContext settingsSharingContext)
        {
            this.settingsSharingContext = settingsSharingContext;
            return true;
        }

        public ISettingsSharingContext GetContext()
        {
            registeredMembers++;
            return settingsSharingContext ?? this;
        }

        #endregion Helpers

        #region Get, Set

        protected virtual TValue Get<TValue>(TValue defaultValue, [CallerMemberName] string propertyName = "")
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return defaultValue;
            }

            object value = (settingsSharingContext?.JsonSettingsDatabase ?? JsonSettingsDatabase).GetValue(propertyName, defaultValue);

            if (value is JToken jTokenValue)
            {
                return jTokenValue.ToObject<TValue>();
            }
            else
            {
                return (TValue)value;
            }
        }

        protected virtual bool Set<TValue>(TValue value, [CallerMemberName] string propertyName = "")
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            return (settingsSharingContext?.JsonSettingsDatabase ?? JsonSettingsDatabase).UpdateKey(propertyName, value);
        }

        #endregion Get, Set
    }
}