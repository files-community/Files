// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.Storage;

namespace Files.App.ViewModels
{
	[Obsolete("Do not use this class as Settings store anymore, settings have been merged to IUserSettingsService.")]
	public class SettingsViewModel : ObservableObject
	{
		private ApplicationDataContainer localSettings
			=> ApplicationData.Current.LocalSettings;

		public SettingsViewModel()
		{
			UpdateThemeElements = new RelayCommand(() => ThemeModeChanged?.Invoke(this, EventArgs.Empty));
		}

		public event EventHandler? ThemeModeChanged;

		public ICommand UpdateThemeElements { get; }

		public bool Set<TValue>(TValue value, [CallerMemberName] string propertyName = null)
		{
			propertyName = 
				propertyName is not null && propertyName.StartsWith("set_", StringComparison.OrdinalIgnoreCase) ?
				propertyName.Substring(4) :
				propertyName;

			TValue originalValue = default;

			if (localSettings.Values.ContainsKey(propertyName))
			{
				originalValue = Get(originalValue, propertyName);

				localSettings.Values[propertyName] = value;
				if (!SetProperty(ref originalValue, value, propertyName))
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

		public TValue Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TValue>(TValue defaultValue, [CallerMemberName] string propertyName = null)
		{
			var name = propertyName ?? throw new ArgumentNullException(nameof(propertyName), "Cannot store property of unnamed.");

			name =
				name.StartsWith("get_", StringComparison.OrdinalIgnoreCase) ?
				propertyName.Substring(4) :
				propertyName;

			if (localSettings.Values.ContainsKey(name))
			{
				var value = localSettings.Values[name];

				if (value is not TValue tValue)
				{
					if (value is IConvertible)
					{
						tValue = (TValue)Convert.ChangeType(value, typeof(TValue));
					}
					else
					{
						var valueType = value.GetType();
						var tryParse = typeof(TValue).GetMethod("TryParse", BindingFlags.Instance | BindingFlags.Public);

						if (tryParse is null)
						{
							return default;
						}

						var stringValue = value.ToString();
						tValue = default;

						var tryParseDelegate =
							(TryParseDelegate<TValue>)Delegate.CreateDelegate(valueType, tryParse, false);

						tValue = (tryParseDelegate?.Invoke(stringValue, out tValue) ?? false) ? tValue : default;
					}

					// Put the corrected value in settings
					Set(tValue, propertyName);

					return tValue;
				}
				return tValue;
			}

			localSettings.Values[propertyName] = defaultValue;

			return defaultValue;
		}

		private delegate bool TryParseDelegate<TValue>(string inValue, out TValue parsedValue);
	}
}
