using System.Collections.Generic;

namespace Locations
{
    public class FavoriteLocationItem
    {
        public string ImageSource { get; set; }
        public string Icon { get; set; }
        public string Text { get; set; }
        public string Tag { get; set; }
        //public string DominantImageColor { get; set; }
    }

    public class ItemLoader
    {
        public static List<FavoriteLocationItem> itemsAdded = new List<FavoriteLocationItem>();
        public static void DisplayItems()
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Blue.png", Icon = "\xE896", Text = resourceLoader.GetString("SidebarDownloads"), Tag = "Downloads" });
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Green.png", Icon = "\xE8A5", Text = resourceLoader.GetString("SidebarDocuments"), Tag = "Documents" });
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Orange.png", Icon = "\xEB9F", Text = resourceLoader.GetString("SidebarPictures"), Tag = "Pictures" });
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Pink.png", Icon = "\xEC4F", Text = resourceLoader.GetString("SidebarMusic"), Tag = "Music" });
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Red.png", Icon = "\xE8B2", Text = resourceLoader.GetString("SidebarVideos"), Tag = "Videos" });
        }
    }
}