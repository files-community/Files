// Copyright (c) Files Community
// Licensed under the MIT License.

using Axe.Windows.Automation;
using Axe.Windows.Core.Enums;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Files.InteractionTests.Helper
{
	public sealed class AxeHelper
	{
		public static IScanner AccessibilityScanner;

		internal static void InitializeAxe()
		{
			var processes = Process.GetProcessesByName("Files");
			Assert.IsTrue(processes.Length > 0);

			var config = Config.Builder.ForProcessId(processes[0].Id).Build();

			AccessibilityScanner = ScannerFactory.CreateScanner(config);
		}

		public static void AssertNoAccessibilityErrors(Func<ScanResult, bool>? ignoredIssueFilter = null)
		{
			var testResult = AccessibilityScanner
				.Scan(null)
				.WindowScanOutputs
				.SelectMany(output => output.Errors)
				.Where(error => error.Rule.ID != RuleId.BoundingRectangleNotNull);

			if (ignoredIssueFilter is not null)
				testResult = testResult.Where(error => !ignoredIssueFilter(error));

			if (testResult.Any())
			{
				StringBuilder sb = new();
				sb.AppendLine();
				sb.AppendLine("============================================================");
				sb.AppendJoin(Environment.NewLine, testResult.Select(BuildAssertMessage));
				sb.AppendLine();
				sb.AppendLine("============================================================");

				Assert.Fail(sb.ToString());
			}
		}

		public static bool IsCommunityToolkitWindowsIssue430(ScanResult result)
		{
			// CommunityToolkit/Windows#430: SettingsExpander internal controls can report
			// duplicate Name + LocalizedControlType until the toolkit fix lands.
			var description = result.Rule.Description;
			return !string.IsNullOrWhiteSpace(description)
				&& description.Contains("LocalizedControlType", StringComparison.OrdinalIgnoreCase)
				&& description.Contains("Name", StringComparison.OrdinalIgnoreCase)
				&& description.Contains("same", StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsCommunityToolkitSettingsCardButtonNameIssue(ScanResult result)
		{
			if (string.IsNullOrWhiteSpace(result.Rule.Description) ||
				!result.Rule.Description.Contains("must not include the element's control type", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			if (!result.Element.Properties.TryGetValue("ControlType", out string? controlType) ||
				!controlType.StartsWith("Button(", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			if (!result.Element.Properties.TryGetValue("Name", out string? name) ||
				string.IsNullOrWhiteSpace(name))
			{
				return false;
			}

			return name.Contains("button", StringComparison.OrdinalIgnoreCase);
		}

		private static string BuildAssertMessage(ScanResult result)
		{
			// e.g., "Element Button(50000) violated rule 'The Name property of a focusable element must not be null.'."
			return $"Element {result.Element.Properties["ControlType"]} at ({ParseBoundingRectangle(result.Element.Properties["BoundingRectangle"])}) violated rule \"{result.Rule.Description}\".";
		}

		private static string ParseBoundingRectangle(string boundingRectangle)
		{
			// e.g., "[l=1617,t=120,r=1663,b=152]" to "x=1617,y=120,w=46,h=32"
			var output = new ushort[4];
			var parts = boundingRectangle.Trim('[').Trim(']').Split(',');
			for (int index = 0; index < 4; index++)
			{
				if (ushort.TryParse(parts[index][2..], out var res))
					output[index] = res;
			}

			return $"x={output[0]},y={output[1]},w={output[2] - output[0]},h={output[3] - output[1]}";
		}
	}
}