// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	/// <summary>
	/// Represents action to run executable item as administrator.
	/// </summary>
	internal sealed class RunAsAdminAction : BaseRunAsAction
	{
		public override string Label
			=> "RunAsAdministrator".GetLocalizedResource();

		public override string Description
			=> "RunAsAdminDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uE7EF");

		public RunAsAdminAction() : base("runas")
		{
		}
	}
}
