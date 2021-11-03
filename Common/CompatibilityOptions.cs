using System;
using System.ComponentModel;

namespace Files.Common
{
    public class CompatibilityOptions
    {
        public OSCompatibility OSCompatibility { get; set; }
        public ReducedColorMode ReducedColorMode { get; set; }
        public bool ExecuteAt640X480 { get; set; } // 640X480
        public bool DisableMaximized { get; set; } // DISABLEDXMAXIMIZEDWINDOWEDMODE
        public bool RunAsAdministrator { get; set; } // RUNASADMIN
        public bool RegisterForRestart { get; set; } // REGISTERAPPRESTART
        public HighDpiOption HighDpiOption { get; set; }
        public HighDpiOverride HighDpiOverride { get; set; }

        public override string ToString()
        {
            var value = $"~ {OSCompatibility.GetDescription()} {ReducedColorMode.GetDescription()} {HighDpiOption.GetDescription()} {HighDpiOverride.GetDescription()} " +
                $"{(ExecuteAt640X480 ? "640X480" : "")} {(DisableMaximized ? "DISABLEDXMAXIMIZEDWINDOWEDMODE" : "")}" +
                $"{(RunAsAdministrator ? "RUNASADMIN" : "")} {(RegisterForRestart ? "REGISTERAPPRESTART" : "")}";
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
                    compatOptions.ExecuteAt640X480 |= value == "640X480";
                    compatOptions.DisableMaximized |= value == "DISABLEDXMAXIMIZEDWINDOWEDMODE";
                    compatOptions.RunAsAdministrator |= value == "RUNASADMIN";
                    compatOptions.RegisterForRestart |= value == "REGISTERAPPRESTART";
                }
            }    
            return compatOptions;
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
