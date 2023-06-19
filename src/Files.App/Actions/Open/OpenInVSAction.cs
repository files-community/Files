// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Contexts;
using Files.App.Shell;

namespace Files.App.Actions
{
	internal sealed class OpenInVSAction : ObservableObject, IAction
	{
		private readonly IContentPageContext _context;

		private readonly bool _isVSInstalled;

		public string Label { get; } = "OpenInVS".GetLocalizedResource();

		public string Description { get; } = "OpenInVSDescription".GetLocalizedResource();

		public bool IsExecutable => 
			_isVSInstalled &&
			!string.IsNullOrWhiteSpace(_context.SolutionFilePath);

		public OpenInVSAction()
		{
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();

			_isVSInstalled = SoftwareHelpers.IsVSInstalled();
			if (_isVSInstalled )
				_context.PropertyChanged += Context_PropertyChanged;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IContentPageContext.SolutionFilePath))
				OnPropertyChanged(nameof(IsExecutable));
		}

		public Task ExecuteAsync()
		{
			Win32API.RunPowershellCommand($"start {_context.SolutionFilePath}", false);

			return Task.CompletedTask;
		}
	}
}
