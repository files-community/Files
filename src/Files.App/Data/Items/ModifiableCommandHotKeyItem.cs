// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents item for a hotkey that is registered for a <see cref="IRichCommand"/>.
	/// </summary>
	public class ModifiableCommandHotKeyItem : ObservableObject
	{
		public CommandCodes CommandCode { get; set; }

		public string Label { get; set; } = string.Empty;

		public string Description { get; set; } = string.Empty;

		public HotKeyCollection DefaultHotKeyCollection { get; set; } = HotKeyCollection.Empty;

		public HotKey PreviousHotKey { get; set; } = HotKey.None;

		public HotKeyCollection HotKeys { get; set; }

		private string _HotKeyText = string.Empty;
		public string HotKeyText
		{
			get => _HotKeyText;
			set => SetProperty(ref _HotKeyText, value);
		}

		private bool _IsEditMode;
		public bool IsEditMode
		{
			get => _IsEditMode;
			set => SetProperty(ref _IsEditMode, value);
		}

		private bool _IsCustomized;
		public bool IsDefaultKey
		{
			get => _IsCustomized;
			set => SetProperty(ref _IsCustomized, value);
		}

		private HotKey _HotKey;
		public HotKey HotKey
		{
			get => _HotKey;
			set
			{
				if (SetProperty(ref _HotKey, value))
				{
					HotKeyText = HotKey.LocalizedLabel;
					HotKeys = new(value);
					OnPropertyChanged(nameof(HotKeys));
				}
			}
		}
	}
}
