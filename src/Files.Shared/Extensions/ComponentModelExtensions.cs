// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Files.Shared.Extensions
{
	public static class ComponentModelExtensions
	{
		public static string GetDescription<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(this T enumValue) where T : Enum
		{
			var description = enumValue.ToString();
			var fieldInfo = typeof(T).GetField(enumValue.ToString());

			if (fieldInfo is not null)
			{
				if (fieldInfo.GetCustomAttribute<DescriptionAttribute>(true) is DescriptionAttribute attribute)
				{
					description = attribute.Description;
				}
			}

			return description;
		}

		public static T? GetValueFromDescription<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(string description) where T : Enum
		{
			foreach (var field in typeof(T).GetFields())
			{
				if (field.GetCustomAttribute<DescriptionAttribute>(true) is DescriptionAttribute attribute)
				{
					if (attribute.Description == description)
					{
						return (T?)field.GetValue(null);
					}
				}
				else
				{
					if (field.Name == description)
					{
						return (T?)field.GetValue(null);
					}
				}
			}

			return default;
		}
	}
}
