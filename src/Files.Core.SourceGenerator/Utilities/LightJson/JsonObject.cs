// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;

#nullable disable

namespace Files.Core.SourceGenerator.Utilities.LightJson
{
	/// <summary>
	/// Represents a key-value pair collection of JsonValue objects.
	/// </summary>
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(JsonObjectDebugView))]
	internal sealed class JsonObject : IEnumerable<KeyValuePair<string, JsonValue>>, IEnumerable<JsonValue>
	{
		private readonly IDictionary<string, JsonValue> properties;

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonObject"/> class.
		/// </summary>
		public JsonObject() => properties = new Dictionary<string, JsonValue>();

		/// <summary>
		/// Gets the number of properties in this JsonObject.
		/// </summary>
		/// <value>The number of properties in this JsonObject.</value>
		public int Count => properties.Count;

		/// <summary>
		/// Gets or sets the property with the given key.
		/// </summary>
		/// <param name="key">The key of the property to get or set.</param>
		/// <remarks>
		/// <para>The getter will return JsonValue.Null if the given key is not assosiated with any value.</para>
		/// </remarks>
		public JsonValue this[string key]
		{
			get
			{

				return properties.TryGetValue(key, out var value) ? value : JsonValue.Null;
			}

			set => properties[key] = value;
		}

		/// <summary>
		/// Adds a key with a null value to this collection.
		/// </summary>
		/// <param name="key">The key of the property to be added.</param>
		/// <remarks><para>Returns this JsonObject.</para></remarks>
		/// <returns>The <see cref="JsonObject"/> that was added.</returns>
		public JsonObject Add(string key) => Add(key, JsonValue.Null);

		/// <summary>
		/// Adds a value associated with a key to this collection.
		/// </summary>
		/// <param name="key">The key of the property to be added.</param>
		/// <param name="value">The value of the property to be added.</param>
		/// <returns>Returns this JsonObject.</returns>
		public JsonObject Add(string key, JsonValue value)
		{
			properties.Add(key, value);
			return this;
		}

		/// <summary>
		/// Removes a property with the given key.
		/// </summary>
		/// <param name="key">The key of the property to be removed.</param>
		/// <returns>
		/// Returns true if the given key is found and removed; otherwise, false.
		/// </returns>
		public bool Remove(string key) => properties.Remove(key);

		/// <summary>
		/// Clears the contents of this collection.
		/// </summary>
		/// <returns>Returns this JsonObject.</returns>
		public JsonObject Clear()
		{
			properties.Clear();
			return this;
		}

		/// <summary>
		/// Changes the key of one of the items in the collection.
		/// </summary>
		/// <remarks>
		/// <para>This method has no effects if the <i>oldKey</i> does not exists.
		/// If the <i>newKey</i> already exists, the value will be overwritten.</para>
		/// </remarks>
		/// <param name="oldKey">The name of the key to be changed.</param>
		/// <param name="newKey">The new name of the key.</param>
		/// <returns>Returns this JsonObject.</returns>
		public JsonObject Rename(string oldKey, string newKey)
		{
			if (oldKey == newKey)
			{
				// Renaming to the same name just does nothing
				return this;
			}


			if (properties.TryGetValue(oldKey, out var value))
			{
				this[newKey] = value;
				_ = Remove(oldKey);
			}

			return this;
		}

		/// <summary>
		/// Determines whether this collection contains an item assosiated with the given key.
		/// </summary>
		/// <param name="key">The key to locate in this collection.</param>
		/// <returns>Returns true if the key is found; otherwise, false.</returns>
		public bool ContainsKey(string key) => properties.ContainsKey(key);

		/// <summary>
		/// Determines whether this collection contains the given JsonValue.
		/// </summary>
		/// <param name="value">The value to locate in this collection.</param>
		/// <returns>Returns true if the value is found; otherwise, false.</returns>
		public bool Contains(JsonValue value) => properties.Values.Contains(value);

		/// <summary>
		/// Returns an enumerator that iterates through this collection.
		/// </summary>
		/// <returns>The enumerator that iterates through this collection.</returns>
		public IEnumerator<KeyValuePair<string, JsonValue>> GetEnumerator() => properties.GetEnumerator();

		/// <summary>
		/// Returns an enumerator that iterates through this collection.
		/// </summary>
		/// <returns>The enumerator that iterates through this collection.</returns>
		IEnumerator<JsonValue> IEnumerable<JsonValue>.GetEnumerator() => properties.Values.GetEnumerator();

		/// <summary>
		/// Returns an enumerator that iterates through this collection.
		/// </summary>
		/// <returns>The enumerator that iterates through this collection.</returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

		private class JsonObjectDebugView
		{
			private readonly JsonObject jsonObject;

			public JsonObjectDebugView(JsonObject jsonObject) => this.jsonObject = jsonObject;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public KeyValuePair[] Keys
			{
				get
				{
					var keys = new KeyValuePair[jsonObject.Count];

					var i = 0;
					foreach (var property in jsonObject)
					{
						keys[i] = new KeyValuePair(property.Key, property.Value);
						i += 1;
					}

					return keys;
				}
			}

			[DebuggerDisplay("{value.ToString(),nq}", Name = "{key}", Type = "JsonValue({Type})")]
			public class KeyValuePair
			{
#pragma warning disable IDE0052 // Remove unread private members
				[DebuggerBrowsable(DebuggerBrowsableState.Never)]
				private readonly string key;
#pragma warning restore IDE0052 // Remove unread private members

				[DebuggerBrowsable(DebuggerBrowsableState.Never)]
				private readonly JsonValue value;

				public KeyValuePair(string key, JsonValue value)
				{
					this.key = key;
					this.value = value;
				}

				[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
				public object View
				{
					get
					{
						if (value.IsJsonObject)
						{
							return (JsonObject)value;
						}
						else
						{
							return value.IsJsonArray ? (JsonArray)value : (object)value;
						}
					}
				}
			}
		}
	}
}
