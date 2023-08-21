// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal sealed class RunAsAnotherUserAction : BaseRunAsAction
	{
		public override string Label
			=> "BaseLayoutContextFlyoutRunAsAnotherUser/Text".GetLocalizedResource();

		public override string Description
			=> "RunAsAnotherUserDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uE7EE");

		public RunAsAnotherUserAction() : base("runasuser")
		{
		}
	}
}
