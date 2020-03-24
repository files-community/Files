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
        public ItemLoader()
        {
        }
        public static List<FavoriteLocationItem> itemsAdded = new List<FavoriteLocationItem>();
        public static void DisplayItems()
        {
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Blue.png", Icon = "\xE896", Text = "Downloads", Tag = "Downloads" });
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Green.png", Icon = "\xE8A5", Text = "Documents", Tag = "Documents" });
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Orange.png", Icon = "\xEB9F", Text = "Pictures", Tag = "Pictures" });
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Pink.png", Icon = "\xEC4F", Text = "Music", Tag = "Music"});
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Red.png", Icon = "\xE8B2", Text = "Videos", Tag = "Videos" });
        }
    }
}