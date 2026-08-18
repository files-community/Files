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

		[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = AotJson.SerializerTrimJustification)]
		[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050", Justification = AotJson.SerializerTrimJustification)]
		public string Serialize()
		{
			return JsonSerializer.Serialize(this, _typesConverter.Options);
		}

		[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = AotJson.SerializerTrimJustification)]
		[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050", Justification = AotJson.SerializerTrimJustification)]
		[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Falls back to ShellPanesPage when the stored page type is unavailable")]
		public static TabBarItemParameter Deserialize(string obj)
		{
			var tempArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(obj, AotJson.Options)
				?? throw new JsonException("The tab data is empty.");
			var typeName = tempArgs[nameof(InitialPageType)].GetString();
			if (string.IsNullOrEmpty(typeName))
				throw new JsonException("The initial page type is missing or invalid.");

			// Restore navigation data from tabs whose original page type no longer exists.
			var initialPageType = Type.GetType(typeName) ?? typeof(Files.App.Views.ShellPanesPage);

			object? navigationParameter;
			try
			{
				navigationParameter = JsonSerializer.Deserialize<PaneNavigationArguments>(tempArgs[nameof(NavigationParameter)].GetRawText(), AotJson.Options);
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
