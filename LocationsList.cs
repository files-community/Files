using System.Collections.Generic;

namespace Locations
{
    public class LocationItem
    {
        public string ImageSource { get; set; }
        public string Icon { get; set; }
        public string Text { get; set; }
        //public string DominantImageColor { get; set; }
    }

    public class ItemLoader
    {
        public ItemLoader()
        {
        }
        public static List<LocationItem> itemsAdded = new List<LocationItem>();
        public static void DisplayItems()
        {
            itemsAdded.Add(new LocationItem() { ImageSource = "Assets/Cards/Gradients/Blue.png", Icon = "\xE896", Text = "Downloads"});
            itemsAdded.Add(new LocationItem() { ImageSource = "Assets/Cards/Gradients/Green.png", Icon = "\xE8A5", Text = "Documents"});
            itemsAdded.Add(new LocationItem() { ImageSource = "Assets/Cards/Gradients/Orange.png", Icon = "\xEB9F", Text = "Pictures" });
            itemsAdded.Add(new LocationItem() { ImageSource = "Assets/Cards/Gradients/Pink.png", Icon = "\xEC4F", Text = "Music" });
            itemsAdded.Add(new LocationItem() { ImageSource = "Assets/Cards/Gradients/Red.png", Icon = "\xE8B2", Text = "Videos" });
        }
    }
}