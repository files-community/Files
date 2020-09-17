using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
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
        public PostedStatusBanner PostBanner(string title, string message, uint initialProgress, StatusBanner.StatusBannerSeverity severity, StatusBanner.StatusBannerOperation operation)
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
        public Progress<uint> Progress;
        public PostedStatusBanner(StatusBanner bannerArg)
        {
            Banner = bannerArg;
            Progress = new Progress<uint>(ReportProgressToBanner);
        }

        private void ReportProgressToBanner(uint value)
        {
            if (value <= 100)
            {
                Banner.FullTitle = Banner.Title + " (" + value + "%)";
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
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
        private uint InitialProgress = 0;
        private string _FullTitle;
        public bool IsProgressing { get; } = false;
        public string Title { get; }
        public StatusBannerSeverity Severity { get; } = StatusBannerSeverity.Ongoing;
        public StatusBannerOperation Operation { get; }
        public string Message { get; }
        public SolidColorBrush StrokeColor { get; } = new SolidColorBrush(Colors.DeepSkyBlue);
        public IconSource GlyphSource { get; }
        public int BannerHeight { get; set; } = 55;
        public string PrimaryButtonText { get; set; }
        public string SecondaryButtonText { get; set; } = "Cancel";
        public Action PrimaryButtonClick { get; }
        public bool SolutionButtonsVisible { get; } = false;

        public string FullTitle
        {
            get => _FullTitle;
            set => SetProperty(ref _FullTitle, value);
        }

        public enum StatusBannerSeverity
        {
            Ongoing,
            Success,
            Error
        }

        public enum StatusBannerOperation
        {
            Recycle,
            Delete,
            Paste,
            Extract
        }

        public StatusBanner(string message, string title, uint progress, StatusBannerSeverity severity, StatusBannerOperation operation)
        {
            Message = message;
            Title = title;
            InitialProgress = progress;
            Severity = severity;
            Operation = operation;

            switch (Severity)
            {
                case StatusBannerSeverity.Ongoing:
                    IsProgressing = true;
                    if (string.IsNullOrWhiteSpace(Title))
                    {
                        switch (Operation)
                        {
                            case StatusBannerOperation.Extract:
                                Title = ResourceController.GetTranslation("ExtractInProgress/Title");
                                GlyphSource = new FontIconSource()
                                {
                                    FontFamily = Application.Current.Resources["FluentUIGlyphs"] as FontFamily,
                                    Glyph = "\xEA5C"    // Extract glyph
                                };
                                break;

                            case StatusBannerOperation.Paste:
                                Title = ResourceController.GetTranslation("PasteInProgress/Title");
                                GlyphSource = new FontIconSource()
                                {
                                    FontFamily = Application.Current.Resources["FluentUIGlyphs"] as FontFamily,
                                    Glyph = "\xE9B2"    // Paste glyph
                                };
                                break;

                            case StatusBannerOperation.Delete:
                                Title = ResourceController.GetTranslation("DeleteInProgress/Title");
                                GlyphSource = new FontIconSource()
                                {
                                    FontFamily = Application.Current.Resources["FluentUIGlyphs"] as FontFamily,
                                    Glyph = "\xE9EE"    // Delete glyph
                                };
                                break;

                            case StatusBannerOperation.Recycle:
                                Title = ResourceController.GetTranslation("RecycleInProgress/Title");
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

                case StatusBannerSeverity.Success:
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

                case StatusBannerSeverity.Error:
                    if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
                    {
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
            Severity = StatusBannerSeverity.Error;

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