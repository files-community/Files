using System;
using System.ComponentModel;

namespace Files.Common
{
    public class CompatibilityOptions
    {
        public OSCompatibility OSCompatibility { get; set; }
        public ReducedColorMode ReducedColorMode { get; set; }
        public bool ExecuteAt640X480 { get; set; }
        public bool DisableMaximized { get; set; }
        public bool RunAsAdministrator { get; set; }
        public bool RegisterForRestart { get; set; }
        public HighDpiOption HighDpiOption { get; set; }
        public HighDpiOverride HighDpiOverride { get; set; }

        public override string ToString()
        {
            var value = $"~ {OSCompatibility.GetDescription()} {ReducedColorMode.GetDescription()} {HighDpiOption.GetDescription()} {HighDpiOverride.GetDescription()} " +
                $"{(ExecuteAt640X480 ? CompatOptions.RegExecuteAt640X480 : "")} {(DisableMaximized ? CompatOptions.RegDisableMaximized : "")} " +
                $"{(RunAsAdministrator ? CompatOptions.RegRunAsAdministrator : "")} {(RegisterForRestart ? CompatOptions.RegRegisterForRestart : "")}";
            return System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"\s+", " ");
        }

        public static CompatibilityOptions FromString(string rawValue)
        {
            var compatOptions = new CompatibilityOptions();
            if (!string.IsNullOrEmpty(rawValue))
            {
                var components = rawValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var value in components)
                {
                    compatOptions.HighDpiOption |= Extensions.GetValueFromDescription<HighDpiOption>(value);
                    compatOptions.HighDpiOverride |= Extensions.GetValueFromDescription<HighDpiOverride>(value);
                    compatOptions.ReducedColorMode |= Extensions.GetValueFromDescription<ReducedColorMode>(value);
                    compatOptions.OSCompatibility |= Extensions.GetValueFromDescription<OSCompatibility>(value);
                    compatOptions.ExecuteAt640X480 |= value == CompatOptions.RegExecuteAt640X480;
                    compatOptions.DisableMaximized |= value == CompatOptions.RegDisableMaximized;
                    compatOptions.RunAsAdministrator |= value == CompatOptions.RegRunAsAdministrator;
                    compatOptions.RegisterForRestart |= value == CompatOptions.RegRegisterForRestart;
                }
            }    
            return compatOptions;
        }

        private class CompatOptions
        {
            public const string RegExecuteAt640X480 = "640X480";
            public const string RegDisableMaximized = "DISABLEDXMAXIMIZEDWINDOWEDMODE";
            public const string RegRunAsAdministrator = "RUNASADMIN";
            public const string RegRegisterForRestart = "REGISTERAPPRESTART";
        }
    }

    [Flags]
    public enum HighDpiOverride
    {
        [Description("")]
        None = 0,
        [Description("HIGHDPIAWARE")]
        Application = 1,
        [Description("DPIUNAWARE")]
        System = 2,
        [Description("GDIDPISCALING")]
        Advanced = 4,
        [Description("GDIDPISCALING DPIUNAWARE")]
        SystemAdvanced = 6,
    }

    public enum HighDpiOption
    {
        [Description("")]
        None,
        [Description("PERPROCESSSYSTEMDPIFORCEOFF")]
        UseDPIOnLogin,
        [Description("PERPROCESSSYSTEMDPIFORCEON")]
        UseDPIOnProgramStart,
    }

    public enum ReducedColorMode
    {
        [Description("")]
        None,
        [Description("256COLOR")]
        Color8Bit,
        [Description("16BITCOLOR")]
        Color16Bit
    }

    public enum OSCompatibility
    {
        [Description("")]
        None,
        [Description("VISTARTM")]
        WindowsVista,
        [Description("VISTASP1")]
        WindowsVistaSP1,
        [Description("VISTASP2")]
        WindowsVistaSP2,
        [Description("WIN7RTM")]
        Windows7,
        [Description("WIN8RTM")]
        Windows8
    }
}
