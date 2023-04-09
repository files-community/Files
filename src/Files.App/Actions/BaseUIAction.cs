using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal abstract class BaseUIAction : ObservableObject, IAction
	{
		public abstract string Label { get; }

		public abstract string Description { get; }

		public virtual bool IsExecutable => UIHelpers.CanShowDialog;

		public BaseUIAction()
		{
			UIHelpers.PropertyChanged += UIHelpers_PropertyChanged;
		}

		public abstract Task ExecuteAsync();

		private void UIHelpers_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(UIHelpers.CanShowDialog))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
