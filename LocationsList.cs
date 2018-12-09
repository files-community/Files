

using System;
using System.Collections.Generic;

namespace Locations
{
    public class LocationItem
    {
        public string ImageSource { get; set; }
        public string Icon { get; set; }
        public string Text { get; set; }
    }

    public class ItemLoader
    {
        
        public ItemLoader()
        {

        }

        public static List<LocationItem> itemsAdded = new List<LocationItem>();
        public static void DisplayItems()
        {
            itemsAdded.Add(new LocationItem() { ImageSource = "Assets/Cards/downloads.jpg", Icon = "&#xE896;", Text = "Downloads"});
            itemsAdded.Add(new LocationItem() { ImageSource = "Assets/Cards/documents.jpg", Icon = "&#xE8A5;", Text = "Documents"});
            itemsAdded.Add(new LocationItem() { ImageSource = "Assets/Cards/pictures.jpg", Icon = "&#xE8A5;", Text = "Pictures" });
            itemsAdded.Add(new LocationItem() { ImageSource = "Assets/Cards/music.jpg", Icon = "&#xE8A5;", Text = "Music" });
            itemsAdded.Add(new LocationItem() { ImageSource = "Assets/Cards/videos.jpg", Icon = "&#xE8A5;", Text = "Videos" });




        }
    }
}