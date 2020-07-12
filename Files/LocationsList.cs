using Files;
using System.Collections.Generic;

namespace Locations
{
    public class FavoriteLocationItem
    {
        public string ImageSource { get; set; }
        public string Icon { get; set; }
        public string Text { get; set; }
        public string Tag { get; set; }
    }

    public class ItemLoader
    {
        public static List<FavoriteLocationItem> itemsAdded = new List<FavoriteLocationItem>();

        public static void DisplayItems()
        {
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Blue.png", Icon = "\xe91c", Text = ResourceController.GetTranslation("SidebarDownloads"), Tag = "Downloads" });
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Green.png", Icon = "\xea11", Text = ResourceController.GetTranslation("SidebarDocuments"), Tag = "Documents" });
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Orange.png", Icon = "\xea83", Text = ResourceController.GetTranslation("SidebarPictures"), Tag = "Pictures" });
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Pink.png", Icon = "\xead4", Text = ResourceController.GetTranslation("SidebarMusic"), Tag = "Music" });
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Red.png", Icon = "\xec0d", Text = ResourceController.GetTranslation("SidebarVideos"), Tag = "Videos" });
        }
    }
}