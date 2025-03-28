// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	/// <summary>
	/// An interface that provides a way to get the text member path of <see cref="OmnibarMode.SuggestionItemsSource"/>.
	/// </summary>
	/// <remarks>
	/// An alternative to this interface is to use an <see cref="Microsoft.UI.Xaml.Data.IBindableCustomPropertyImplementation"/> powered by CsWinRT.
	/// </remarks>
	public interface IOmnibarTextMemberPathProvider
	{
		/// <summary>
		/// Retrieves the path of the text member as a string. This path can be used to identify the location of the text member.
		/// </summary>
		/// <returns>Returns a string representing the path of the text member.</returns>
		string GetTextMemberPath(string textMemberPath);
	}
}
