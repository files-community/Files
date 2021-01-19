using System;
using System.Windows.Input;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Files.ViewModels.Dialogs
{
    public enum DynamicResult
    {
        Primary = 0,
        Secondary = 1,
        Cancel = 2
    }

    public enum DynamicButtons
    {
        Primary = 0,
        Secondary = 1,
        Cancel = 2
    }

    public class DynamicDialogViewModel : ObservableObject, IDisposable
    {
        #region Public Properties

        public object displayControl;
        /// <summary>
        /// The control that is dynamically displayed.
        /// </summary>
        public object DisplayControl
        {
            get => displayControl;
            set => SetProperty(ref displayControl, value);
        }

        private DynamicButtons dynamicButtons;
        /// <summary>
        /// Decides which buttons to show.
        /// <br/>
        /// <br/>
        /// Note:
        /// <br/>
        /// Setting value to <see cref="DynamicButtons"/> may override
        /// <see cref="PrimaryButtonText"/> and/or <see cref="SecondaryButtonText"/> and/or <see cref="CloseButtonText"/>.
        /// </summary>
        public DynamicButtons DynamicButtons
        {
            get => dynamicButtons;
            set
            {
                if (SetProperty(ref dynamicButtons, value))
                {
                    if (!value.HasFlag(DynamicButtons.Primary))
                    {
                        PrimaryButtonText = null; // Hides this option
                    }
                    if (!value.HasFlag(DynamicButtons.Secondary))
                    {
                        SecondaryButtonText = null; // Hides this option
                    }
                    if (!value.HasFlag(DynamicButtons.Cancel))
                    {
                        CloseButtonText = null; // Hides this option
                    }
                }
            }
        }

        private string titleText;
        public string TitleText
        {
            get => titleText;
            set => SetProperty(ref titleText, value);
        }

        private string subtitleText;
        public string SubtitleText
        {
            get => subtitleText;
            set => SetProperty(ref subtitleText, value);
        }

        private string primaryButtonText;
        public string PrimaryButtonText
        {
            get => primaryButtonText;
            set => SetProperty(ref primaryButtonText, value);
        }

        private string secondaryButtonText;
        public string SecondaryButtonText
        {
            get => secondaryButtonText;
            set => SetProperty(ref secondaryButtonText, value);
        }

        private string closeButtonText;
        public string CloseButtonText 
        {
            get => closeButtonText;
            set => SetProperty(ref closeButtonText, value);
        }

        public DynamicResult DynamicResult { get; set; }

        #endregion

        #region Actions

        /// <summary>
        /// Hides the dialog.
        /// <br/>
        /// <br/>
        /// Note:
        /// <br/>
        /// This action will be overriden.
        /// </summary>
        public Action HideDialog { get; set; }

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
                        DynamicResult = DynamicResult.Primary;
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
                        DynamicResult = DynamicResult.Secondary;
                        SecondaryButtonAction(this, e);
                    });
                }
            }
        }

        private Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> closeButtonAction;
        public Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> CloseButtonAction
        {
            get => closeButtonAction;
            set
            {
                if (SetProperty(ref closeButtonAction, value))
                {
                    CloseButtonCommand = new RelayCommand<ContentDialogButtonClickEventArgs>((e) =>
                    {
                        DynamicResult = DynamicResult.Cancel;
                        CloseButtonAction(this, e);
                    });
                }
            }
        }

        private Action<DynamicDialogViewModel, KeyRoutedEventArgs> keyDownAction;
        public Action<DynamicDialogViewModel, KeyRoutedEventArgs> KeyDownAction
        {
            get => keyDownAction;
            set
            {
                if (SetProperty(ref keyDownAction, value))
                {
                    DynamicKeyDownCommand = new RelayCommand<KeyRoutedEventArgs>((e) =>
                    {
                        DynamicResult = DynamicResult.Cancel;
                        KeyDownAction(this, e);
                    });
                }
            }
        }

        #endregion

        #region Commands

        public ICommand PrimaryButtonCommand { get; private set; }

        public ICommand SecondaryButtonCommand { get; private set; }

        public ICommand CloseButtonCommand { get; private set; }

        public ICommand DynamicKeyDownCommand { get; private set; }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            (displayControl as IDisposable)?.Dispose();

            displayControl = null;
            titleText = null;
            subtitleText = null;
            primaryButtonText = null;
            secondaryButtonText = null;
            closeButtonText = null;

            primaryButtonAction = null;
            secondaryButtonAction = null;
            closeButtonAction = null;
            keyDownAction = null;

            PrimaryButtonCommand = null;
            SecondaryButtonCommand = null;
            CloseButtonCommand = null;
        }

        #endregion
    }
}
