using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.UserControls;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.Views.LayoutModes
{
    public sealed partial class ColumnViewBrowser : BaseLayout
    {
        public ColumnViewBrowser() : base()
        {
            this.InitializeComponent();
        }

        protected override void HookEvents()
        {
        }

        protected override void UnhookEvents()
        {
        }

        protected override ListedItem GetItemFromElement(object element)
        {
            return null;
        }

        private void ColumnViewBase_ItemInvoked(object sender, EventArgs e)
        {
            var column = sender as ColumnParam;
            if (column.ListView.FindAscendant<ColumnViewBrowser>() != this)
            {
                return;
            }

            DismissOtherBlades(column.ListView);

            var frame = new Frame();
            frame.Navigated += Frame_Navigated;
            var newblade = new BladeItem();
            newblade.Content = frame;
            ColumnHost.Items.Add(newblade);

            frame.Navigate(typeof(ColumnShellPage), new ColumnParam
            {
                Column = ColumnHost.ActiveBlades.IndexOf(newblade),
                Path = column.Path
            });
        }

        private void ContentChanged(IShellPage p)
        {
            (ParentShellPageInstance as ModernShellPage)?.RaiseContentChanged(p, p.TabItemArguments);
            if (ColumnHost.ActiveBlades != null)
            {
                ColumnHost.ActiveBlades.ForEach(x =>
                {
                    var shellPage = (x.Content as Frame)?.Content as ColumnShellPage;
                    shellPage.IsCurrentInstance = ParentShellPageInstance.IsCurrentInstance && (shellPage == LastColumnShellPage);
                });
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);

            var navigationArguments = (NavigationArguments)eventArgs.Parameter;
            MainPageFrame.Navigated += Frame_Navigated;
            MainPageFrame.Navigate(typeof(ColumnShellPage), new ColumnParam
            {
                Column = 0,
                Path = navigationArguments.NavPathParam
            });
        }

        protected override void InitializeCommandsViewModel()
        {
            CommandsViewModel = new BaseLayoutCommandsViewModel(new BaseLayoutCommandImplementationModel(ParentShellPageInstance, ItemManipulationModel));
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
        }

        #region IDisposable

        public override void Dispose()
        {
            base.Dispose();
            ColumnHost.ActiveBlades.ForEach(x => (((x.Content as Frame)?.Content as ColumnShellPage).SlimContentPage as ColumnViewBase).ItemInvoked -= ColumnViewBase_ItemInvoked);
            ColumnHost.ActiveBlades.Select(x => (x.Content as Frame)?.Content).OfType<IDisposable>().ForEach(x => x.Dispose());
            UnhookEvents();
            CommandsViewModel?.Dispose();
        }

        #endregion IDisposable

        private void DismissOtherBlades(ListView listView)
        {
            DismissOtherBlades(listView.FindAscendant<BladeItem>());
        }

        private void DismissOtherBlades(BladeItem blade)
        {
            var index = ColumnHost.ActiveBlades.IndexOf(blade);
            if (index >= 0)
            {
                Common.Extensions.IgnoreExceptions(() =>
                {
                    while (ColumnHost.ActiveBlades.Count > index + 1)
                    {
                        if ((ColumnHost.ActiveBlades[index + 1].Content as Frame)?.Content is IDisposable disposableContent)
                        {
                            disposableContent.Dispose();
                        }
                        (((ColumnHost.ActiveBlades[index + 1].Content as Frame).Content as ColumnShellPage).SlimContentPage as ColumnViewBase).ItemInvoked -= ColumnViewBase_ItemInvoked;
                        ColumnHost.Items.RemoveAt(index + 1);
                        ColumnHost.ActiveBlades.RemoveAt(index + 1);
                    }
                });
            }
            ContentChanged(LastColumnShellPage);
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            var f = sender as Frame;
            f.Navigated -= Frame_Navigated;
            (f.Content as IShellPage).ContentChanged += ColumnViewBrowser_ContentChanged;
        }

        private void ColumnViewBrowser_ContentChanged(object sender, UserControls.MultitaskingControl.TabItemArguments e)
        {
            var c = sender as IShellPage;
            c.ContentChanged -= ColumnViewBrowser_ContentChanged;
            (c.SlimContentPage as ColumnViewBase).ItemInvoked -= ColumnViewBase_ItemInvoked;
            (c.SlimContentPage as ColumnViewBase).ItemInvoked += ColumnViewBase_ItemInvoked;
            ContentChanged(c);
        }

        public void UpColumn()
        {
            if (!IsLastColumnBase)
            {
                DismissOtherBlades(ColumnHost.ActiveBlades[ColumnHost.ActiveBlades.Count - 2]);
            }
            else
            {
                (ParentShellPageInstance as ModernShellPage)?.Up_Click();
            }
        }

        public void SetSelectedPathOrNavigate(PathNavigationEventArgs e)
        {
            if (!IsLastColumnBase)
            {
                foreach (var item in ColumnHost.ActiveBlades)
                {
                    if ((item.Content as Frame)?.Content is ColumnShellPage s &&
                        PathNormalization.NormalizePath(s.FilesystemViewModel.WorkingDirectory) ==
                        PathNormalization.NormalizePath(e.ItemPath))
                    {
                        DismissOtherBlades(item);
                        return;
                    }
                }
            }
            if (PathNormalization.NormalizePath(ParentShellPageInstance.FilesystemViewModel.WorkingDirectory) !=
                PathNormalization.NormalizePath(e.ItemPath))
            {
                ParentShellPageInstance.NavigateToPath(e.ItemPath);
            }
            else
            {
                DismissOtherBlades(ColumnHost.ActiveBlades[0]);
            }
        }

        public IShellPage LastColumnShellPage => (ColumnHost.ActiveBlades?.Last().Content as Frame)?.Content as ColumnShellPage ?? ParentShellPageInstance;

        public bool IsLastColumnBase => ColumnHost.ActiveBlades?.Count == 1;
    }
}