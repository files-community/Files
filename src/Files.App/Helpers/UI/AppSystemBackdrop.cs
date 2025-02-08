using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics.CodeAnalysis;
using Windows.UI;

namespace Files.App.Helpers
{
	internal sealed partial class AppSystemBackdrop : SystemBackdrop
	{
		private bool isSecondaryWindow;
		private IUserSettingsService userSettingsService;
		private ISystemBackdropControllerWithTargets? controller;
		private ICompositionSupportsSystemBackdrop target;
		private XamlRoot root;
		private SystemBackdropTheme? prevTheme = null;

		public AppSystemBackdrop(bool isSecondaryWindow = false)
		{
			this.isSecondaryWindow = isSecondaryWindow;
			userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			userSettingsService.OnSettingChangedEvent += OnSettingChanged;
		}

		[MemberNotNull(nameof(target), nameof(root))]
		protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
		{
			if (target is not null)
				throw new InvalidOperationException("AppSystemBackdrop cannot be used with more than one target");

			base.OnTargetConnected(connectedTarget, xamlRoot);
			this.target = connectedTarget;
			this.root = xamlRoot;
			var configuration = GetDefaultSystemBackdropConfiguration(connectedTarget, xamlRoot);
			controller = GetSystemBackdropController(userSettingsService.AppearanceSettingsService.AppThemeBackdropMaterial, configuration.Theme);
			controller?.SetSystemBackdropConfiguration(configuration);
			controller?.AddSystemBackdropTarget(connectedTarget);
		}

		protected override void OnDefaultSystemBackdropConfigurationChanged(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
		{
			base.OnDefaultSystemBackdropConfigurationChanged(target, xamlRoot);
			var configuration = GetDefaultSystemBackdropConfiguration(target, xamlRoot);
			if (controller is not DesktopAcrylicController acrylicController || acrylicController.Kind != DesktopAcrylicKind.Thin || configuration.Theme == prevTheme)
				return;

			prevTheme = configuration.Theme;
			SetThinAcrylicBackdropProperties(acrylicController, configuration.Theme);
		}

		protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
		{
			base.OnTargetDisconnected(disconnectedTarget);
			this.target = null!;
			this.root = null!;


			try
			{
				controller?.RemoveSystemBackdropTarget(disconnectedTarget);
			}
			catch (ObjectDisposedException)
			{
				// Ignore errors when the controller is already disposed
			}

			controller?.Dispose();
			userSettingsService.OnSettingChangedEvent -= OnSettingChanged;
		}

		private void OnSettingChanged(object? sender, SettingChangedEventArgs e)
		{
			if (target is null)
				return;

			switch (e.SettingName)
			{
				case nameof(IAppearanceSettingsService.AppThemeBackdropMaterial):
					controller?.RemoveAllSystemBackdropTargets();
					controller?.Dispose();
					var configuration = GetDefaultSystemBackdropConfiguration(target, root);
					var newController = GetSystemBackdropController((BackdropMaterialType)e.NewValue!, configuration.Theme);
					newController?.SetSystemBackdropConfiguration(configuration);
					newController?.AddSystemBackdropTarget(target);
					controller = newController;
					break;
			}
		}

		private ISystemBackdropControllerWithTargets? GetSystemBackdropController(BackdropMaterialType backdropType, SystemBackdropTheme theme)
		{
			if (isSecondaryWindow && backdropType == BackdropMaterialType.MicaAlt)
				backdropType = BackdropMaterialType.Mica;

			switch (backdropType)
			{
				case BackdropMaterialType.MicaAlt:
					return new MicaController()
					{
						Kind = MicaKind.BaseAlt
					};

				case BackdropMaterialType.Mica:
					return new MicaController()
					{
						Kind = MicaKind.Base
					};

				case BackdropMaterialType.Acrylic:
					return new DesktopAcrylicController()
					{
						Kind = DesktopAcrylicKind.Base,
					};

				case BackdropMaterialType.ThinAcrylic:
					var acrylicController = new DesktopAcrylicController()
					{
						Kind = DesktopAcrylicKind.Thin
					};
					SetThinAcrylicBackdropProperties(acrylicController, theme);
					return acrylicController;

				default:
					return null;
			}
		}

		private void SetThinAcrylicBackdropProperties(DesktopAcrylicController controller, SystemBackdropTheme theme)
		{
			// This sets all properties to work around other properties not updating when fallback color is changed
			// This uses the Thin Acrylic recipe from the WinUI Figma toolkit

			switch(theme)
			{
				case SystemBackdropTheme.Light:
					controller.TintColor = Color.FromArgb(0xff, 0xd3, 0xd3, 0xd3);
					controller.TintOpacity = 0f;
					controller.LuminosityOpacity = 0.44f;
					controller.FallbackColor = Color.FromArgb(0x99, 0xd3, 0xd3, 0xd3);
					break;

				case SystemBackdropTheme.Dark:
					controller.TintColor = Color.FromArgb(0xff, 0x54, 0x54, 0x54);
					controller.TintOpacity = 0f;
					controller.LuminosityOpacity = 0.64f;
					controller.FallbackColor = Color.FromArgb(0xff, 0x20, 0x20, 0x20);
					break;
			}
		}
	}
}
