using Microsoft.UI.Xaml;
using System.Globalization;

namespace Files.App.Data.Contracts
{
	public interface IRealTimeLayoutService
	{
		void AddCallback(object target, Action callback);

		void UpdateContent(FrameworkElement frameworkElement);

		void UpdateContent(Window window);

		void UpdateCulture(CultureInfo culture);

		bool UpdateTitleBar(Window window);
	}
}
