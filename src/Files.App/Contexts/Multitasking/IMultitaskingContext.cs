using Files.App.UserControls.MultitaskingControl;
using System.ComponentModel;

namespace Files.App.Contexts
{
	public interface IMultitaskingContext : INotifyPropertyChanged
	{
		IMultitaskingControl? Control { get; }

		ushort TabCount { get; }

		TabItem CurrentTabItem { get; }
		ushort CurrentTabIndex { get; }

		TabItem SelectedTabItem { get; }
		ushort SelectedTabIndex { get; }
	}
}
