using Files.Shared.Utils;
using System;

namespace Files.Backend.Models
{
	/// <summary>
	/// Represents a database to store data identified by <typeparamref name="TKey"/>.
	/// </summary>
	/// <typeparam name="TKey">The key to identify data with.</typeparam>
	public interface IDatabaseModel<in TKey> : IPersistable
	{
		/// <summary>
		/// Gets a value from the database.
		/// </summary>
		/// <typeparam name="TValue">The type of value.</typeparam>
		/// <param name="key">The name of the value to get.</param>
		/// <param name="defaultValue">Retrieves the default value. If <paramref name="defaultValue"/> is null, returns the default value of <typeparamref name="TValue"/>.</param>
		/// <returns>A value from the database. The value is determined by the availability in the storage or by the <paramref name="defaultValue"/>.</returns>
		TValue? GetValue<TValue>(TKey key, Func<TValue>? defaultValue = null);

		/// <summary>
		/// Sets a value in the database.
		/// </summary>
		/// <typeparam name="TValue">The type of value.</typeparam>
		/// <param name="key">The name of the value to set.</param>
		/// <param name="value">The value to be stored.</param>
		/// <returns>If the value has been updated in the database, returns true otherwise false.</returns>
		bool SetValue<TValue>(TKey key, TValue? value);
	}
}
