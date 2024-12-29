// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class OpenTerminalAsAdminAction : OpenTerminalAction
	{
		public override string Label
			=> "OpenTerminalAsAdmin".GetLocalizedResource();

		public override string Description
			=> "OpenTerminalAsAdminDescription".GetLocalizedResource();

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
