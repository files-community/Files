// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils
{
	public sealed class EnumToLocalizedConverter<T> where T : Enum
	{
		public string Name
		{
			get
			{
				var localized = $"{typeof(T).Name}_{Enum.GetName(typeof(T), Value)}".GetLocalizedResource();

				if (string.IsNullOrEmpty(localized))
				{
					localized = $"{Enum.GetName(typeof(T), Value)}".GetLocalizedResource();
				}

				return localized;
			}
		}

		public T Value { get; set; }

		public EnumToLocalizedConverter(T value)
		{
			Value = value;
		}
	}
}
