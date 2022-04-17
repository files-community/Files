﻿using Files.Shared;
using Files.Shared.Extensions;
using Files.Uwp.Filesystem;
using Files.Uwp.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Files.Uwp.Helpers;

namespace Files.Uwp.ViewModels.Properties
{
    public class CompatibilityProperties : ObservableObject
    {
        public ListedItem Item { get; }

        private string ExePath => Item is ShortcutItem sht ? sht.TargetPath : Item.ItemPath;

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

        public LocalizedEnumHelper<OSCompatibility> osCompatibility;
        public LocalizedEnumHelper<OSCompatibility> OSCompatibility
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

        public LocalizedEnumHelper<ReducedColorMode> reducedColorMode;
        public LocalizedEnumHelper<ReducedColorMode> ReducedColorMode
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

        public LocalizedEnumHelper<HighDpiOption> highDpiOption;
        public LocalizedEnumHelper<HighDpiOption> HighDpiOption
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

        public LocalizedEnumHelper<HighDpiOverride> highDpiOverride;
        public LocalizedEnumHelper<HighDpiOverride> HighDpiOverride
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

        public List<LocalizedEnumHelper<HighDpiOption>> HighDpiOptionList { get; } = Enum.GetValues(typeof(HighDpiOption)).Cast<HighDpiOption>().Select(x => new LocalizedEnumHelper<HighDpiOption>(x)).ToList();
        public List<LocalizedEnumHelper<HighDpiOverride>> HighDpiOverrideList { get; } = Enum.GetValues(typeof(HighDpiOverride)).Cast<HighDpiOverride>().Where(x => x != Shared.HighDpiOverride.Advanced).Select(x => new LocalizedEnumHelper<HighDpiOverride>(x)).ToList();
        public List<LocalizedEnumHelper<OSCompatibility>> OSCompatibilityList { get; } = Enum.GetValues(typeof(OSCompatibility)).Cast<OSCompatibility>().Select(x => new LocalizedEnumHelper<OSCompatibility>(x)).ToList();
        public List<LocalizedEnumHelper<ReducedColorMode>> ReducedColorModeList { get; } = Enum.GetValues(typeof(ReducedColorMode)).Cast<ReducedColorMode>().Select(x => new LocalizedEnumHelper<ReducedColorMode>(x)).ToList();

        public IRelayCommand RunTroubleshooterCommand { get; set; }

        public CompatibilityProperties(ListedItem item)
        {
            Item = item;

            RunTroubleshooterCommand = new AsyncRelayCommand(RunTroubleshooter);
        }

        public async void GetCompatibilityOptions()
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var value = new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "ReadCompatOptions" },
                    { "filepath", ExePath }
                };
                var (status, response) = await connection.SendMessageForResponseAsync(value);
                if (status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success
                    && response.ContainsKey("CompatOptions"))
                {
                    CompatibilityOptions = CompatibilityOptions.FromString((string)response["CompatOptions"]);
                }
            }
        }

        public async Task<bool> SetCompatibilityOptions()
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var value = new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "SetCompatOptions" },
                    { "filepath", ExePath },
                    { "options", CompatibilityOptions?.ToString() }
                };
                var (status, response) = await connection.SendMessageForResponseAsync(value);
                return (status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success
                    && response.Get("Success", false));
            }
            return false;
        }

        public async Task RunTroubleshooter()
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var value = new ValueSet()
                {
                    { "Arguments", "RunCompatibilityTroubleshooter" },
                    { "filepath", ExePath }
                };
                await connection.SendMessageAsync(value);
            }
        }
    }
}
