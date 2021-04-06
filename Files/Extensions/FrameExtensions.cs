using Files.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Files.Extensions
{
    public static class FrameExtensions
    {
        /// <summary>
        /// Wrapper function for <see cref="Frame.Navigate(Type, object)"/> that calls <see cref="IDisposable.Dispose"/> for current content page type
        /// </summary>
        /// <param name="contentFrame"></param>
        /// <param name="pageType"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static bool SafeNavigate(this Frame contentFrame, Type pageType, object parameter)
        {
            return SafeNavigate(contentFrame, pageType, parameter, new SuppressNavigationTransitionInfo());
        }

        /// <summary>
        /// Wrapper function for <see cref="Frame.Navigate(Type, object, NavigationTransitionInfo)"/> that calls <see cref="IDisposable.Dispose"/> for current content page type
        /// </summary>
        /// <param name="contentFrame"></param>
        /// <param name="pageType"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static bool SafeNavigate(this Frame contentFrame, Type pageType, object parameter, NavigationTransitionInfo transitionInfo)
        {
            if (MustDispose(contentFrame.Content?.GetType(), pageType))
            {
                (contentFrame.Content as IDisposable)?.Dispose();
            }

            return contentFrame.Navigate(pageType, parameter, transitionInfo);
        }

        public static bool MustDispose(Type currentContent, Type incomingContent)
        {
            return currentContent == typeof(YourHome) && currentContent != incomingContent;
            //return currentContent != incomingContent;
        }
    }
}
