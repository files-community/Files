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
    public sealed partial class StatusCenter : UserControl
    {
        public static ObservableCollection<StatusBanner> StatusBannersSource { get; set; } = new ObservableCollection<StatusBanner>();

        public StatusCenter()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Posts a new banner to the Status Center control for an operation.
        /// It may be used to return the progress, success, or failure of the respective operation.
        /// </summary>
        /// <param name="title">Reserved for success and error banners. Otherwise, pass an empty string for this argument.</param>
        /// <param name="message"></param>
        /// <param name="initialProgress"></param>
        /// <param name="severity"></param>
        /// <param name="operation"></param>
        /// <returns>A StatusBanner object which may be used to track/update the progress of an operation.</returns>
        public PostedStatusBanner PostBanner(string title, string message, float initialProgress, Status severity, FileOperationType operation)
        {
            var item = new StatusBanner(message, title, initialProgress, severity, operation);
            StatusBannersSource.Add(item);
            return new PostedStatusBanner(item);
        }

        /// <summary>
        /// Posts a new banner with expanded height to the Status Center control. This is typically
        /// used to represent a failure during a prior operation which must be acted upon.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="primaryButtonText"></param>
        /// <param name="cancelButtonText"></param>
        /// <param name="primaryAction"></param>
        /// <returns>A StatusBanner object which may be used to automatically remove the banner from UI.</returns>
        public PostedStatusBanner PostActionBanner(string title, string message, string primaryButtonText, string cancelButtonText, Action primaryAction)
        {
            var item = new StatusBanner(message, title, primaryButtonText, cancelButtonText, primaryAction);
            StatusBannersSource.Add(item);
            return new PostedStatusBanner(item);
        }

        // Dismiss banner button event handler
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var itemToDismiss = (sender as Button).DataContext as StatusBanner;
            StatusBannersSource.Remove(itemToDismiss);
        }

        // Primary action button click
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var itemToDismiss = (sender as Button).DataContext as StatusBanner;
            await Task.Run(itemToDismiss.PrimaryButtonClick);
            StatusBannersSource.Remove(itemToDismiss);
        }
    }

    public class PostedStatusBanner
    {
        internal StatusBanner Banner;
        public Progress<float> Progress;
        public Progress<Status> Status;

        public PostedStatusBanner(StatusBanner bannerArg)
        {
            Banner = bannerArg;
            Progress = new Progress<float>(ReportProgressToBanner);
            Status = new Progress<Status>(ReportProgressToBanner);
        }

        private void ReportProgressToBanner(float value)
        {
            if (value <= 100.0f)
            {
                Banner.FullTitle = Banner.Title + " (" + value + "%)";
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        private void ReportProgressToBanner(Status status)
        {

        }

        public void Remove()
        {
            if (StatusCenter.StatusBannersSource.Contains(Banner))
            {
                StatusCenter.StatusBannersSource.Remove(Banner);
            }
        }
    }

    public class StatusBanner : ObservableObject
    {
        #region Private Members

        private readonly float InitialProgress = 0f;

        private string _FullTitle;

        #endregion

        #region Public Properties

        public bool InProgress { get; private set; } = false;

        public string Title { get; private set; }

        public Status Status { get; private set; } = Status.InProgress;

        public FileOperationType Operation { get; private set; }

        public string Message { get; private set; }

        public SolidColorBrush StrokeColor { get; private set; } = new SolidColorBrush(Colors.DeepSkyBlue);

        public IconSource GlyphSource { get; private set; }

        public int BannerHeight { get; set; } = 55;

        public string PrimaryButtonText { get; set; }

        public string SecondaryButtonText { get; set; } = "Cancel";

        public Action PrimaryButtonClick { get; private set; }

        public bool SolutionButtonsVisible { get; private set; } = false;

        public string FullTitle
        {
            get => _FullTitle;
            set => SetProperty(ref _FullTitle, value);
        }

        #endregion

        public StatusBanner(string message, string title, float progress, Status status, FileOperationType operation)
        {
            Message = message;
            Title = title;
            InitialProgress = progress;
            Status = status;
            Operation = operation;

            switch (Status)
            {
                case Status.InProgress:
                    InProgress = true;
                    if (string.IsNullOrWhiteSpace(Title))
                    {
                        switch (Operation)
                        {
                            case FileOperationType.Extract:
                                Title = "ExtractInProgress/Title".GetLocalized();
                                GlyphSource = new FontIconSource()
                                {
                                    FontFamily = Application.Current.Resources["FluentUIGlyphs"] as FontFamily,
                                    Glyph = "\xEA5C"    // Extract glyph
                                };
                                break;

                            case FileOperationType.Copy:
                                Title = "CopyInProgress/Title".GetLocalized();
                                GlyphSource = new FontIconSource()
                                {
                                    FontFamily = Application.Current.Resources["FluentUIGlyphs"] as FontFamily,
                                    Glyph = "\xE9B2"    // Paste glyph
                                };
                                break;

                            case FileOperationType.Move:
                                Title = "MoveInProgress/Title".GetLocalized();
                                GlyphSource = new FontIconSource()
                                {
                                    FontFamily = Application.Current.Resources["FluentUIGlyphs"] as FontFamily,
                                    Glyph = "\xE9B2"    // Paste glyph
                                };
                                break;

                            case FileOperationType.Delete:
                                Title = "DeleteInProgress/Title".GetLocalized();
                                GlyphSource = new FontIconSource()
                                {
                                    FontFamily = Application.Current.Resources["FluentUIGlyphs"] as FontFamily,
                                    Glyph = "\xE9EE"    // Delete glyph
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
                    FullTitle = Title + " (" + InitialProgress + "%)";
                    break;

                case Status.Success:
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
                            FontFamily = Application.Current.Resources["FluentUIGlyphs"] as FontFamily,
                            Glyph = "\xE9A1"    // CheckMark glyph
                        };
                    }
                    break;

                case Status.Failed | Status.NullException | Status.IntegrityCheckFailed | Status.IllegalArgumentException | Status.UnknownException | Status.AccessUnauthorized:
                    if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
                    {
                        Debugger.Break();
                        throw new NotImplementedException();
                    }
                    else
                    {
                        // Expanded banner
                        BannerHeight = 70;
                        FullTitle = Title;
                        StrokeColor = new SolidColorBrush(Colors.Red);
                        GlyphSource = new FontIconSource()
                        {
                            FontFamily = Application.Current.Resources["FluentUIGlyphs"] as FontFamily,
                            Glyph = "\xEA41"    // Error glyph
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
            Status = Status.Failed;

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
                BannerHeight = 70;
                FullTitle = Title;
                StrokeColor = new SolidColorBrush(Colors.Red);
                GlyphSource = new FontIconSource()
                {
                    FontFamily = Application.Current.Resources["FluentUIGlyphs"] as FontFamily,
                    Glyph = "\xEA41"    // Error glyph
                };
            }
        }
    }
}