using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Files.Navigation
{
    public class UniversalPath : INotifyPropertyChanged
    {
        private string _WorkingDirectory;
        public string WorkingDirectory
        {
            get
            {
                return _WorkingDirectory;
            }

            set
            {
                if(!string.IsNullOrWhiteSpace(value))
                {
                    _WorkingDirectory = value;
                    
                    App.CurrentInstance.SidebarSelectedItem = App.sideBarItems.FirstOrDefault(x => x.Path != null && x.Path.Equals(value.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
                    if(App.CurrentInstance.SidebarSelectedItem == null)
                    {
                        App.CurrentInstance.SidebarSelectedItem = App.sideBarItems.FirstOrDefault(x => x.Path != null && x.Path.Equals(Path.GetPathRoot(value), StringComparison.OrdinalIgnoreCase));
                    }

                    NotifyPropertyChanged("path");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
