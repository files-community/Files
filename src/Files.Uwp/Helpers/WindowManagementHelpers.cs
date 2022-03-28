using System;
using System.Linq;
using Windows.UI;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace Files.Uwp.Helpers
{
    public static class WindowManagementHelpers
    {
        /// <summary>
        /// Gets the root content page for an app window by XamlRoot.
        /// </summary>
        /// <param name="element">A UIElement placed on an ApplicationView or AppWindow</param>
        /// <returns>Root content page node from the window construct it's placed on</returns>
        public static UIElement GetWindowContentFromUIElement(UIElement element)
        {
            return (element.XamlRoot.Content as Frame)?.Content as UIElement;
        }

        /// <summary>
        /// Gets the root content page from an AppWindow element.
        /// </summary>
        /// <param name="window">An instance of AppWindow</param>
        /// <returns>Root content page node from the AppWindow it's placed on</returns>
        public static UIElement GetWindowContentFromAppWindow(AppWindow window)
        {
            return (ElementCompositionPreview.GetAppWindowContent(window) as Frame)?.Content as UIElement;
        }

        /// <summary>
        /// Gets the window construct associated with a supplied UIContext.
        /// </summary>
        /// <param name="context">The indentifying currency from an app window or view</param>
        /// <returns>An object that must be casted to the expected window construct type (either Window or AppWindow)</returns>
        public static object GetWindowFromUIContext(UIContext context)
        {
            if (App.AppWindows.ContainsKey(context))
            {
                return App.AppWindows[context];
            }
            else if (Window.Current?.Content != null)
            {
                return Window.Current;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the root content page for an app window from a UIContext identifier.
        /// </summary>
        /// <param name="context">The indentifying currency from an app window or view</param>
        /// <returns>Root content page node from the window construct it's placed on</returns>
        public static UIElement GetWindowContentFromUIContext(UIContext context)
        {
            var window = GetWindowFromUIContext(context);
            if (window is AppWindow aw)
            {
                return GetWindowContentFromAppWindow(aw);
            }
            else if (window is Window w)
            {
                return (w.Content as Frame)?.Content as UIElement;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Callers of this method should be aware that it will
        /// attempt to get the primary Window instance, but may
        /// instead return the first AppWindow available; otherwise 
        /// null.
        /// </summary>
        /// <returns>An object that must be casted to the expected window construct type (either Window or AppWindow)</returns>
        [Obsolete("This method will be removed soon. Present callers should decouple their UI layer to leverage other helper methods that infer a window construct's specific instance", false)]
        public static object GetAnyWindow()
        {
            if (Window.Current?.Content != null)
            {
                return Window.Current;
            }
            else if (App.AppWindows.Any())
            {
                return App.AppWindows.FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Callers of this method should be aware that it will
        /// attempt to get root content page from the primary Window 
        /// instance, but may instead return root content page from 
        /// the first AppWindow available; otherwise null.
        /// </summary>
        /// <returns>Root content page node from the window construct it's placed on</returns>
        [Obsolete("This method will be removed soon. Present callers should decouple their UI layer to leverage other helper methods that infer a window construct's specific instance", false)]
        public static UIElement GetWindowContent()
        {
            if (Window.Current?.Content != null)
            {
                return (Window.Current.Content as Frame)?.Content as UIElement;
            }
            else if (App.AppWindows.Any())
            {
                return GetWindowContentFromAppWindow(App.AppWindows.Values.FirstOrDefault());
            }
            else
            {
                return null;
            }
        }
    }
}
