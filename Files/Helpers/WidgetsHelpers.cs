using Files.UserControls.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Helpers
{
    public static class WidgetsHelpers
    {
        public static LibraryCards GetLibraryCards()
        {
            if (App.AppSettings.ShowLibraryCardsWidget)
            {
                return new LibraryCards();
            }
            else
            {
                return null;
            }
        }

        public static DrivesWidget GetDrivesWidget()
        {
            if (App.AppSettings.ShowDrivesWidget)
            {
                return new DrivesWidget();
            }
            else
            {
                return null;
            }
        }

        public static Bundles GetBundles()
        {
            if (App.AppSettings.ShowBundlesWidget)
            {
                return new Bundles();
            }
            else
            {
                return null;
            }
        }

        public static RecentFiles GetRecentFiles()
        {
            if (App.AppSettings.ShowBundlesWidget)
            {
                return new RecentFiles();
            }
            else
            {
                return null;
            }
        }
    }
}
