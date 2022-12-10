using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Commands;
using Files.App.DataModels;
using Files.App.Extensions;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class MultiSelectAction : ObservableObject, IAction
	{
		public CommandCodes Code => CommandCodes.MultiSelect;
		public string Label => "NavToolbarMultiselect/Text".GetLocalizedResource();

		public IGlyph Glyph { get; } = new Glyph("\uE762");

		public bool IsOn
		{
			get => App.AppModel.MultiselectEnabled;
			set => App.AppModel.MultiselectEnabled = value;
		}

		public MultiSelectAction() => App.AppModel.PropertyChanged += Model_PropertyChanged;

		public Task ExecuteAsync()
		{
			App.AppModel.MultiselectEnabled = !App.AppModel.MultiselectEnabled;
			return Task.CompletedTask;
		}

		private void Model_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AppModel.MultiselectEnabled))
				OnPropertyChanged(nameof(IsOn));

		}
	}
}
