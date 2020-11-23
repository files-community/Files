using Files.Converters;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Files.Filesystem
{
    /// <summary>
    /// This class is used to represent a file property from the Windows.Storage API
    /// </summary>
    public class FileProperty : ObservableObject
    {
        /// <summary>
        /// This property defines the resource to use to get the name from string resources
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The string resource of the property name
        /// </summary>
        public string NameResource { get; set; }
        public string Property { get; set; }
        public string Section { get; set; }
        public string SectionResource { get; set; }
        public object Value { get; set; }
        public IValueConverter Converter { get; set; }
        public bool IsReadOnly { get; set; } = true;
        /// <summary>
        /// If the property is hidden, it is only shown when the user presses show all
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// If a property has an action to run on a button press, eg "Open in maps", define it's action here
        /// </summary>
        public Button ActionButton { get; set; }

        /// <summary>
        /// Should be used in instances where a property does not have a "Property" value, but needs to be idenitfiable in a list of properties
        /// </summary>
        public string ID { get; set; }

        public Visibility Visibility { get; set; } = Visibility.Visible;

        public FileProperty()
        {

        }


        /// <summary>
        /// Use this function when taking a file property from the base list and adding it to the view model list.
        /// </summary>
        /// <param name="fileProperty"></param>
        /// <returns></returns>
        public async Task InitializeProperty(StorageFile file)
        {
            if(!string.IsNullOrEmpty(Property))
               await SetValueFromFile(file);

            InitializeNames();
        }

        /// <summary>
        /// Sets the Value property
        /// </summary>
        /// <param name="file">The file to get the property of</param>
        public async Task SetValueFromFile(StorageFile file)
        {
            var props = await file.Properties.RetrievePropertiesAsync(new List<string>() { Property });
            Value = props[Property];
            UpdateConverter();
        }

        /// <summary>
        /// Call this function just after getting properties to set the property's converter based on the value type
        /// </summary>
        private void UpdateConverter()
        {
            if (Value is UInt32)
                Converter = new UInt32ToString();

            if (Value is Double)
                Converter = new DoubleToString();

            if (Value is DateTimeOffset)
                Converter = new DateTimeOffsetToString();

            if (Value is string[])
                Converter = new StringArrayToString();
        }


        public void InitializeNames()
        {
            Name = ResourceController.GetTranslation(NameResource);
            Section = ResourceController.GetTranslation(SectionResource);
        }

        public void SaveProperty()
        {

        }

        public static readonly List<FileProperty> PropertyListItemsBase = new List<FileProperty>()
        {
            new FileProperty() { NameResource = "Address", SectionResource = "PropertySectionGPS", ID = "address" },

            new FileProperty() { Property = "System.RatingText", NameResource = "PropertyRatingText", SectionResource = "PropertySectionCore"},
            new FileProperty() { Property = "System.ItemFolderPathDisplay", NameResource = "PropertyItemFolderPathDisplay", SectionResource = "PropertySectionCore"},
            new FileProperty() { Property = "System.ItemTypeText", NameResource = "PropertyItemTypeText", SectionResource = "PropertySectionCore"},
            new FileProperty() { Property = "System.Title", NameResource = "PropertyTitle", SectionResource = "PropertySectionCore", IsReadOnly = false},
            new FileProperty() { Property = "System.Subject", NameResource = "PropertySubject", SectionResource = "PropertySectionCore", IsReadOnly = false},
            new FileProperty() { Property = "System.Comment", NameResource = "PropertyComment", SectionResource = "PropertySectionCore"},
            new FileProperty() { Property = "System.Copyright", NameResource = "PropertyCopyright", SectionResource = "PropertySectionCore"},
            new FileProperty() { Property = "System.DateCreated", NameResource = "PropertyDateCreated", SectionResource = "PropertySectionCore"},
            new FileProperty() { Property = "System.DateModified", NameResource = "PropertyDateModified", SectionResource = "PropertySectionCore"},
            new FileProperty() { Property = "System.Image.CompressedBitsPerPixel", NameResource = "PropertyCompressedBitsPerPixel", SectionResource = "PropertySectionImage"},
            new FileProperty() { Property = "System.Image.BitDepth", NameResource = "PropertyBitDepth", SectionResource = "PropertySectionImage"},
            new FileProperty() { Property = "System.Image.Dimensions", NameResource = "PropertyDimensions", SectionResource = "PropertySectionImage"},
            new FileProperty() { Property = "System.Image.HorizontalResolution", NameResource = "PropertyHorizontalResolution", SectionResource = "PropertySectionImage"},
            new FileProperty() { Property = "System.Image.VerticalResolution", NameResource = "PropertyVerticalResolution", SectionResource = "PropertySectionImage"},
            new FileProperty() { Property = "System.Image.CompressionText", NameResource = "PropertyCompressionText", SectionResource = "PropertySectionImage"},
            new FileProperty() { Property = "System.Image.ResolutionUnit", NameResource = "PropertyResolutionUnit", SectionResource = "PropertySectionImage"},
            new FileProperty() { Property = "System.Image.HorizontalSize", NameResource = "PropertyHorizontalSize", SectionResource = "PropertySectionImage"},
            new FileProperty() { Property = "System.Image.VerticalSize", NameResource = "PropertyVerticalSize", SectionResource = "PropertySectionImage"},
            new FileProperty() { Property = "System.GPS.Latitude", NameResource = "PropertyLatitude", SectionResource = "PropertySectionGPS"},
            new FileProperty() { Property = "System.GPS.LatitudeDecimal", NameResource = "PropertyLatitudeDecimal", SectionResource = "PropertySectionGPS"},
            new FileProperty() { Property = "System.GPS.LatitudeRef", NameResource = "PropertyLatitudeRef", SectionResource = "PropertySectionGPS"},
            new FileProperty() { Property = "System.GPS.Longitude", NameResource = "PropertyLongitude", SectionResource = "PropertySectionGPS"},
            new FileProperty() { Property = "System.GPS.LongitudeDecimal", NameResource = "PropertyLongitudeDecimal", SectionResource = "PropertySectionGPS"},
            new FileProperty() { Property = "System.GPS.LongitudeRef", NameResource = "PropertyLongitudeRef", SectionResource = "PropertySectionGPS"},
            new FileProperty() { Property = "System.GPS.Altitude", NameResource = "PropertyAltitude", SectionResource = "PropertySectionGPS", IsReadOnly = false},
            new FileProperty() { Property = "System.Photo.CameraManufacturer", NameResource = "PropertyCameraManufacturer", SectionResource = "PropertySectionPhoto"},
            new FileProperty() { Property = "System.Photo.CameraModel", NameResource = "PropertyCameraModel", SectionResource = "PropertySectionPhoto"},
            new FileProperty() { Property = "System.Photo.ExposureTime", NameResource = "PropertyExposureTime", SectionResource = "PropertySectionPhoto"},
            new FileProperty() { Property = "System.Photo.FocalLength", NameResource = "PropertyFocalLength", SectionResource = "PropertySectionPhoto"},
            new FileProperty() { Property = "System.Photo.Aperture", NameResource = "PropertyAperture", SectionResource = "PropertySectionPhoto"},
            new FileProperty() { Property = "System.Photo.DateTaken", NameResource = "PropertyDateTaken", SectionResource = "PropertySectionPhoto"},
            new FileProperty() { Property = "System.Audio.ChannelCount", NameResource = "PropertyChannelCount", SectionResource = "PropertySectionAudio"},
            new FileProperty() { Property = "System.Audio.EncodingBitrate", NameResource = "PropertyEncodingBitrate", SectionResource = "PropertySectionAudio"},
            new FileProperty() { Property = "System.Audio.Compression", NameResource = "PropertyCompression", SectionResource = "PropertySectionAudio"},
            new FileProperty() { Property = "System.Audio.Format", NameResource = "PropertyFormat", SectionResource = "PropertySectionAudio"},
            new FileProperty() { Property = "System.Audio.SampleRate", NameResource = "PropertySampleRate", SectionResource = "PropertySectionAudio"},
            new FileProperty() { Property = "System.Music.AlbumID", NameResource = "PropertyAlbumID", SectionResource = "PropertySectionMusic"},
            new FileProperty() { Property = "System.Music.DisplayArtist", NameResource = "PropertyDisplayArtist", SectionResource = "PropertySectionMusic"},
            new FileProperty() { Property = "System.Media.CreatorApplication", NameResource = "PropertyCreatorApplication", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Music.AlbumArtist", NameResource = "PropertyAlbumArtist", SectionResource = "PropertySectionMusic"},
            new FileProperty() { Property = "System.Music.AlbumTitle", NameResource = "PropertyAlbumTitle", SectionResource = "PropertySectionMusic"},
            new FileProperty() { Property = "System.Music.Artist", NameResource = "PropertyArtist", SectionResource = "PropertySectionMusic"},
            new FileProperty() { Property = "System.Music.BeatsPerMinute", NameResource = "PropertyBeatsPerMinute", SectionResource = "PropertySectionMusic"},
            new FileProperty() { Property = "System.Music.Composer", NameResource = "PropertyComposer", SectionResource = "PropertySectionMusic"},
            new FileProperty() { Property = "System.Music.Conductor", NameResource = "PropertyConductor", SectionResource = "PropertySectionMusic"},
            new FileProperty() { Property = "System.Music.DiscNumber", NameResource = "PropertyDiscNumber", SectionResource = "PropertySectionMusic"},
            new FileProperty() { Property = "System.Music.Genre", NameResource = "PropertyGenre", SectionResource = "PropertySectionMusic"},
            new FileProperty() { Property = "System.Music.TrackNumber", NameResource = "PropertyTrackNumber", SectionResource = "PropertySectionMusic"},
            new FileProperty() { Property = "System.Media.AverageLevel", NameResource = "PropertyAverageLevel", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.Duration", NameResource = "PropertyDuration", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.FrameCount", NameResource = "PropertyFrameCount", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.ProtectionType", NameResource = "PropertyProtectionType", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.AuthorUrl", NameResource = "PropertyAuthorUrl", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.ContentDistributor", NameResource = "PropertyContentDistributor", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.DateReleased", NameResource = "PropertyDateReleased", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.SeriesName", NameResource = "PropertySeriesName", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.SeasonNumber", NameResource = "PropertySeasonNumber", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.EpisodeNumber", NameResource = "PropertyEpisodeNumber", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.Producer", NameResource = "PropertyProducer", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.PromotionUrl", NameResource = "PropertyPromotionUrl", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.ProviderStyle", NameResource = "PropertyProviderStyle", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.Publisher", NameResource = "PropertyPublisher", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.ThumbnailLargePath", NameResource = "PropertyThumbnailLargePath", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.ThumbnailLargeUri", NameResource = "PropertyThumbnailLargeUri", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.ThumbnailSmallPath", NameResource = "PropertyThumbnailSmallPath", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.ThumbnailSmallUri", NameResource = "PropertyThumbnailSmallUri", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.UniqueFileIdentifier", NameResource = "PropertyUniqueFileIdentifier", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.UserWebUrl", NameResource = "PropertyUserWebUrl", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.Writer", NameResource = "PropertyWriter", SectionResource = "PropertySectionMedia"},
            new FileProperty() { Property = "System.Media.Year", NameResource = "PropertyYear", SectionResource = "PropertySectionMedia"},
        };
    }
}