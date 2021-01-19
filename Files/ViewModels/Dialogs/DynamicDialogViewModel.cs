using System;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels.Dialogs
{
    public class DynamicDialogViewModel : ObservableObject
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

        private Action<ContentDialogButtonClickEventArgs> primaryButtonAction;
        public Action<ContentDialogButtonClickEventArgs> PrimaryButtonAction 
        {
            get => primaryButtonAction;
            set
            {
                if (SetProperty(ref primaryButtonAction, value))
                {
                    PrimaryButtonCommand = new RelayCommand<ContentDialogButtonClickEventArgs>(PrimaryButtonAction);
                }
            }
        }

        private Action<ContentDialogButtonClickEventArgs> secondaryButtonAction;
        public Action<ContentDialogButtonClickEventArgs> SecondaryButtonAction 
        {
            get => secondaryButtonAction;
            set
            {
                if (SetProperty(ref secondaryButtonAction, value))
                {
                    SecondaryButtonCommand = new RelayCommand<ContentDialogButtonClickEventArgs>(SecondaryButtonAction);
                }
            }
        }

        #endregion

        #region Commands

        public ICommand PrimaryButtonCommand { get; private set; }

        public ICommand SecondaryButtonCommand { get; private set; }

        #endregion
    }
}
