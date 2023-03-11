using Files.App.Commands;
using Files.App.Extensions;
using System.Diagnostics;
using Windows.System;

namespace Files.App.Actions
{
	internal class OpenTerminalAsAdminAction : OpenTerminalAction
	{
		public override string Label { get; } = "OpenTerminalAsAdmin".GetLocalizedResource();

		public override HotKey HotKey { get; } = new((VirtualKey)192, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		protected override ProcessStartInfo[] GetProcessesStartInfo()
		{
			var startInfo = base.GetProcessesStartInfo();
			for (int i = 0; i < startInfo.Length; i++)
			{
				startInfo[i].Verb = "runas";
				startInfo[i].UseShellExecute = true;
			}

			return startInfo;
		}
	}
}
