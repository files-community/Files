using System;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels.Dialogs
{
    public enum DynamicResult
    {
        OK = 0,
        Cancel = 1
    }

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

        public DynamicResult DynamicResult { get; set; }

        #endregion

        #region Actions

        private Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> primaryButtonAction;
        public Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> PrimaryButtonAction 
        {
            get => primaryButtonAction;
            set
            {
                if (SetProperty(ref primaryButtonAction, value))
                {
                    PrimaryButtonCommand = new RelayCommand<ContentDialogButtonClickEventArgs>((e) =>
                    {
                        DynamicResult = DynamicResult.OK;
                        PrimaryButtonAction(this, e);
                    });
                }
            }
        }

        private Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> secondaryButtonAction;
        public Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> SecondaryButtonAction 
        {
            get => secondaryButtonAction;
            set
            {
                if (SetProperty(ref secondaryButtonAction, value))
                {
                    SecondaryButtonCommand = new RelayCommand<ContentDialogButtonClickEventArgs>((e) =>
                    {
                        DynamicResult = DynamicResult.Cancel;
                        SecondaryButtonAction(this, e);
                    });
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
