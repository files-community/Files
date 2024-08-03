using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Controls
{
	// TemplateParts
	[TemplatePart( Name = ToolbarButtonStackPanelPartName	, Type = typeof( StackPanel ) )]
	[TemplatePart( Name = ToolbarItemsRepeaterPartName		, Type = typeof( ItemsRepeater ) )]



	public partial class Toolbar : Control
	{
		// TemplatePart Names
		internal const string ToolbarButtonStackPanelPartName	= "PART_StackPanel";
		internal const string ToolbarItemsRepeaterPartName		= "PART_ItemsRepeater";



		// Resource Names
		internal const string SmallMinWidthResourceKey		= "ToolbarButtonSmallMinWidth";
		internal const string SmallMinHeightResourceKey		= "ToolbarButtonSmallMinHeight";

		internal const string MediumMinWidthResourceKey		= "ToolbarButtonMediumMinWidth";
		internal const string MediumMinHeightResourceKey	= "ToolbarButtonMediumMinHeight";

		internal const string LargeMinWidthResourceKey		= "ToolbarButtonLargeMinWidth";
		internal const string LargeMinHeightResourceKey		= "ToolbarButtonLargeMinHeight";
	}
}
