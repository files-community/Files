using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable enable

namespace Files.Uwp.Serialization
{
    internal abstract class BaseObservableJsonSettings : BaseJsonSettings, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected override bool Set<TValue>(TValue? value, [CallerMemberName] string propertyName = "")
            where TValue : default
        {
            if (base.Set<TValue>(value, propertyName))
            {
                OnPropertyChanged(propertyName);
                return true;
            }

            return false;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
