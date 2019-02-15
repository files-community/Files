using System.ComponentModel;

namespace Files.Navigation
{
    public class UniversalPath : INotifyPropertyChanged
    {


        public string _path;
        public string path
        {
            get
            {
                return _path;
            }

            set
            {
                if (value != _path)
                {
                    _path = value;
                    NotifyPropertyChanged("path");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

    }
}
