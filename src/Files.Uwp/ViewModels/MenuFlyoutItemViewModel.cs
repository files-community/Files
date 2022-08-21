using CommunityToolkit.Mvvm.Input;
using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Uwp.ViewModels
{
    #region Interface

    // Could be merged with ContextMenuFlyoutItemViewModel?
    public interface IMenuFlyoutItemViewModel
    {
        public MenuFlyoutItemBase Build();
    }

    #endregion

    #region Basic Items

    public class MenuFlyoutSeparatorViewModel : IMenuFlyoutItemViewModel
    {
        public MenuFlyoutItemBase Build() => new MenuFlyoutSeparator();
    }

    public class MenuFlyoutItemViewModel : IMenuFlyoutItemViewModel
    {
        public string Text { get; init; }

        public IRelayCommand<string> OnSelect { get; init; }

        public bool IsEnabled { get; set; } = true;

        public virtual MenuFlyoutItemBase Build()
        {
            var mfi = new MenuFlyoutItem
            {
                Text = this.Text,
                Command = this.OnSelect,
                IsEnabled = this.IsEnabled,
            };
            return mfi;
        }
    }

    public class MenuFlyoutSubItemViewModel : MenuFlyoutItemViewModel
    {
        public IList<IMenuFlyoutItemViewModel> Items { get; init; } = new List<IMenuFlyoutItemViewModel>();

        public override MenuFlyoutItemBase Build()
        {
            var mfsi = new MenuFlyoutSubItem
            {
                Text = this.Text,
                IsEnabled = this.IsEnabled && this.Items.Count > 0,
            };
            this.Items.ForEach(item => mfsi.Items.Add(item.Build()));
            return mfsi;
        }
    }

    public class MenuFlyoutFactoryItemViewModel : IMenuFlyoutItemViewModel
    {
        public Func<MenuFlyoutItemBase> Factory { get; init; }

        public MenuFlyoutItemBase Build() => Factory();
    }

    public class MenuFlyoutTemplateItemViewModel : IMenuFlyoutItemViewModel
    {
        public object DataContext { get; init; }

        public DataTemplate Template { get; init; }

        public MenuFlyoutItemBase Build()
        {
            // Throw error if template is null or not derived from MenuFlyoutItemBase
            var mfci = (MenuFlyoutItemBase)Template.LoadContent();
            mfci.DataContext = this.DataContext;
            return mfci;
        }
    }

    #endregion

    #region Specialized Items

    public class MenuFlyoutItemPathViewModel : MenuFlyoutItemViewModel
    {
        public string Path { get; init; }

        public override MenuFlyoutItemBase Build()
        {
            var mfi = new MenuFlyoutItem
            {
                Text = this.Text,
                Command = this.OnSelect,
                CommandParameter = this.Path,
                IsEnabled = this.IsEnabled,
            };
            if (!string.IsNullOrEmpty(this.Path))
            {
                ToolTipService.SetToolTip(mfi, this.Path);
            }
            return mfi;
        }
    }

    #endregion
}
