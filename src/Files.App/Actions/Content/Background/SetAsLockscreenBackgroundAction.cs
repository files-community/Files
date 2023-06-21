﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.Shared.Enums;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class SetAsLockscreenBackgroundAction : BaseSetAsAction
	{
		public override string Label
			=> "SetAsLockscreen".GetLocalizedResource();

		public override string Description
			=> "SetAsLockscreenBackgroundDescription".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new("\uEE3F");

		public override bool IsExecutable => base.IsExecutable &&
			context.SelectedItem is not null;

		public override Task ExecuteAsync()
		{
			if (context.SelectedItem is not null)
				WallpaperHelpers.SetAsBackground(WallpaperType.LockScreen, context.SelectedItem.ItemPath);

			return Task.CompletedTask;
		}
	}
}
