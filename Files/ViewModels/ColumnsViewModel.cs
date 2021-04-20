using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
        private double statusColumnMaxLength = 100;
        public double StatusColumnMaxLength
        {
            get => statusColumnMaxLength;
            set => SetProperty(ref statusColumnMaxLength, value);
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

        private GridLength originalPathColumnLength = new GridLength(1, GridUnitType.Star);
        public GridLength OriginalPathColumnLength
        {
            get => originalPathColumnLength;
            set => SetProperty(ref originalPathColumnLength, value);
        }

        private Visibility originalPathColumnVisibility = Visibility.Visible;
        public Visibility OriginalPathColumnVisibility
        {
            get => originalPathColumnVisibility;
            set => SetProperty(ref originalPathColumnVisibility, value);
        }

        private double originalPathColumnMaxLength = 500;
        public double OriginalPathMaxLength
        {
            get => originalPathColumnMaxLength;
            set => SetProperty(ref originalPathColumnMaxLength, value);
        }

        private GridLength dateDeletedColumnLength = new GridLength(1, GridUnitType.Star);
        public GridLength DateDeletedColumnLength
        {
            get => dateDeletedColumnLength;
            set => SetProperty(ref dateDeletedColumnLength, value);
        }
        private Visibility dateDeletedColumnVisibility = Visibility.Visible;
        public Visibility DateDeletedColumnVisibility
        {
            get => dateDeletedColumnVisibility;
            set => SetProperty(ref dateDeletedColumnVisibility, value);
        }

        private double dateDeletedColumnMaxLength = 200;
        public double DateDeletedMaxLength
        {
            get => dateDeletedColumnMaxLength;
            set => SetProperty(ref dateDeletedColumnMaxLength, value);
        }
        
        private double totalWidth = 600;
        public double TotalWidth
        {
            get => totalWidth;
            set => SetProperty(ref totalWidth, value);
        }
    }
}
