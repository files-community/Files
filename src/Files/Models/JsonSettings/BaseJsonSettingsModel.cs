using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using Files.Helpers;
using Files.Models.JsonSettings.Implementation;
using System;
using Files.EventArguments;
using System.IO;

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

        #region Events

        public event EventHandler<SettingChangedEventArgs> OnSettingChangedEvent;

        #endregion Events

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
            NativeFileOperationsHelper.CreateDirectoryFromApp(Path.GetDirectoryName(FilePath), IntPtr.Zero);
            NativeFileOperationsHelper.CreateFileForWrite(FilePath, false).Dispose();
        }

        public virtual bool FlushSettings()
        {
            return JsonSettingsDatabase.FlushSettings();
        }

        public virtual object ExportSettings()
        {
            return JsonSettingsDatabase.ExportSettings();
        }

        public virtual bool ImportSettings(object import)
        {
            return JsonSettingsDatabase.ImportSettings(import);
        }

        public bool RegisterSettingsContext(ISettingsSharingContext settingsSharingContext)
        {
            if (this.settingsSharingContext == null)
            {
                // Can set only once
                this.settingsSharingContext = settingsSharingContext;
                return true;
            }

            return false;
        }

        public ISettingsSharingContext GetSharingContext()
        {
            registeredMembers++;
            return settingsSharingContext ?? this;
        }

        public virtual void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            OnSettingChangedEvent?.Invoke(sender, e);
            settingsSharingContext?.RaiseOnSettingChangedEvent(sender, e);
        }

        #endregion Helpers

        #region Get, Set

        protected virtual TValue Get<TValue>(TValue defaultValue, [CallerMemberName] string propertyName = "")
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return defaultValue;
            }

            return JsonSettingsDatabase.GetValue<TValue>(propertyName, defaultValue);
        }

        protected virtual bool Set<TValue>(TValue value, [CallerMemberName] string propertyName = "")
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            if (JsonSettingsDatabase.UpdateKey(propertyName, value))
            {
                RaiseOnSettingChangedEvent(this, new SettingChangedEventArgs(propertyName, value));
                return true;
            }

            return false;
        }

        #endregion Get, Set
    }
}