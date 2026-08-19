// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Text.Json.Nodes;

namespace Files.App.Data.Parameters
{
	public sealed class TabBarItemParameter
	{
		private Type? _initialPageType;

		public Type InitialPageType
		{
			get => _initialPageType ?? throw new InvalidOperationException("The initial page type has not been set.");
			set => _initialPageType = value;
		}

		public object? NavigationParameter { get; set; }

		public string Serialize()
		{
			JsonNode? navigationParameter = NavigationParameter switch
			{
				PaneNavigationArguments paneArguments => JsonSerializer.SerializeToNode(paneArguments, AppJsonSerializerContext.Default.PaneNavigationArguments),
				string path => path,
				null => null,
				_ => throw new JsonException($"Unsupported tab navigation parameter type: {NavigationParameter.GetType()}.")
			};

			return new JsonObject
			{
				[nameof(InitialPageType)] = InitialPageType.FullName ?? throw new JsonException("The initial page type does not have a full name."),
				[nameof(NavigationParameter)] = navigationParameter,
			}.ToJsonString();
		}

		public static TabBarItemParameter Deserialize(string obj)
		{
			var data = JsonNode.Parse(obj)?.AsObject()
				?? throw new JsonException("The tab data is empty.");
			var typeName = data[nameof(InitialPageType)]?.GetValue<string>();
			if (string.IsNullOrEmpty(typeName))
				throw new JsonException("The initial page type is missing or invalid.");

			// Restore navigation data from tabs whose original page type no longer exists.
			var initialPageType = typeName switch
			{
				var name when name == typeof(ShellPanesPage).FullName => typeof(ShellPanesPage),
				var name when name == typeof(ModernShellPage).FullName => typeof(ModernShellPage),
				var name when name == typeof(ColumnShellPage).FullName => typeof(ColumnShellPage),
				_ => typeof(ShellPanesPage),
			};

			object? navigationParameter;
			try
			{
				navigationParameter = data[nameof(NavigationParameter)]?.Deserialize(AppJsonSerializerContext.Default.PaneNavigationArguments);
			}
			catch (JsonException)
			{
				navigationParameter = data[nameof(NavigationParameter)]?.GetValue<string>();
			}

			return new TabBarItemParameter
			{
				InitialPageType = initialPageType,
				NavigationParameter = navigationParameter,
			};
		}
	}
}
