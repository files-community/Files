// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Shell;

namespace Files.App.Actions
{
	/// <summary>
	/// Represents action to open the Visual Studio.
	/// </summary>
	internal sealed class OpenInVSAction : ObservableObject, IAction
	{
		private readonly IContentPageContext _context;

		private readonly bool _isVSInstalled;

		public string Label
			=> "OpenInVS".GetLocalizedResource();

		public string Description
			=> "OpenInVSDescription".GetLocalizedResource();

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

		public Task ExecuteAsync()
		{
			return Win32API.RunPowershellCommandAsync($"start \'{_context.SolutionFilePath}\'", false);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IContentPageContext.SolutionFilePath))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
