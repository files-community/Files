using Files.Enums;
using Files.Helpers;
using Files.Interacts;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class StatusCenter : UserControl, IStatusCenterActions
    {
        #region Public Properties

        public static ObservableCollection<StatusBanner> StatusBannersSource { get; private set; } = new ObservableCollection<StatusBanner>();

        public int OngoingOperationsCount
        {
            get
            {
                int count = 0;

                foreach (var item in StatusBannersSource)
                {
                    if (item.IsProgressing)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool AnyOperationsOngoing
        {
            get => OngoingOperationsCount > 0;
        }

        #endregion

        #region Events

        public event EventHandler<PostedStatusBanner> ProgressBannerPosted;

        #endregion

        #region Constructor

        public StatusCenter()
        {
            this.InitializeComponent();
        }

        #endregion

        #region IStatusCenterActions

        public PostedStatusBanner PostBanner(string title, string message, float initialProgress, ReturnResult status, FileOperationType operation)
        {
            StatusBanner banner = new StatusBanner(message, title, initialProgress, status, operation);
            PostedStatusBanner postedBanner = new PostedStatusBanner(banner, this);

            StatusBannersSource.Add(banner);
            ProgressBannerPosted?.Invoke(this, postedBanner);
            return postedBanner;
        }

        public PostedStatusBanner PostActionBanner(string title, string message, string primaryButtonText, string cancelButtonText, Action primaryAction)
        {
            StatusBanner banner = new StatusBanner(message, title, primaryButtonText, cancelButtonText, primaryAction);
            PostedStatusBanner postedBanner = new PostedStatusBanner(banner, this);

            StatusBannersSource.Add(banner);
            ProgressBannerPosted?.Invoke(this, postedBanner);
            return postedBanner;
        }

        public bool CloseBanner(StatusBanner banner)
        {
            if (!StatusBannersSource.Contains(banner))
            {
                return false;
            }

            StatusBannersSource.Remove(banner);
            return true;
        }

        #endregion

        // Dismiss banner button event handler
        private void DismissBanner(object sender, RoutedEventArgs e)
        {
            StatusBanner itemToDismiss = (sender as Button).DataContext as StatusBanner;
            CloseBanner(itemToDismiss);
        }

        // Primary action button click
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            StatusBanner itemToDismiss = (sender as Button).DataContext as StatusBanner;
            await Task.Run(itemToDismiss.PrimaryButtonClick);
            CloseBanner(itemToDismiss);
        }
    }

    public class PostedStatusBanner
    {
        #region Private Members

        private readonly IStatusCenterActions statusCenterActions;
        
        private readonly StatusBanner Banner;

        #endregion

        #region Public Members

        public readonly Progress<float> Progress;

        public readonly Progress<FileSystemStatusCode> ErrorCode;

        #endregion

        #region Constructor

        public PostedStatusBanner(StatusBanner banner, IStatusCenterActions statusCenterActions)
        {
            this.Banner = banner;
            this.statusCenterActions = statusCenterActions;

            this.Progress = new Progress<float>(ReportProgressToBanner);
            this.ErrorCode = new Progress<FileSystemStatusCode>((errorCode) => ReportProgressToBanner(errorCode.ToStatus()));
        }

        #endregion

        #region Private Helpers

        private void ReportProgressToBanner(float value)
        {
            if (value <= 100.0f)
            {
                Banner.IsProgressing = true;
                Banner.Progress = value;
                Banner.FullTitle = $"{Banner.Title} ({value:0.00}%)";
                return;
            }
            else
            {
                Debugger.Break(); // Argument out of range :(
            }

            Banner.IsProgressing = false;
        }

        private void ReportProgressToBanner(ReturnResult value)
        {
        }

        #endregion

        #region Public Helpers

        public void Remove()
        {
            statusCenterActions.CloseBanner(Banner);
        }

        #endregion
    }

    public class StatusBanner : ObservableObject
    {
        #region Private Members

        private readonly float initialProgress = 0.0f;

        private string fullTitle;

        #endregion Private Members

        #region Public Properties

        private float progress = 0.0f;

        public float Progress
        {
            get => progress;
            set
            {
                SetProperty(ref progress, value);
            }
        }

        public bool IsProgressing { get; set; } = false;

        public string Title { get; private set; }

        public ReturnResult Status { get; private set; } = ReturnResult.InProgress;

        public FileOperationType Operation { get; private set; }

        public string Message { get; private set; }

        public SolidColorBrush StrokeColor { get; private set; } = new SolidColorBrush(Colors.DeepSkyBlue);

        public IconSource GlyphSource { get; private set; }

        public string PrimaryButtonText { get; set; }

        public string SecondaryButtonText { get; set; } = "Cancel";

        public Action PrimaryButtonClick { get; }

        public bool SolutionButtonsVisible { get; } = false;

        public string FullTitle
        {
            get => fullTitle;
            set => SetProperty(ref fullTitle, value ?? string.Empty);
        }

        #endregion Public Properties

        public StatusBanner(string message, string title, float progress, ReturnResult status, FileOperationType operation)
        {
            Message = message;
            Title = title;
            FullTitle = title;
            initialProgress = progress;
            Status = status;
            Operation = operation;

            switch (Status)
            {
                case ReturnResult.InProgress:
                    IsProgressing = true;
                    if (string.IsNullOrWhiteSpace(Title))
                    {
                        switch (Operation)
                        {
                            case FileOperationType.Extract:
                                Title = "ExtractInProgress/Title".GetLocalized();
                                GlyphSource = new FontIconSource()
                                {
                                    FontFamily = Application.Current.Resources["OldFluentUIGlyphs"] as FontFamily,
                                    Glyph = "\xEA5C"    // Extract glyph
                                };
                                break;

                            case FileOperationType.Copy:
                                Title = "CopyInProgress/Title".GetLocalized();
                                GlyphSource = new FontIconSource()
                                {
                                    Glyph = "\xE8C8"    // Copy glyph
                                };
                                break;

                            case FileOperationType.Move:
                                Title = "MoveInProgress/Title".GetLocalized();
                                GlyphSource = new FontIconSource()
                                {
                                    Glyph = "\xE77F"    // Move glyph
                                };
                                break;

                            case FileOperationType.Delete:
                                Title = "DeleteInProgress/Title".GetLocalized();
                                GlyphSource = new FontIconSource()
                                {
                                    Glyph = "\xE74D"    // Delete glyph
                                };
                                break;

                            case FileOperationType.Recycle:
                                Title = "RecycleInProgress/Title".GetLocalized();
                                GlyphSource = new FontIconSource()
                                {
                                    FontFamily = Application.Current.Resources["RecycleBinIcons"] as FontFamily,
                                    Glyph = "\xEF87"    // RecycleBin Custom Glyph
                                };
                                break;
                        }
                    }
                    FullTitle = $"{Title} ({initialProgress}%)";
                    break;

                case ReturnResult.Success:
                    IsProgressing = false;
                    if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        FullTitle = Title;
                        StrokeColor = new SolidColorBrush(Colors.Green);
                        GlyphSource = new FontIconSource()
                        {
                            Glyph = "\xE73E"    // CheckMark glyph
                        };
                    }
                    break;

                case ReturnResult.Failed:
                    IsProgressing = false;
                    if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        // Expanded banner
                        FullTitle = Title;
                        StrokeColor = new SolidColorBrush(Colors.Red);
                        GlyphSource = new FontIconSource()
                        {
                            Glyph = "\xE783"    // Error glyph
                        };
                    }
                    break;
            }
        }

        /// <summary>
        /// Post an error message banner following a failed operation
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <param name="primaryButtonText">Solution buttons are not visible if this property is an empty string</param>
        /// <param name="secondaryButtonText">Set to "Cancel" by default</param>
        public StatusBanner(string message, string title, string primaryButtonText, string secondaryButtonText, Action primaryButtonClicked)
        {
            Message = message;
            Title = title;
            PrimaryButtonText = primaryButtonText;
            SecondaryButtonText = secondaryButtonText;
            PrimaryButtonClick = primaryButtonClicked;
            Status = ReturnResult.Failed;

            if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
            {
                throw new NotImplementedException();
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(PrimaryButtonText))
                {
                    SolutionButtonsVisible = true;
                }

                // Expanded banner
                FullTitle = Title;
                StrokeColor = new SolidColorBrush(Colors.Red);
                GlyphSource = new FontIconSource()
                {
                    Glyph = "\xE783"    // Error glyph
                };
            }
        }
    }
}