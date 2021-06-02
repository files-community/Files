using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Files.ViewModels
{
    public class SearchBoxViewModel : ObservableObject, ISearchBox
    {

        private string query;
        public string Query
        {
            get => query;
            set => SetProperty(ref query, value);
        }

        public event TypedEventHandler<Windows.UI.Xaml.Controls.AutoSuggestBox, Windows.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs> QueryChanged;
        public event TypedEventHandler<Windows.UI.Xaml.Controls.AutoSuggestBox, Windows.UI.Xaml.Controls.AutoSuggestBoxQuerySubmittedEventArgs> QuerySubmitted;
        public event TypedEventHandler<Windows.UI.Xaml.Controls.AutoSuggestBox, Windows.UI.Xaml.Controls.AutoSuggestBoxSuggestionChosenEventArgs> SuggestionChosen;
        public event EventHandler<Windows.UI.Xaml.Controls.AutoSuggestBox> Escaped;
    }
}
