using System.ComponentModel;

namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents special permissions of an ACE for UI
	/// </summary>
	public class SpecialPermission : GrantedPermission
	{
		public SpecialPermission(FileSystemAccessRuleForUI fileSystemAccessRule)
			: base(fileSystemAccessRule)
		{
			IsEditable = false;
		}

		private bool isGranted;
		public override bool IsGranted
		{
			get => fsar.GrantsSpecial;
			set => SetProperty(ref isGranted, value);
		}

		protected override void Fsar_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "FileSystemRights")
				OnPropertyChanged(nameof(IsGranted));
		}
	}
}
