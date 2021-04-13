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
        private GridLength row1Width = new GridLength(30, GridUnitType.Pixel);
        public GridLength Row1Width
        {
            get => row1Width;
            set => SetProperty(ref row1Width, value);
        }
        private GridLength row2Width = new GridLength(1, GridUnitType.Star);
        public GridLength Row2Width
        {
            get => row2Width;
            set => SetProperty(ref row2Width, value);
        }
        private GridLength row3Width = new GridLength(40, GridUnitType.Pixel);
        public GridLength Row3Width
        {
            get => row3Width;
            set => SetProperty(ref row3Width, value);
        }

        private GridLength row4Width = new GridLength(1, GridUnitType.Star);
        public GridLength Row4Width
        {
            get => row4Width;
            set => SetProperty(ref row4Width, value);
        }
        private GridLength row5Width = new GridLength(1, GridUnitType.Star);
        public GridLength Row5Width
        {
            get => row5Width;
            set => SetProperty(ref row5Width, value);
        }
    }
}
