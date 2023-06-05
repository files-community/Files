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
			base.OnTargetConnected(connectedTarget, xamlRoot);
			this.target = connectedTarget;
			this.root = xamlRoot;
			controller = GetSystemBackdropController(userSettingsService.AppearanceSettingsService.AppThemeSystemBackdrop);
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
				case nameof(IAppearanceSettingsService.AppThemeSystemBackdrop):
					controller?.RemoveAllSystemBackdropTargets();
					controller?.Dispose();
					var newController = GetSystemBackdropController((SystemBackdropType)e.NewValue!);
					newController?.SetSystemBackdropConfiguration(GetDefaultSystemBackdropConfiguration(target, root));
					newController?.AddSystemBackdropTarget(target);
					controller = newController;
					break;
			}
		}

		private ISystemBackdropControllerWithTargets? GetSystemBackdropController(SystemBackdropType backdropType)
		{
			if (isSecondaryWindow && backdropType == SystemBackdropType.MicaAlt)
				backdropType = SystemBackdropType.Mica;

			switch (backdropType)
			{
				case SystemBackdropType.MicaAlt:
					return new MicaController()
					{
						Kind = MicaKind.BaseAlt
					};

				case SystemBackdropType.Mica:
					return new MicaController()
					{
						Kind = MicaKind.Base
					};

				case SystemBackdropType.Acrylic:
					return new DesktopAcrylicController();

				default:
					return null;
			}
		}
	}
}
