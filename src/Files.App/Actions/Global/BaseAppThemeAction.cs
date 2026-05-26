// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace Files.App.Actions
{
	internal abstract class BaseAppThemeAction : ObservableObject
	{
		protected IAppThemeModeService AppThemeModeService { get; } = Ioc.Default.GetRequiredService<IAppThemeModeService>();

		protected BaseAppThemeAction()
		{
			AppThemeModeService.AppThemeModeChanged += AppThemeModeService_AppThemeModeChanged;
		}

		protected static ElementTheme ResolveSystemThemeFallback()
			=> Application.Current.RequestedTheme is ApplicationTheme.Dark
				? ElementTheme.Dark
				: ElementTheme.Light;

		protected ElementTheme GetEffectiveTheme()
		{
			var requestedTheme = AppThemeModeService.AppThemeMode;
			if (requestedTheme is ElementTheme.Light or ElementTheme.Dark)
				return requestedTheme;

			if (MainWindow.Instance?.Content is FrameworkElement rootElement)
			{
				var actualTheme = rootElement.ActualTheme;
				if (actualTheme is ElementTheme.Light or ElementTheme.Dark)
					return actualTheme;
			}

			return ResolveSystemThemeFallback();
		}

		protected void SetTheme(ElementTheme appTheme)
		{
			AppThemeModeService.AppThemeMode = appTheme;
		}

		private void AppThemeModeService_AppThemeModeChanged(object? sender, EventArgs e)
		{
			OnPropertyChanged(nameof(IAction.IsExecutable));
		}
	}
}
