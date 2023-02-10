using CommunityToolkit.Mvvm.ComponentModel;
using Files.Sdk.Storage.LocatableStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.UserControls.Widgets
{
	public abstract class WidgetCardItem : ObservableObject
	{
		public string Path;

		public virtual object Item { get; set; }
	}
}
