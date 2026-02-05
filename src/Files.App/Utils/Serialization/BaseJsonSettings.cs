// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

namespace Files.App.Utils.Serialization
{
	/// <summary>
	/// A base class to easily manage all application's settings.
	/// </summary>
	internal abstract class BaseJsonSettings : ISettingsSharingContext
	{
		private ISettingsSharingContext? _settingsSharingContext;

		public bool IsAvailable { get; protected set; }

		private ISettingsSerializer? _SettingsSerializer;
		protected ISettingsSerializer? SettingsSerializer
		{
			get => _settingsSharingContext?.Instance?.SettingsSerializer ?? _SettingsSerializer;
			set => _SettingsSerializer = value;
		}

		private IJsonSettingsSerializer? _JsonSettingsSerializer;
		protected IJsonSettingsSerializer? JsonSettingsSerializer
		{
			get => _settingsSharingContext?.Instance?.JsonSettingsSerializer ?? _JsonSettingsSerializer;
			set => _JsonSettingsSerializer = value;
		}

		private IJsonSettingsDatabase? _JsonSettingsDatabase;
		protected IJsonSettingsDatabase? JsonSettingsDatabase
		{
			get => _settingsSharingContext?.Instance?.JsonSettingsDatabase ?? _JsonSettingsDatabase;
			set => _JsonSettingsDatabase = value;
		}

		BaseJsonSettings ISettingsSharingContext.Instance => this;

		public event EventHandler<SettingChangedEventArgs>? OnSettingChangedEvent;

		public virtual bool FlushSettings()
		{
			return JsonSettingsDatabase?.FlushSettings() ?? false;
		}

		public virtual object ExportSettings()
		{
			return JsonSettingsDatabase?.ExportSettings() ?? false;
		}

		public virtual bool ImportSettings(object import)
		{
			return JsonSettingsDatabase?.ImportSettings(import) ?? false;
		}

		public bool RegisterSettingsContext(ISettingsSharingContext settingsSharingContext)
		{
			if (_settingsSharingContext is null)
			{
				// Can register only once
				_settingsSharingContext = settingsSharingContext;
				IsAvailable = settingsSharingContext.Instance.IsAvailable;
				return true;
			}

			return false;
		}

		public ISettingsSharingContext GetSharingContext()
		{
			return _settingsSharingContext ?? this;
		}

		protected virtual void Initialize(string filePath)
		{
			IsAvailable = SettingsSerializer?.CreateFile(filePath) ?? false;
		}

		protected virtual TValue? Get<TValue>(TValue? defaultValue, [CallerMemberName] string propertyName = "")
		{
			if (string.IsNullOrEmpty(propertyName))
			{
				return defaultValue;
			}

			return JsonSettingsDatabase is null ? defaultValue : JsonSettingsDatabase.GetValue(propertyName, defaultValue) ?? defaultValue;
		}

		protected virtual bool Set<TValue>(TValue? value, [CallerMemberName] string propertyName = "")
		{
			if (string.IsNullOrEmpty(propertyName))
			{
				return false;
			}

			// Sanitize special double values to prevent JSON serialization errors
			var sanitizedValue = SanitizeValue(value);

			if (JsonSettingsDatabase is not null &&
				(!JsonSettingsDatabase.GetValue<TValue>(propertyName)?.Equals(sanitizedValue) ?? true) &&
				JsonSettingsDatabase.SetValue(propertyName, sanitizedValue))
			{
				RaiseOnSettingChangedEvent(this, new SettingChangedEventArgs(propertyName, sanitizedValue));
				return true;
			}

			return false;
		}

		private static TValue? SanitizeValue<TValue>(TValue? value)
		{
			// Handle double values
			if (value is double doubleValue)
			{
				if (double.IsInfinity(doubleValue) || double.IsNaN(doubleValue))
				{
					return default;
				}
			}
			// Handle float values
			else if (value is float floatValue)
			{
				if (float.IsInfinity(floatValue) || float.IsNaN(floatValue))
				{
					return default;
				}
			}

			return value;
		}

		protected virtual void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			OnSettingChangedEvent?.Invoke(sender, e);
			_settingsSharingContext?.Instance.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
