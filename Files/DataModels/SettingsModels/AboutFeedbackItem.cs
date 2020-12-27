using Microsoft.Toolkit.Mvvm.Input;

namespace Files.DataModels.SettingsModels
{
    public class AboutFeedbackItem
    {
        public AboutFeedbackItem()
        {
        }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string AutomationPropertiesName { get; set; }
        public string Glyph { get; set; }
        public RelayCommand Command { get; set; }
    }
}
