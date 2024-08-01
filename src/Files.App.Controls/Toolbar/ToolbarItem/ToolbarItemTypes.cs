using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Controls
{
	public enum ToolbarItemTypes
	{		
		Button,
		Content,				// Default type
		FlyoutButton,
		RadioButton,			// Possibly add support for Radio Buttons using the GroupName
		Separator,
		SplitButton,
		ToggleButton,
	}
}
