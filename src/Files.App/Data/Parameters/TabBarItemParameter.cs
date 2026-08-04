// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Parameters
{
	public sealed class TabBarItemParameter
	{
		private static readonly KnownTypesConverter _typesConverter = new();
		private Type? _initialPageType;

		public Type InitialPageType
		{
			get => _initialPageType ?? throw new InvalidOperationException("The initial page type has not been set.");
			set => _initialPageType = value;
		}

		public object? NavigationParameter { get; set; }

		public string Serialize()
		{
			return JsonSerializer.Serialize(this, _typesConverter.Options);
		}

		public static TabBarItemParameter Deserialize(string obj)
		{
			var tempArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(obj)
				?? throw new JsonException("The tab data is empty.");
			var typeName = tempArgs[nameof(InitialPageType)].GetString();
			if (string.IsNullOrEmpty(typeName))
				throw new JsonException("The initial page type is missing or invalid.");

			// Restore navigation data from tabs whose original page type no longer exists.
			var initialPageType = Type.GetType(typeName) ?? typeof(Files.App.Views.ShellPanesPage);

			object? navigationParameter;
			try
			{
				navigationParameter = JsonSerializer.Deserialize<PaneNavigationArguments>(tempArgs[nameof(NavigationParameter)].GetRawText());
			}
			catch (JsonException)
			{
				navigationParameter = tempArgs[nameof(NavigationParameter)].GetString();
			}

			return new TabBarItemParameter
			{
				InitialPageType = initialPageType,
				NavigationParameter = navigationParameter,
			};
		}
	}
}
