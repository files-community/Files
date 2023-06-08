using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Helpers
{
	internal sealed class AppSystemBackdrop : SystemBackdrop
	{
		private bool isSecondaryWindow;
		private IUserSettingsService userSettingsService;
		private ISystemBackdropControllerWithTargets? controller;
		private ICompositionSupportsSystemBackdrop target;
		private XamlRoot root;

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
			controller = GetSystemBackdropController(userSettingsService.AppearanceSettingsService.AppThemeBackdropMaterial);
			controller?.SetSystemBackdropConfiguration(GetDefaultSystemBackdropConfiguration(connectedTarget, xamlRoot));
			controller?.AddSystemBackdropTarget(connectedTarget);
		}

		protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
		{
			base.OnTargetDisconnected(disconnectedTarget);
			this.target = null!;
			this.root = null!;
			controller?.RemoveSystemBackdropTarget(disconnectedTarget);
			controller?.Dispose();
			userSettingsService.OnSettingChangedEvent -= OnSettingChanged;
		}

		private void OnSettingChanged(object? sender, Shared.EventArguments.SettingChangedEventArgs e)
		{
			if (target is null)
				return;

			switch (e.SettingName)
			{
				case nameof(IAppearanceSettingsService.AppThemeBackdropMaterial):
					controller?.RemoveAllSystemBackdropTargets();
					controller?.Dispose();
					var newController = GetSystemBackdropController((BackdropMaterialType)e.NewValue!);
					newController?.SetSystemBackdropConfiguration(GetDefaultSystemBackdropConfiguration(target, root));
					newController?.AddSystemBackdropTarget(target);
					controller = newController;
					break;
			}
		}

		private ISystemBackdropControllerWithTargets? GetSystemBackdropController(BackdropMaterialType backdropType)
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
					return new DesktopAcrylicController();

				default:
					return null;
			}
		}
	}
}
