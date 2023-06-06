using Files.Backend.Models;
using Files.Shared.Utils;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Backend.AppModels
{
	/// <inheritdoc cref="IPersistable"/>
	public abstract class SettingsModel : IPersistable, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets the <see cref="IDatabaseModel{T}"/> where settings are stored.
		/// </summary>
		protected abstract IDatabaseModel<string> SettingsDatabase { get; }

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <inheritdoc/>
		public virtual Task<bool> LoadAsync(CancellationToken cancellationToken = default)
		{
			return SettingsDatabase.LoadAsync(cancellationToken);
		}

		/// <inheritdoc/>
		public virtual Task<bool> SaveAsync(CancellationToken cancellationToken = default)
		{
			return SettingsDatabase.SaveAsync(cancellationToken);
		}

		/// <summary>
		/// Gets a value of a setting defined by <paramref name="settingName"/>.
		/// </summary>
		/// <typeparam name="T">The type of value.</typeparam>
		/// <param name="defaultValue">Retrieves the default value. If <paramref name="defaultValue"/> is null, returns the default value of <typeparamref name="T"/>.</param>
		/// <param name="settingName">The name of the setting.</param>
		/// <returns>A requested setting. The value is determined by the availability of the setting in the storage or by the <paramref name="defaultValue"/>.</returns>
		[return: NotNullIfNotNull(nameof(defaultValue))]
		protected virtual T? GetSetting<T>(Func<T>? defaultValue = null, [CallerMemberName] string settingName = "")
		{
			if (string.IsNullOrEmpty(settingName))
				return defaultValue is not null ? defaultValue() : default;

			return SettingsDatabase.GetValue(settingName, defaultValue);
		}

		/// <summary>
		/// Sets a setting value defined by <paramref name="settingName"/>.
		/// </summary>
		/// <typeparam name="T">The type of value.</typeparam>
		/// <param name="value">The value to be stored.</param>
		/// <param name="settingName">The name of the setting.</param>
		/// <returns>If the setting has been updated, returns true otherwise false.</returns>
		protected virtual bool SetSetting<T>(T? value, [CallerMemberName] string settingName = "")
		{
			if (string.IsNullOrEmpty(settingName))
				return false;

			return SettingsDatabase.SetValue(settingName, value);
		}

		/// <summary>
		/// Invokes <see cref="PropertyChanged"/> event notifying that a value of a specific setting has changed.
		/// </summary>
		/// <param name="propertyName">The name of the property whose value has changed.</param>
		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new(propertyName));
		}
	}
}
