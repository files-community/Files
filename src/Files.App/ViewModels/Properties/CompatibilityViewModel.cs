// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.Utils;
using Files.App.Helpers;
using Files.App.Utils.Shell;
using Files.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.ViewModels.Properties
{
	public class CompatibilityViewModel : ObservableObject
	{
		public ListedItem Item { get; }

		private string ExePath
			=> Item is ShortcutItem sht ? sht.TargetPath : Item.ItemPath;

		private CompatibilityOptions compatibilityOptions;
		public CompatibilityOptions CompatibilityOptions
		{
			get => compatibilityOptions;
			set
			{
				if (SetProperty(ref compatibilityOptions, value))
				{
					ExecuteAt640X480 = value.ExecuteAt640X480;
					DisableMaximized = value.DisableMaximized;
					RunAsAdministrator = value.RunAsAdministrator;
					RegisterForRestart = value.RegisterForRestart;
					OSCompatibility = OSCompatibilityList.SingleOrDefault(x => x.Value == value.OSCompatibility);
					HighDpiOption = HighDpiOptionList.SingleOrDefault(x => x.Value == value.HighDpiOption);
					HighDpiOverride = HighDpiOverrideList.SingleOrDefault(x => x.Value == value.HighDpiOverride);
					ReducedColorMode = ReducedColorModeList.SingleOrDefault(x => x.Value == value.ReducedColorMode);
				}
			}
		}

		private bool executeAt640X480;
		public bool ExecuteAt640X480
		{
			get => executeAt640X480;
			set
			{
				if (SetProperty(ref executeAt640X480, value))
				{
					compatibilityOptions.ExecuteAt640X480 = value;
				}
			}
		}

		private bool disableMaximized;
		public bool DisableMaximized
		{
			get => disableMaximized;
			set
			{
				if (SetProperty(ref disableMaximized, value))
				{
					compatibilityOptions.DisableMaximized = value;
				}
			}
		}

		private bool runAsAdministrator;
		public bool RunAsAdministrator
		{
			get => runAsAdministrator;
			set
			{
				if (SetProperty(ref runAsAdministrator, value))
				{
					compatibilityOptions.RunAsAdministrator = value;
				}
			}
		}

		private bool registerForRestart;
		public bool RegisterForRestart
		{
			get => registerForRestart;
			set
			{
				if (SetProperty(ref registerForRestart, value))
				{
					compatibilityOptions.RegisterForRestart = value;
				}
			}
		}

		public EnumToLocalizedConverter<OSCompatibility> osCompatibility;
		public EnumToLocalizedConverter<OSCompatibility> OSCompatibility
		{
			get => osCompatibility;
			set
			{
				if (SetProperty(ref osCompatibility, value))
				{
					compatibilityOptions.OSCompatibility = value.Value;
				}
			}
		}

		public EnumToLocalizedConverter<ReducedColorMode> reducedColorMode;
		public EnumToLocalizedConverter<ReducedColorMode> ReducedColorMode
		{
			get => reducedColorMode;
			set
			{
				if (SetProperty(ref reducedColorMode, value))
				{
					compatibilityOptions.ReducedColorMode = value.Value;
				}
			}
		}

		public EnumToLocalizedConverter<HighDpiOption> highDpiOption;
		public EnumToLocalizedConverter<HighDpiOption> HighDpiOption
		{
			get => highDpiOption;
			set
			{
				if (SetProperty(ref highDpiOption, value))
				{
					compatibilityOptions.HighDpiOption = value.Value;
				}
			}
		}

		public EnumToLocalizedConverter<HighDpiOverride> highDpiOverride;
		public EnumToLocalizedConverter<HighDpiOverride> HighDpiOverride
		{
			get => highDpiOverride;
			set
			{
				if (SetProperty(ref highDpiOverride, value))
				{
					compatibilityOptions.HighDpiOverride = value.Value;
				}
			}
		}

		public List<EnumToLocalizedConverter<HighDpiOption>> HighDpiOptionList { get; } = Enum.GetValues(typeof(HighDpiOption)).Cast<HighDpiOption>().Select(x => new EnumToLocalizedConverter<HighDpiOption>(x)).ToList();
		public List<EnumToLocalizedConverter<HighDpiOverride>> HighDpiOverrideList { get; } = Enum.GetValues(typeof(HighDpiOverride)).Cast<HighDpiOverride>().Where(x => x != Core.Data.Items.HighDpiOverride.Advanced).Select(x => new EnumToLocalizedConverter<HighDpiOverride>(x)).ToList();
		public List<EnumToLocalizedConverter<OSCompatibility>> OSCompatibilityList { get; } = Enum.GetValues(typeof(OSCompatibility)).Cast<OSCompatibility>().Select(x => new EnumToLocalizedConverter<OSCompatibility>(x)).ToList();
		public List<EnumToLocalizedConverter<ReducedColorMode>> ReducedColorModeList { get; } = Enum.GetValues(typeof(ReducedColorMode)).Cast<ReducedColorMode>().Select(x => new EnumToLocalizedConverter<ReducedColorMode>(x)).ToList();

		public IRelayCommand RunTroubleshooterCommand { get; set; }

		public CompatibilityViewModel(ListedItem item)
		{
			Item = item;

			RunTroubleshooterCommand = new AsyncRelayCommand(RunTroubleshooter);
		}

		public void GetCompatibilityOptions()
		{
			var options = FileOperationsHelpers.ReadCompatOptions(ExePath);

			CompatibilityOptions = CompatibilityOptions.FromString(options);
		}

		public bool SetCompatibilityOptions()
			=> FileOperationsHelpers.SetCompatOptions(ExePath, CompatibilityOptions.ToString());

		public Task RunTroubleshooter()
			=> LaunchHelper.RunCompatibilityTroubleshooterAsync(ExePath);
	}
}
