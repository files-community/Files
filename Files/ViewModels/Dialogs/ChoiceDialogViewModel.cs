using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels.Dialogs
{
    public class ChoiceDialogViewModel : ObservableObject
    {
        #region Public Properties

        /// <summary>
        /// The control that is dynamically displayed on choice
        /// </summary>
        public object DisplayControl { get; set; }

        public string TitleText { get; set; }

        public string SubtitleText { get; set; }

        public string PrimaryButtonText { get; set; }

        public string SecondaryButtonText { get; set; }

        #endregion

        #region Actions

        public Action<ContentDialogButtonClickEventArgs> PrimaryButtonAction;

        public Action<ContentDialogButtonClickEventArgs> SecondaryButtonAction;

        #endregion

        #region Commands

        public ICommand PrimaryButtonCommand { get; private set; }

        public ICommand SecondaryButtonCommand { get; private set; }

        #endregion

        #region Constructor

        public ChoiceDialogViewModel()
        {
            // Create commands
            PrimaryButtonCommand = new RelayCommand<ContentDialogButtonClickEventArgs>(PrimaryButtonAction);
            SecondaryButtonCommand = new RelayCommand<ContentDialogButtonClickEventArgs>(SecondaryButtonAction);
        }

        #endregion
    }
}
