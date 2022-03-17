using Windows.UI;

namespace Files.Dialogs
{
    public interface IDialogWithUIContext
    {
        public UIContext Context { get; set; }
    }
}