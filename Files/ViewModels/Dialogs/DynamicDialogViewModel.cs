using Files.Enums;
using Files.Helpers.XamlHelpers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Files.ViewModels.Dialogs
{
    public class DynamicDialogViewModel : ObservableObject, IDisposable
    {
        #region Public Properties

        public object displayControl;

        private bool displayControlLoad;

        private DynamicDialogButtons dynamicButtons;

        private DynamicDialogButtons dynamicButtonsEnabled;

        private bool subtitleLoad;

        private string subtitleText;

        private string titleText;

        /// <summary>
        /// Stores any additional data that could be written to, read from.
        /// </summary>
        public object AdditionalData { get; set; }

        /// <summary>
        /// The control that is dynamically displayed.
        /// </summary>
        public object DisplayControl
        {
            get => displayControl;
            set
            {
                if (SetProperty(ref displayControl, value))
                {
                    if (value == null)
                    {
                        DisplayControlLoad = false;
                    }
                    else
                    {
                        DisplayControlLoad = true;
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the <see cref="DisplayControl"/> is loaded, value of this property is automatically handled.
        /// </summary>
        public bool DisplayControlLoad
        {
            get => displayControlLoad;
            set => SetProperty(ref displayControlLoad, value);
        }

        /// <summary>
        /// Decides which buttons to show.
        /// <br/>
        /// <br/>
        /// Note:
        /// <br/>
        /// Setting value to <see cref="DynamicButtons"/> may override
        /// <see cref="PrimaryButtonText"/> and/or <see cref="SecondaryButtonText"/> and/or <see cref="CloseButtonText"/>.
        /// </summary>
        public DynamicDialogButtons DynamicButtons
        {
            get => dynamicButtons;
            set
            {
                if (SetProperty(ref dynamicButtons, value))
                {
                    if (value.HasFlag(DynamicDialogButtons.None))
                    {
                        PrimaryButtonText = null; // Hides this option
                        SecondaryButtonText = null; // Hides this option
                        CloseButtonText = null; // Hides this option

                        return;
                    }

                    if (!value.HasFlag(DynamicDialogButtons.Primary))
                    {
                        PrimaryButtonText = null; // Hides this option
                    }
                    if (!value.HasFlag(DynamicDialogButtons.Secondary))
                    {
                        SecondaryButtonText = null; // Hides this option
                    }
                    if (!value.HasFlag(DynamicDialogButtons.Cancel))
                    {
                        CloseButtonText = null; // Hides this option
                    }
                }
            }
        }

        /// <summary>
        /// Determines which buttons should be enabled
        /// </summary>
        public DynamicDialogButtons DynamicButtonsEnabled
        {
            get => dynamicButtonsEnabled;
            set
            {
                if (SetProperty(ref dynamicButtonsEnabled, value))
                {
                    if (!value.HasFlag(DynamicDialogButtons.Cancel))
                    {
                        Debugger.Break(); // Cannot disable the Close button!
                    }

                    if (value.HasFlag(DynamicDialogButtons.None))
                    {
                        IsPrimaryButtonEnabled = false; // Hides this option
                        IsSecondaryButtonEnabled = false; // Hides this option

                        return;
                    }

                    if (!value.HasFlag(DynamicDialogButtons.Primary))
                    {
                        IsPrimaryButtonEnabled = false; // Hides this option
                    }
                    else
                    {
                        IsPrimaryButtonEnabled = true;
                    }

                    if (!value.HasFlag(DynamicDialogButtons.Secondary))
                    {
                        IsSecondaryButtonEnabled = false; // Hides this option
                    }
                    else
                    {
                        IsSecondaryButtonEnabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// The result of the dialog, value of this property is automatically handled.
        /// </summary>
        public DynamicDialogResult DynamicResult { get; set; }

        /// <summary>
        /// Determines whether the <see cref="SubtitleText"/> is loaded, value of this property is automatically handled.
        /// </summary>
        public bool SubtitleLoad
        {
            get => subtitleLoad;
            private set => SetProperty(ref subtitleLoad, value);
        }

        /// <summary>
        /// The subtitle of the dialog.
        /// <br/>
        /// (Can be null or empty)
        /// </summary>
        public string SubtitleText
        {
            get => subtitleText;
            set
            {
                if (SetProperty(ref subtitleText, value))
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        SubtitleLoad = true;
                    }
                    else
                    {
                        SubtitleLoad = false;
                    }
                }
            }
        }

        /// <summary>
        /// The Title text of the dialog.
        /// </summary>
        public string TitleText
        {
            get => titleText;
            set => SetProperty(ref titleText, value);
        }

        #region Primary Button

        private bool isPrimaryButtonEnabled;
        private string primaryButtonText;

        /// <summary>
        /// Determines whether Primary Button is enabled.
        /// </summary>
        public bool IsPrimaryButtonEnabled
        {
            get => isPrimaryButtonEnabled;
            private set => SetProperty(ref isPrimaryButtonEnabled, value);
        }

        /// <summary>
        /// The text content of the primary button.
        /// </summary>
        public string PrimaryButtonText
        {
            get => primaryButtonText;
            set => SetProperty(ref primaryButtonText, value);
        }

        #endregion Primary Button

        #region Secondary Button

        private bool isSecondaryButtonEnabled;
        private string secondaryButtonText;

        /// <summary>
        /// Determines whether Secondary Button is enabled.
        /// </summary>
        public bool IsSecondaryButtonEnabled
        {
            get => isSecondaryButtonEnabled;
            private set => SetProperty(ref isSecondaryButtonEnabled, value);
        }

        /// <summary>
        /// The text of the secondary button.
        /// </summary>
        public string SecondaryButtonText
        {
            get => secondaryButtonText;
            set => SetProperty(ref secondaryButtonText, value);
        }

        #endregion Secondary Button

        #region Close Button

        private string closeButtonText;

        /// <summary>
        /// The text of the close button.
        /// </summary>
        public string CloseButtonText
        {
            get => closeButtonText;
            set => SetProperty(ref closeButtonText, value);
        }

        #endregion Close Button

        #endregion Public Properties

        #region Actions

        private Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> closeButtonAction;

        private Action<DynamicDialogViewModel, RoutedEventArgs> displayControlOnLoaded;

        private Action<DynamicDialogViewModel, KeyRoutedEventArgs> keyDownAction;

        private Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> primaryButtonAction;

        private Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> secondaryButtonAction;

        /// <summary>
        /// OnClose action.
        /// </summary>
        public Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> CloseButtonAction
        {
            get => closeButtonAction;
            set
            {
                if (SetProperty(ref closeButtonAction, value))
                {
                    CloseButtonCommand = new RelayCommand<ContentDialogButtonClickEventArgs>((e) =>
                    {
                        DynamicResult = DynamicDialogResult.Cancel;
                        CloseButtonAction(this, e);
                    });
                }
            }
        }

        public Action<DynamicDialogViewModel, RoutedEventArgs> DisplayControlOnLoaded
        {
            get => displayControlOnLoaded;
            set
            {
                if (SetProperty(ref displayControlOnLoaded, value))
                {
                    DisplayControlOnLoadedCommand = new RelayCommand<RoutedEventArgs>((e) =>
                    {
                        DisplayControlOnLoaded(this, e);
                    });
                }
            }
        }

        /// <summary>
        /// Hides the dialog.
        /// <br/>
        /// <br/>
        /// Note:
        /// <br/>
        /// This action is assigned by default.
        /// </summary>
        public Action HideDialog { get; set; }

        /// <summary>
        /// The keydown action on the dialog.
        /// <br/>
        /// <br/>
        /// Note:
        /// <br/>
        /// This action is assigned by default.
        /// </summary>
        public Action<DynamicDialogViewModel, KeyRoutedEventArgs> KeyDownAction
        {
            get => keyDownAction;
            set
            {
                if (SetProperty(ref keyDownAction, value))
                {
                    KeyDownCommand = new RelayCommand<KeyRoutedEventArgs>((e) =>
                    {
                        DynamicResult = DynamicDialogResult.Cancel;
                        KeyDownAction(this, e);
                    });
                }
            }
        }

        /// <summary>
        /// OnPrimary action.
        /// </summary>
        public Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> PrimaryButtonAction
        {
            get => primaryButtonAction;
            set
            {
                if (SetProperty(ref primaryButtonAction, value))
                {
                    PrimaryButtonCommand = new RelayCommand<ContentDialogButtonClickEventArgs>((e) =>
                    {
                        DynamicResult = DynamicDialogResult.Primary;
                        PrimaryButtonAction(this, e);
                    });
                }
            }
        }

        /// <summary>
        /// OnSecondary action.
        /// </summary>
        public Action<DynamicDialogViewModel, ContentDialogButtonClickEventArgs> SecondaryButtonAction
        {
            get => secondaryButtonAction;
            set
            {
                if (SetProperty(ref secondaryButtonAction, value))
                {
                    SecondaryButtonCommand = new RelayCommand<ContentDialogButtonClickEventArgs>((e) =>
                    {
                        DynamicResult = DynamicDialogResult.Secondary;
                        SecondaryButtonAction(this, e);
                    });
                }
            }
        }

        #endregion Actions

        #region Commands

        public ICommand CloseButtonCommand { get; private set; }
        public ICommand DisplayControlOnLoadedCommand { get; private set; }
        public ICommand KeyDownCommand { get; private set; }
        public ICommand PrimaryButtonCommand { get; private set; }

        public ICommand SecondaryButtonCommand { get; private set; }

        #endregion Commands

        #region Constructor

        public DynamicDialogViewModel()
        {
            // Create default implementation
            TitleText = "DynamicDialog";
            PrimaryButtonText = "Ok";
            PrimaryButtonAction = (vm, e) => HideDialog();
            SecondaryButtonAction = (vm, e) => HideDialog();
            CloseButtonAction = (vm, e) => HideDialog();
            KeyDownAction = (vm, e) =>
            {
                if (e.Key == VirtualKey.Escape)
                {
                    HideDialog();
                }
            };
            DisplayControlOnLoaded = (vm, e) =>
            {
                Control control = (vm.DisplayControl as Control);

                if (control == null)
                {
                    control = DependencyObjectHelpers.FindChild<Control>(vm.DisplayControl as DependencyObject);
                }

                control?.Focus(FocusState.Programmatic);
            };

            DynamicButtons = DynamicDialogButtons.Primary;
            DynamicButtonsEnabled = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary | DynamicDialogButtons.Cancel;
        }

        #endregion Constructor

        #region IDisposable

        public void Dispose()
        {
            (displayControl as IDisposable)?.Dispose();

            AdditionalData = null;
            displayControl = null;
            titleText = null;
            subtitleText = null;
            primaryButtonText = null;
            secondaryButtonText = null;
            closeButtonText = null;

            HideDialog = null;
            primaryButtonAction = null;
            secondaryButtonAction = null;
            closeButtonAction = null;
            keyDownAction = null;
            displayControlOnLoaded = null;

            PrimaryButtonCommand = null;
            SecondaryButtonCommand = null;
            CloseButtonCommand = null;
            KeyDownCommand = null;
            DisplayControlOnLoadedCommand = null;
        }

        #endregion IDisposable
    }
}