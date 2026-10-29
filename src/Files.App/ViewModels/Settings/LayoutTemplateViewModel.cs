// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Windows.Input;

namespace Files.App.ViewModels.Settings
{
	/// <summary>
	/// Represents a layout template listed in Settings.
	/// </summary>
	public sealed partial class LayoutTemplateViewModel : ObservableObject
	{
		private readonly ILayoutTemplatesService _templatesService;

		public string Id { get; }

		public string DisplayName
			=> (_templatesService.GetTemplate(Id) ?? _templatesService.DefaultTemplate).DisplayName;

		public string EditName
			=> string.Format(Strings.EditNamed.GetLocalizedResource(), DisplayName);

		public string DeleteName
			=> string.Format(Strings.DeleteNamed.GetLocalizedResource(), DisplayName);

		public bool CanDelete
			=> Id != _templatesService.DefaultTemplate.Id;

		public string? DefaultLabel
			=> CanDelete ? null : Strings.Default.GetLocalizedResource();

		public ICommand EditCommand { get; }

		public ICommand DeleteCommand { get; }

		public LayoutTemplateViewModel(ILayoutTemplatesService templatesService, string id, LayoutViewModel owner)
		{
			_templatesService = templatesService;
			Id = id;

			EditCommand = new AsyncRelayCommand(() => owner.EditTemplateAsync(this));
			DeleteCommand = new AsyncRelayCommand(() => owner.DeleteTemplateAsync(this));
		}

		/// <summary>
		/// Raises change notifications after the template changed.
		/// </summary>
		public void Refresh()
		{
			OnPropertyChanged(string.Empty);
		}
	}
}
