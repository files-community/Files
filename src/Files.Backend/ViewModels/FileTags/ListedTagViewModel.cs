﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace Files.Backend.ViewModels.FileTags
{
	public class ListedTagViewModel : ObservableObject
	{
		private TagViewModel tag;
		public TagViewModel Tag
		{
			get => tag;
			set => SetProperty(ref tag, value);
		}

		private bool isEditing;
		public bool IsEditing
		{
			get => isEditing;
			set => SetProperty(ref isEditing, value);
		}

		private bool isNameValid = true;
		public bool IsNameValid
		{
			get => isNameValid;
			set => SetProperty(ref isNameValid, value);
		}

		private bool canCommit = false;
		public bool CanCommit
		{
			get => canCommit;
			set => SetProperty(ref canCommit, value);
		}

		private string newName;
		public string NewName
		{
			get => newName;
			set => SetProperty(ref newName, value);
		}

		private string newColor;
		public string NewColor
		{
			get => newColor;
			set => SetProperty(ref newColor, value);
		}

		public ListedTagViewModel(TagViewModel tag)
		{
			Tag = tag;
			NewColor = tag.Color;
		}
	}
}
