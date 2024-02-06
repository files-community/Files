// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Files.Core.Utils.Serialization
{
	/// <summary>
	/// A base class to easily manage all application's settings.
	/// </summary>
	public abstract class BaseJsonSettings : ISettingsSharingContext, INotifyPropertyChanged
	{
		public static JsonSerializerOptions JsonSerializerOptions { get; } = new()
		{
			WriteIndented = true
		};

		private ISettingsSharingContext? _settingsSharingContext;

		public bool IsAvailable { get; protected set; }

		private IJsonSettingsDatabase? _JsonSettingsDatabase;
		protected IJsonSettingsDatabase? JsonSettingsDatabase
		{
			get => _settingsSharingContext?.Instance?.JsonSettingsDatabase ?? _JsonSettingsDatabase;
			set => _JsonSettingsDatabase = value;
		}

		BaseJsonSettings ISettingsSharingContext.Instance => this;

		public event EventHandler<SettingChangedEventArgs>? OnSettingChangedEvent;

		public event PropertyChangedEventHandler? PropertyChanged;

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
			IsAvailable = JsonSettingsDatabase?.CreateFile(filePath) ?? false;
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
