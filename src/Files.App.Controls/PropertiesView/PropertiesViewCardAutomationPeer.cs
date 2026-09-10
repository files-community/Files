// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Files.App.Controls;

public sealed partial class PropertiesViewCardAutomationPeer : ButtonAutomationPeer
{
	public PropertiesViewCardAutomationPeer(PropertiesViewCard owner)
		: base(owner)
	{
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return Owner is PropertiesViewCard { IsClickEnabled: true }
			? AutomationControlType.Button
			: AutomationControlType.Group;
	}

	protected override string GetClassNameCore()
	{
		return Owner.GetType().Name;
	}

	protected override string GetNameCore()
	{
		if (Owner is PropertiesViewCard { IsClickEnabled: true } card)
		{
			string name = AutomationProperties.GetName(card);
			if (!string.IsNullOrEmpty(name))
				return name;

			if (card.Header is string { Length: > 0 } header)
				return header;
		}

		return base.GetNameCore();
	}

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface is PatternInterface.Invoke &&
			Owner is PropertiesViewCard { IsClickEnabled: false })
		{
			return null;
		}

		return base.GetPatternCore(patternInterface);
	}
}
