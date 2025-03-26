// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class OpenTerminalAsAdminAction : OpenTerminalAction
	{
		public override string Label
			=> Strings.OpenTerminalAsAdmin.GetLocalizedResource();

		public override string Description
			=> Strings.OpenTerminalAsAdminDescription.GetLocalizedResource();

		public override HotKey HotKey
			=> new(Keys.Oem3, KeyModifiers.CtrlShift);

		protected override ProcessStartInfo? GetProcessStartInfo()
		{
			var startInfo = base.GetProcessStartInfo();
			if (startInfo is not null)
			{
				startInfo.Verb = "runas";
				startInfo.UseShellExecute = true;
			}

			return startInfo;
		}
	}
}
