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
    public abstract class BaseJsonSettingsModel : IJsonSettingsContext
    {
        #region Protected Members

        protected readonly string filePath;

        protected int registeredMembers = 0;

        protected IJsonSettingsContext jsonSettingsContext;

        protected readonly IJsonSettingsSerializer jsonSettingsSerializer;

        protected readonly ISettingsSerializer settingsSerializer;

        #endregion Protected Members

        #region Properties

        public IJsonSettingsDatabase JsonSettingsDatabase { get; protected set; }

        #endregion Properties

        #region Constructor

        public BaseJsonSettingsModel(string filePath)
            : this (filePath, null, null, null)
        {
        }

        public BaseJsonSettingsModel(string filePath, bool isCachingEnabled,
            IJsonSettingsSerializer jsonSettingsSerializer = null,
            ISettingsSerializer settingsSerializer = null)
        {
            this.filePath = filePath;
            Initialize();

            this.jsonSettingsSerializer = jsonSettingsSerializer;
            this.settingsSerializer = settingsSerializer;

            // Fallback
            this.jsonSettingsSerializer ??= new DefaultJsonSettingsSerializer();
            this.settingsSerializer ??= new DefaultSettingsSerializer(this.filePath);

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
            this.filePath = filePath;
            Initialize();

            this.jsonSettingsSerializer = jsonSettingsSerializer;
            this.settingsSerializer = settingsSerializer;
            this.JsonSettingsDatabase = jsonSettingsDatabase;

            // Fallback
            this.jsonSettingsSerializer ??= new DefaultJsonSettingsSerializer();
            this.settingsSerializer ??= new DefaultSettingsSerializer(this.filePath);
            this.JsonSettingsDatabase ??= new DefaultJsonSettingsDatabase(this.jsonSettingsSerializer, this.settingsSerializer);
        }

        #endregion Constructor

        #region Helpers

        protected virtual void Initialize()
        {
            // Create the file
            NativeFileOperationsHelper.CreateFileForWrite(filePath, false).Dispose();
        }

        public virtual object ExportSettings()
        {
            return (jsonSettingsContext?.JsonSettingsDatabase ?? JsonSettingsDatabase)?.ExportSettings();
        }

        public virtual void ImportSettings(object import)
        {
            (jsonSettingsContext?.JsonSettingsDatabase ?? JsonSettingsDatabase)?.ImportSettings(import);
        }

        public bool RegisterSettingsContext(IJsonSettingsContext jsonSettingsContext)
        {
            this.jsonSettingsContext = jsonSettingsContext;
            return true;
        }

        public IJsonSettingsContext GetContext()
        {
            registeredMembers++;
            return this;
        }

        #endregion Helpers

        #region Get, Set

        protected virtual TValue Get<TValue>(TValue defaultValue, [CallerMemberName] string propertyName = "")
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return defaultValue;
            }

            object value = (jsonSettingsContext?.JsonSettingsDatabase ?? JsonSettingsDatabase).GetValue(propertyName, defaultValue);

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

            return (jsonSettingsContext?.JsonSettingsDatabase ?? JsonSettingsDatabase).UpdateKey(propertyName, value);
        }

        #endregion Get, Set
    }
}