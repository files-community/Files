using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Files.ViewModels
{
    public class ColumnsViewModel : ObservableObject
    {
        private GridLength iconColumnLength = new GridLength(30, GridUnitType.Pixel);
        public GridLength IconColumnLength
        {
            get => iconColumnLength;
            set => SetProperty(ref iconColumnLength, value);
        }
        private GridLength nameColumnLength = new GridLength(1, GridUnitType.Star);
        public GridLength NameColumnLength
        {
            get => nameColumnLength;
            set => SetProperty(ref nameColumnLength, value);
        }
        private GridLength statusColumnLength = new GridLength(40, GridUnitType.Pixel);
        public GridLength StatusColumnLength
        {
            get => statusColumnLength;
            set => SetProperty(ref statusColumnLength, value);
        }

        private Visibility statusColumnVisibility = Visibility.Visible;
        public Visibility StatusColumnVisibility
        {
            get => statusColumnVisibility;
            set => SetProperty(ref statusColumnVisibility, value);
        }

        private GridLength dateModifiedColumnLength = new GridLength(1, GridUnitType.Star);
        public GridLength DateModifiedColumnLength
        {
            get => dateModifiedColumnLength;
            set => SetProperty(ref dateModifiedColumnLength, value);
        }
        private GridLength itemTypeColumnLength = new GridLength(1, GridUnitType.Star);
        public GridLength ItemTypeColumnLength
        {
            get => itemTypeColumnLength;
            set => SetProperty(ref itemTypeColumnLength, value);
        }
    }
}
