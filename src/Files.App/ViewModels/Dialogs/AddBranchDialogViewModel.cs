namespace Files.App.ViewModels.Dialogs
{
	public class AddBranchDialogViewModel : ObservableObject
	{
		private readonly string _repositoryPath;

		private string _NewBranchName = string.Empty;
		public string NewBranchName
		{
			get => _NewBranchName;
			set
			{
				if (!SetProperty(ref _NewBranchName, value))
					return;
				IsBranchValid = !string.IsNullOrWhiteSpace(value) && GitHelpers.ValidateBranchNameForRepository(value, _repositoryPath);

				OnPropertyChanged(nameof(ShowWarningTip));
			}
		}

		private bool _IsBranchValid;
		public bool IsBranchValid
		{
			get => _IsBranchValid;
			set
			{
				if (SetProperty(ref _IsBranchValid, value))
					OnPropertyChanged(nameof(ShowWarningTip));
			}
		}

		private string _BasedOn = null!;
		public string BasedOn
		{
			get => _BasedOn;
			set => SetProperty(ref _BasedOn, value);
		}

		private bool _Checkout = true;
		public bool Checkout
		{
			get => _Checkout;
			set => SetProperty(ref _Checkout, value);
		}

		public bool ShowWarningTip => !string.IsNullOrEmpty(_NewBranchName) && !_IsBranchValid;

		public string[] Branches { get; init; }

		public AddBranchDialogViewModel(string repositoryPath, string activeBranch)
		{
			_repositoryPath = repositoryPath;
			Branches = GitHelpers.GetBranchesNames(repositoryPath);
			BasedOn = activeBranch;
		}
	}
}
