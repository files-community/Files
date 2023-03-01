using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Commands;
using Files.App.DataModels;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class MultiSelectAction : ObservableObject, IToggleAction
	{
		public string Label { get; } = "MultiSelect";

		public RichGlyph Glyph { get; } = new("\uE762");

		public bool IsOn => App.AppModel.ShowSelectionCheckboxes;

		public MultiSelectAction()
		{
			App.AppModel.PropertyChanged += Model_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			App.AppModel.ShowSelectionCheckboxes = !App.AppModel.ShowSelectionCheckboxes;
			return Task.CompletedTask;
		}

		private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AppModel.ShowSelectionCheckboxes))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
