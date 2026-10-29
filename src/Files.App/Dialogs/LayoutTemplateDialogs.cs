// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Dialogs
{
	/// <summary>
	/// Asks the user about layout templates.
	/// </summary>
	public static class LayoutTemplateDialogs
	{
		/// <summary>
		/// Asks for a name and returns it, or null when the user cancels.
		/// </summary>
		public static async Task<string?> AskNameAsync(string title, string primaryButtonText, string name)
		{
			var textBox = new TextBox()
			{
				Text = name,
				PlaceholderText = Strings.TemplateName.GetLocalizedResource(),
			};
			AutomationProperties.SetName(textBox, Strings.TemplateName.GetLocalizedResource());

			var dialog = new ContentDialog()
			{
				Title = title,
				Content = textBox,
				PrimaryButtonText = primaryButtonText,
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary,
				PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
				IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(name),
				Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
			};

			textBox.TextChanged += (_, _) => dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(textBox.Text);
			dialog.Opened += (_, _) =>
			{
				textBox.Focus(FocusState.Programmatic);
				textBox.SelectAll();
			};

			return await dialog.TryShowAsync() is ContentDialogResult.Primary ? textBox.Text.Trim() : null;
		}

		/// <summary>
		/// Asks the user to confirm deleting a template.
		/// </summary>
		/// <summary>
		/// Asks for file extensions and returns them, or null when the user cancels.
		/// </summary>
		/// <param name="findConflict">Explains why one of the extensions can't be used, or returns null.</param>
		public static async Task<string?> EditFileTypesAsync(string extensions, Func<IEnumerable<string>, string?> findConflict)
		{
			var extensionsBox = new TextBox()
			{
				Header = Strings.FileExtensions.GetLocalizedResource(),
				PlaceholderText = ".jpg, .png",
				Text = extensions,
			};

			var conflictText = new TextBlock()
			{
				Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"],
				TextWrapping = TextWrapping.WrapWholeWords,
				Visibility = Visibility.Collapsed,
			};
			AutomationProperties.SetLiveSetting(conflictText, AutomationLiveSetting.Assertive);

			var dialog = new ContentDialog()
			{
				Title = Strings.FileTypes.GetLocalizedResource(),
				Content = new StackPanel()
				{
					Spacing = 8,
					Children = { extensionsBox, conflictText },
				},
				PrimaryButtonText = Strings.Save.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary,
				PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
				Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
			};

			void Validate()
			{
				var newExtensions = new LayoutRule() { Extensions = extensionsBox.Text }.GetExtensions().ToArray();
				var conflict = findConflict(newExtensions);
				conflictText.Text = conflict ?? string.Empty;
				conflictText.Visibility = conflict is null ? Visibility.Collapsed : Visibility.Visible;
				if (conflict is not null)
					FrameworkElementAutomationPeer.CreatePeerForElement(conflictText)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
				dialog.IsPrimaryButtonEnabled = newExtensions.Length > 0 && conflict is null;
			}

			Validate();
			extensionsBox.TextChanged += (_, _) => Validate();
			dialog.Opened += (_, _) => extensionsBox.Focus(FocusState.Programmatic);

			return await dialog.TryShowAsync() is ContentDialogResult.Primary ? extensionsBox.Text.Trim() : null;
		}

		public static async Task<bool> ConfirmDeleteAsync(string name)
		{
			var dialog = new ContentDialog()
			{
				Title = Strings.DeleteTemplateConfirmationTitle.GetLocalizedResource(),
				Content = string.Format(Strings.DeleteTemplateConfirmation.GetLocalizedResource(), name, Ioc.Default.GetRequiredService<ILayoutTemplatesService>().DefaultTemplate.DisplayName),
				PrimaryButtonText = Strings.Delete.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Close,
				Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
			};

			return await dialog.TryShowAsync() is ContentDialogResult.Primary;
		}
	}
}
