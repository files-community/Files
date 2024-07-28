using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Controls
{
	// Template parts
	[TemplatePart( Name = ThemedIconPartName , Type = typeof( ThemedIcon ) )]
	[TemplatePart( Name = ThemedIconRootPartName , Type = typeof( Border ) )]

	public partial class MenuFlyoutItemEx : MenuFlyoutItem
	{
		internal const string ThemedIconPartName = "PART_ThemedIcon";
		internal const string ThemedIconRootPartName = "PART_ThemedIconRoot";
	}
}
