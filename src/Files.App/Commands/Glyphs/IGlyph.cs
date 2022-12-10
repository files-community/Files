using Microsoft.UI.Xaml.Media;

namespace Files.App.Commands
{
	public interface IGlyph
	{
		string Base { get; }
		string Overlay { get; }
		string Family { get; }

		FontFamily FontFamily { get; }
	}
}
