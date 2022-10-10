using Windows.System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Files.App.ViewModels;
using Files.App.ServicesImplementation.Settings;

namespace Files.App.Views
{
    public sealed partial class PaneHolderControl : UserControl
    {
        private PaneHolderViewModel? viewModel => DataContext as PaneHolderViewModel;

        public PaneHolderControl()
        {
            InitializeComponent();
        }

        private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            var ctrl = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control);
            var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
            var menu = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu);

            switch (c: ctrl, s: shift, m: menu, k: args.KeyboardAccelerator.Key)
            {
                case (true, true, false, VirtualKey.Left): // ctrl + shift + "<-" select left pane
                    if (UserSettingsService.MultitaskingSettingsService.IsDualPaneEnabled)
                    {
                        viewModel.MovePaneSelection(PaneHolderViewModel.PaneSelectionDirection.Left);
                    }
                    break;

                case (true, true, false, VirtualKey.Right): // ctrl + shift + "->" select right pane
                    if (UserSettingsService.MultitaskingSettingsService.IsDualPaneEnabled)
                    {
                        viewModel.MovePaneSelection(PaneHolderViewModel.PaneSelectionDirection.Right);
                    }
                    break;

                case (true, true, false, VirtualKey.W): // ctrl + shift + "W" close right pane
                    if (UserSettingsService.MultitaskingSettingsService.IsDualPaneEnabled)
                    {
                        viewModel.CloseSelectedPane();
                    }
                    break;

                case (false, true, true, VirtualKey.Add): // alt + shift + "+" open pane
                    if (UserSettingsService.MultitaskingSettingsService.IsDualPaneEnabled)
                    {
                        viewModel.CreateNewPane();
                    }
                    break;
            }
        }
    }
}