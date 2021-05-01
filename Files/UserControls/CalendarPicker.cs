using System;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls
{
    public class CalendarPicker : CalendarDatePicker
    {
        public CalendarPicker()
        {
            DateChanged += CalendarPicker_DateChanged;
        }

        private void CalendarPicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (args.NewDate != null && args.NewDate.Value.Year == DateTime.Today.Year - 100)
            {
                SetDisplayDate(DateTimeOffset.Now);
                Date = null;
            }
        }
    }
}
