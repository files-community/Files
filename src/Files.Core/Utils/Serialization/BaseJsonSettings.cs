// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Files.Core.Utils.Serialization
{
	/// <summary>
	/// Represents base class to manage json settings.
	/// </summary>
	public abstract class BaseJsonSettings : ISettingsSharingContext, INotifyPropertyChanged
	{
		// Fields & Properties

		private ISettingsSharingContext? _settingsSharingContext;

		public static JsonSerializerOptions JsonSerializerOptions { get; } = new()
		{
			WriteIndented = true
		};

		public bool IsAvailable { get; protected set; }

		BaseJsonSettings ISettingsSharingContext.Instance
			=> this;

		private IJsonSettingsDatabaseService? _JsonSettingsDatabase;
		protected IJsonSettingsDatabaseService? JsonSettingsDatabase
		{
			get => _settingsSharingContext?.Instance?.JsonSettingsDatabase ?? _JsonSettingsDatabase;
			set => _JsonSettingsDatabase = value;
		}

		// Events

		public event EventHandler<SettingChangedEventArgs>? OnSettingChangedEvent;
		public event PropertyChangedEventHandler? PropertyChanged;

		// Methods

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
				return false;

			if (JsonSettingsDatabase?.SetValue(propertyName, value) ?? false)
			{
				RaiseOnSettingChangedEvent(this, new SettingChangedEventArgs(propertyName, value));

				OnPropertyChanged(propertyName);

				return true;
			}

			return false;
		}

		// Event Methods

		protected virtual void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			OnSettingChangedEvent?.Invoke(sender, e);
			_settingsSharingContext?.Instance.RaiseOnSettingChangedEvent(sender, e);
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
