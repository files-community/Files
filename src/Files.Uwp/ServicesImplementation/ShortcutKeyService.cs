using CommunityToolkit.Mvvm.ComponentModel;
using Files.Backend.Services;

namespace Files.Uwp.ServicesImplementation
{
    public class ShortcutKeyService : ObservableObject, IShortcutKeyService
    {
        private bool _canInvokeShortcutKeys = true;

        public bool CanInvokeShortcutKeys
        {
            get => _canInvokeShortcutKeys;
            set => SetProperty(ref _canInvokeShortcutKeys, value);
        }
    }
}
