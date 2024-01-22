// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Storage.Security;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs
{
	public sealed partial class AccessControlEditorDialog : ContentDialog
	{
		public AccessControlEntryModifiable? ModifiableModel { get; set; }

		public AccessControlEditorDialog()
		{
			InitializeComponent();
		}
	}
}
