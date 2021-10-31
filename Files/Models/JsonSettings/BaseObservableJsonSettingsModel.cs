using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Files.Models.JsonSettings
{
    public abstract class BaseObservableJsonSettingsModel : BaseJsonSettingsModel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public BaseObservableJsonSettingsModel()
            : base()
        {
        }

        protected BaseObservableJsonSettingsModel(string filePath)
            : base(filePath)
        {
        }

        protected BaseObservableJsonSettingsModel(string filePath, bool isCachingEnabled, IJsonSettingsSerializer jsonSettingsSerializer = null, ISettingsSerializer settingsSerializer = null)
            : base(filePath, isCachingEnabled, jsonSettingsSerializer, settingsSerializer)
        {
        }

        protected override bool Set<TValue>(TValue value, [CallerMemberName] string propertyName = "")
        {
            if (base.Set(value, propertyName))
            {
                OnPropertyChanged(propertyName);
                return true;
            }

            return false;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
