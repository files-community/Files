using Microsoft.Toolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Windows.UI.Xaml;

namespace Files.ViewModels
{
    public class ColumnsViewModel : ObservableObject
    {
        private ColumnViewModel iconColumn = new ColumnViewModel()
        {
            UserLength = new GridLength(44, GridUnitType.Pixel),
        };

        public ColumnViewModel IconColumn
        {
            get => iconColumn;
            set => SetProperty(ref iconColumn, value);
        }

        private ColumnViewModel nameColumn = new ColumnViewModel();

        public ColumnViewModel NameColumn
        {
            get => nameColumn;
            set => SetProperty(ref nameColumn, value);
        }


        private ColumnViewModel statusColumn = new ColumnViewModel();

        public ColumnViewModel StatusColumn
        {
            get => statusColumn;
            set => SetProperty(ref statusColumn, value);
        }


        private ColumnViewModel dateModifiedColumn = new ColumnViewModel();
        public ColumnViewModel DateModifiedColumn
        {
            get => dateModifiedColumn;
            set => SetProperty(ref dateModifiedColumn, value);
        }


        private ColumnViewModel originalPathColumn = new ColumnViewModel()
        {
            NormalMaxLength = 500,
        };
        public ColumnViewModel OriginalPathColumn
        {
            get => originalPathColumn;
            set => SetProperty(ref originalPathColumn, value);
        }


        private ColumnViewModel itemTypeColumn = new ColumnViewModel();
        public ColumnViewModel ItemTypeColumn
        {
            get => itemTypeColumn;
            set => SetProperty(ref itemTypeColumn, value);
        }


        private ColumnViewModel dateDeletedColumn = new ColumnViewModel();
        public ColumnViewModel DateDeletedColumn
        {
            get => dateDeletedColumn;
            set => SetProperty(ref dateDeletedColumn, value);
        }


        private ColumnViewModel dateCreatedColumn = new ColumnViewModel()
        {
            UserCollapsed = true
        };

        public ColumnViewModel DateCreatedColumn
        {
            get => dateCreatedColumn;
            set => SetProperty(ref dateCreatedColumn, value);
        }


        private ColumnViewModel sizeColumn = new ColumnViewModel();
        public ColumnViewModel SizeColumn
        {
            get => sizeColumn;
            set => SetProperty(ref sizeColumn, value);
        }

        public double TotalWidth => IconColumn.Length.Value + NameColumn.Length.Value + StatusColumn.Length.Value + DateModifiedColumn.Length.Value + OriginalPathColumn.Length.Value
            + ItemTypeColumn.Length.Value + DateDeletedColumn.Length.Value + DateCreatedColumn.Length.Value + SizeColumn.Length.Value;

        public void SetDesiredSize(double width)
        {
            if (TotalWidth > width || TotalWidth < width)
            {
                var proportion = width / TotalWidth;
                SetColumnSizeProportionally(proportion);
            }
        }

        /// <summary>
        /// Multiplies every column's length by the given amount if it is within the size
        /// </summary>
        /// <param name="factor"></param>
        private void SetColumnSizeProportionally(double factor)
        {
            NameColumn.TryMultiplySize(factor);
            StatusColumn.TryMultiplySize(factor);
            DateModifiedColumn.TryMultiplySize(factor);
            OriginalPathColumn.TryMultiplySize(factor);
            ItemTypeColumn.TryMultiplySize(factor);
            DateDeletedColumn.TryMultiplySize(factor);
            DateCreatedColumn.TryMultiplySize(factor);
            SizeColumn.TryMultiplySize(factor);
        }
    }

    public class ColumnViewModel : ObservableObject
    {

        private bool isHidden;
        public bool IsHidden
        {
            get => isHidden;
            set => SetProperty(ref isHidden, value);
        }

        public double MaxLength
        {
            get => IsHidden || UserCollapsed ? 0 : NormalMaxLength;
        }

        private double normalMaxLength = 800;
        [JsonIgnore]
        public double NormalMaxLength
        {
            get => normalMaxLength;
            set => SetProperty(ref normalMaxLength, value);
        }


        private double normalMinLength = 50;
        [JsonIgnore]
        public double NormalMinLength
        {
            get => normalMinLength;
            set
            {
                if (SetProperty(ref normalMinLength, value))
                {
                    OnPropertyChanged(nameof(MinLength));
                }
            }
        }

        public double MinLength => IsHidden || UserCollapsed ? 0 : NormalMinLength;

        public Visibility Visibility => IsHidden || UserCollapsed ? Visibility.Collapsed : Visibility.Visible;

        private bool userCollapsed;
        public bool UserCollapsed
        {
            get => userCollapsed;
            set
            {
                if (SetProperty(ref userCollapsed, value))
                {
                    UpdateVisibility();
                }
            }
        }

        public GridLength Length
        {
            get => IsHidden || UserCollapsed ? new GridLength(0) : UserLength;

        }

        [JsonIgnore]
        private GridLength userLength = new GridLength(200, GridUnitType.Pixel);
        public GridLength UserLength
        {
            get => userLength;
            set
            {
                if (SetProperty(ref userLength, value))
                {
                    OnPropertyChanged(nameof(Length));
                }
            }
        }

        public double UserLengthPixels
        {
            get => UserLength.Value;
            set => UserLength = new GridLength(value, GridUnitType.Pixel);
        }

        public void Hide()
        {
            IsHidden = true;
            UpdateVisibility();
        }

        public void Show()
        {
            IsHidden = false;
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            OnPropertyChanged(nameof(Length));
            OnPropertyChanged(nameof(MaxLength));
            OnPropertyChanged(nameof(Visibility));
            OnPropertyChanged(nameof(MinLength));
        }

        public void TryMultiplySize(double factor)
        {
            var newSize = Length.Value * factor;
            if (newSize == 0)
            {
                return;
            }

            double setLength = newSize;
            if (newSize < MinLength)
            {
                setLength = MinLength;
            }
            else if (newSize >= MaxLength)
            {
                setLength = MaxLength;
            }

            UserLength = new GridLength(setLength);
        }
    }
}
