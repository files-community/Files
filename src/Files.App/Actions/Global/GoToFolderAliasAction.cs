// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Windows.Win32;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed class GoToFolderAliasAction : IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> Strings.GoToFolderAlias.GetLocalizedResource();

		public string Description
			=> Strings.GoToFolderAliasDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Navigation;

		public HotKey HotKey
			=> GetAtHotKey() is { Modifier: not KeyModifiers.CtrlAlt } hotKey ? hotKey : HotKey.None;

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Shortcut");

		public Task ExecuteAsync(object? parameter = null)
		{
			return context.ShellPage?.ToolbarViewModel.SwitchToPathModeWithInputAsync(FolderAliasHelpers.Prefix)
				?? Task.CompletedTask;
		}

		/// <summary>
		/// Gets whether the hot key types "@" on the current keyboard layout.
		/// </summary>
		public static bool IsAtHotKey(HotKey hotKey)
			=> GetAtHotKey() is { IsNone: false } at && at.Key == hotKey.Key && at.Modifier == hotKey.Modifier;

		private static HotKey GetAtHotKey()
		{
			// The low byte is the virtual key and the high byte the modifiers it needs
			var scan = PInvoke.VkKeyScan('@');
			if (scan is -1)
				return HotKey.None;

			var modifier = (scan >> 8) switch
			{
				0 => KeyModifiers.None,
				1 => KeyModifiers.Shift,
				6 => KeyModifiers.CtrlAlt,
				_ => (KeyModifiers?)null,
			};

			return modifier is { } m
				? new((Keys)(scan & 0xFF), m)
				: HotKey.None;
		}
	}
}
