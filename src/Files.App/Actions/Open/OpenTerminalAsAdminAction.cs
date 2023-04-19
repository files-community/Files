using Files.App.Commands;
using System.Diagnostics;

namespace Files.App.Actions
{
	internal class OpenTerminalAsAdminAction : OpenTerminalAction
	{
		public override string Label { get; } = "OpenTerminalAsAdmin".GetLocalizedResource();

		public override string Description => "OpenTerminalAsAdminDescription".GetLocalizedResource();

		public override HotKey HotKey { get; } = new(Keys.Oem3, KeyModifiers.CtrlShift);

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
