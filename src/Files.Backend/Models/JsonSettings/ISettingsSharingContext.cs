using Files.Backend.EventArguments;

namespace Files.Backend.Models.JsonSettings
{
    public interface ISettingsSharingContext
    {
        string FilePath { get; }

        IJsonSettingsDatabase JsonSettingsDatabase { get; }

        ISettingsSharingContext GetSharingContext();

        void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e);
    }
}
