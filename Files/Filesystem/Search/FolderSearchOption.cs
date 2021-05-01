using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;

namespace Files.Filesystem.Search
{
    public class FolderSearchOption : ObservableObject
    {
        private DateTimeOffset? minDate = null;

        public DateTimeOffset? MinDate
        {
            get => minDate;
            set => SetProperty(ref minDate, value);
        }

        private DateTimeOffset? maxDate = null;

        public DateTimeOffset? MaxDate
        {
            get => maxDate;
            set => SetProperty(ref maxDate, value);
        }

        public void Clear()
        {
            MinDate = null;
            MaxDate = null;
        }
    }
}
