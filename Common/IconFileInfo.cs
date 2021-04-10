using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Files.Common
{
    public class IconFileInfo : INotifyPropertyChanged
    {
        public string IconData { get; }
        public int Index { get; }

        private object imageSource = null;
        public object ImageSource
        {
            get => imageSource;
            set
            {
                if (value != imageSource)
                {
                    imageSource = value;
                    RaisePropertyChanged("ImageSource");
                }
            }
        }

        public IconFileInfo(string iconData, int index)
        {
            IconData = iconData;
            Index = index;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
