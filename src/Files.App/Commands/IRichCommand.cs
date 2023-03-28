using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Files.App.Commands
{
	public interface IRichCommand : ICommand, INotifyPropertyChanging, INotifyPropertyChanged
	{
		CommandCodes Code { get; }

		string Label { get; }
		string LabelWithHotKey { get; }
		string AutomationName { get; }

		string Description { get; }

		IconSource Glyph { get; }
		Style? OpacityStyle { get; }

		string? HotKeyText { get; }
		HotKey HotKey { get; }
		HotKey SecondHotKey { get; }
		HotKey ThirdHotKey { get; }
		HotKey MediaHotKey { get; }

		bool IsToggle { get; }
		bool IsOn { get; set; }
		bool CanExecute(object? obj);

		void Execute(object? obj);
		void ExecuteTapped(object sender, TappedRoutedEventArgs e);
	}
}
