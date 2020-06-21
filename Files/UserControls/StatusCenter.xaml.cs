using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class StatusCenter : UserControl
    {
        public StatusCenter()
        {
            this.InitializeComponent();
        }
    }

    public class StatusBanner : ViewModelBase
    {
        public string Title { get; set; }
        public StatusBannerSeverity Severity { get; set; } = StatusBannerSeverity.Ongoing;
        public StatusBannerOperation Operation { get; set; }
        public string Message { get; set; }
        private uint _Progress = 0;
        public uint Progress
        {
            get => _Progress;
            set => Set(ref _Progress, value);
        }

        private Color StrokeColor { get; set; } = Colors.DeepSkyBlue;
        private IconSource Glyph { get; set; }
        private bool IsProgressing = false;

        private string _FullTitle;
        public string FullTitle
        {
            get => _FullTitle;
            set => Set(ref _FullTitle, value);
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

        public StatusBanner()
        {
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
                                Glyph = new FontIconSource()
                                {
                                    Glyph = "\uED25"    // OpenFolder to substitute for extract glyph
                                };
                                break;
                            case StatusBannerOperation.Paste:
                                Title = ResourceController.GetTranslation("PasteInProgress/Title");
                                Glyph = new FontIconSource()
                                {
                                    Glyph = "\uE77F"    // Paste glyph
                                };
                                break;
                            case StatusBannerOperation.Delete:
                                Title = ResourceController.GetTranslation("DeleteInProgress/Title");
                                Glyph = new FontIconSource()
                                {
                                    Glyph = "\uE74D"    // Delete glyph
                                };
                                break;
                            case StatusBannerOperation.Recycle:
                                Title = ResourceController.GetTranslation("RecycleInProgress/Title");
                                Glyph = new FontIconSource()
                                {
                                    //Glyph = "\uED25"    // OpenFolder to substitute for extract glyph
                                };
                                break;
                        }
                    }
                    
                    break;
                case StatusBannerSeverity.Success:
                    if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        FullTitle = Title;
                        StrokeColor = Colors.Green;
                        Glyph = new FontIconSource()
                        {
                            Glyph = "\uE73E"    // CheckMark glyph
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
                        FullTitle = Title;
                        StrokeColor = Colors.Red;
                        Glyph = new FontIconSource()
                        {
                            Glyph = "\uE783"    // Error glyph
                        };
                    }
                    break;
            }
        }
    }
}
