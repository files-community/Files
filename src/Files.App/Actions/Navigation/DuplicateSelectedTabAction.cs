// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class DuplicateSelectedTabAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext context;

		public string Label
			=> "DuplicateTab".GetLocalizedResource();

		public string Description
			=> "DuplicateSelectedTabDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.K, KeyModifiers.CtrlShift);

		public bool IsExecutable
			=> context.SelectedTabItem is not null;

		public DuplicateSelectedTabAction()
		{
			context = Ioc.Default.GetRequiredService<IMultitaskingContext>();
			context.PropertyChanged += MultitaskingContext_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			var arguments = context.SelectedTabItem.NavigationParameter;

			if (arguments is null)
			{
				await NavigationHelpers.AddNewTabByPathAsync(typeof(PaneHolderPage), "Home", true);
			}
			else
			{
				await NavigationHelpers.AddNewTabByParamAsync(arguments.InitialPageType, arguments.NavigationParameter, context.SelectedTabIndex + 1);
			}
		}

		private void MultitaskingContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMultitaskingContext.SelectedTabItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
