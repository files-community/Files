// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers
{
	[Obsolete("Do not use. This class makes maintaining the resources much harder.")]
	public sealed class LocalizedEnumHelper<T> where T : Enum
	{
		public string Name
		{
			get
			{
				var localized = $"{typeof(T).Name}_{Enum.GetName(typeof(T), Value)}".GetLocalizedResource();

				if (string.IsNullOrEmpty(localized))
					localized = $"{Enum.GetName(typeof(T), Value)}".GetLocalizedResource();

				return localized;
			}
		}

		public T Value { get; set; }

		public LocalizedEnumHelper(T value)
		{
			Value = value;
		}
	}
}
