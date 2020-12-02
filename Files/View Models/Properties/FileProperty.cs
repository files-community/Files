using Files.Converters;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Files.View_Models.Properties
{
    /// <summary>
    /// This class is represents a system file property from the Windows.Storage API
    /// </summary>
    public class FileProperty : ObservableObject
    {
        /// <summary>
        /// The name to display
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The name of the string resource for the property name
        /// </summary>
        public string NameResource { get; set; }
        /// <summary>
        /// The name of the section to display
        /// </summary>
        public string Section { get; set; }
        /// <summary>
        /// The name of the string resource for the section name
        /// </summary>
        public string SectionResource { get; set; }
        /// <summary>
        /// The identifier of the property to get, eg System.Media.Duration
        /// </summary>
        public string Property { get; set; }
        /// <summary>
        /// The value of the property
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// The string value of the property's value
        /// </summary>
        public string ValueText
        {
            get => ConvertToString();
            set
            {
                if (!IsReadOnly)
                    Value = ConvertBack(value);
            }
        }

        /// <summary>
        /// This function is run on the value of the property before displaying it.
        /// Also serves as an alternative to the Converter property
        /// Note: should only be used on read only properties
        /// </summary>
        public Func<object, string> DisplayFunction { get; set; }

        /// <summary>
        /// The converter used to convert the property to a string, and vice versa if needed
        /// </summary>
        public IValueConverter Converter
        {
            get => GetConverter();
        }
        public bool IsReadOnly { get; set; } = true;

        /// <summary>
        /// Should be used in instances where a property does not have a "Property" value, but needs to be idenitfiable in a list of properties
        /// </summary>
        public string ID { get; set; }

        public Visibility Visibility { get; set; } = Visibility.Visible;

        public FileProperty()
        {
        }

        public FileProperty(string property, string nameResource, string sectionResource)
        {
            Property = property;
            NameResource = nameResource;
            SectionResource = sectionResource;
        }
        public FileProperty(string nameResource, string sectionResource)
        {
            NameResource = nameResource;
            SectionResource = sectionResource;
        }
        public FileProperty(string property, string nameResource, string sectionResource, bool isReadOnly)
        {
            Property = property;
            NameResource = nameResource;
            SectionResource = sectionResource;
            IsReadOnly = isReadOnly;
        }

        /// <summary>
        /// Use this function when taking a file property from the base list and adding it to the view model list.
        /// </summary>
        /// <param name="fileProperty"></param>
        /// <returns></returns>
        public async Task InitializeProperty(StorageFile file)
        {
            if (!string.IsNullOrEmpty(Property))
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
        }

        /// <summary>
        /// Saves the property to a given file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task SaveValueToFile(StorageFile file)
        {
            if (!string.IsNullOrEmpty(Property))
                return;

            var propsToSave = new Dictionary<string, object>();
            propsToSave.Add(Property, Converter.ConvertBack(Value, null, null, null));
            await file.Properties.SavePropertiesAsync(propsToSave);
        }

        /// <summary>
        /// Call this function just after getting properties to set the property's converter based on the value type.
        /// For some reason, this does not work for arrays. In the case of arrays, override the converter
        /// </summary>
        private IValueConverter GetConverter()
        {
            if (Value is UInt32)
                return new UInt32ToString();

            if (Value is Double)
                return new DoubleToString();

            if (Value is DateTimeOffset)
                return new DateTimeOffsetToString();

            if (Value != null && Value.GetType().IsArray)
            {
                if (Value.GetType().GetElementType().Equals(typeof(string)))
                    return new StringArrayToString();

                if (Value.GetType().GetElementType().Equals(typeof(double)))
                    return new DoubleArrayToString();
            }
            return null;
        }

        /// <summary>
        /// Converts the property to a string to be displayed
        /// </summary>
        /// <returns></returns>
        private string ConvertToString()
        {
            if (DisplayFunction != null)
                return DisplayFunction.Invoke(Value);

            if (Converter != null && Value != null)
                return Converter.Convert(Value, typeof(string), null, null) as string;

            return Value as string;
        }

        /// <summary>
        /// Converts a string from a text block back to it's original type
        /// </summary>
        /// <param name="value">The input string</param>
        /// <returns></returns>
        private object ConvertBack(string value)
        {
            if (Converter != null && value != null)
                return Converter.ConvertBack(value, typeof(object), null, null);

            return value;
        }

        /// <summary>
        /// Uses the name and section name resources to get their values
        /// </summary>
        public void InitializeNames()
        {
            Name = NameResource.GetLocalized();
            Section = SectionResource.GetLocalized();
        }

        public static readonly List<FileProperty> PropertyListItemsBase = new List<FileProperty>()
        {
            new FileProperty("PropertyAddress", "PropertySectionGPS") { ID = "address" },

            new FileProperty("System.RatingText", "PropertyRatingText", "PropertySectionCore") ,
            new FileProperty("System.ItemFolderPathDisplay", "PropertyItemFolderPathDisplay", "PropertySectionCore") ,
            new FileProperty("System.ItemTypeText", "PropertyItemTypeText", "PropertySectionCore"),
            new FileProperty("System.Title", "PropertyTitle", "PropertySectionCore", false),
            new FileProperty("System.Subject", "PropertySubject", "PropertySectionCore", false),
            new FileProperty("System.Comment", "PropertyComment", "PropertySectionCore", false),
            new FileProperty("System.Copyright", "PropertyCopyright", "PropertySectionCore") ,
            new FileProperty( "System.DateCreated", "PropertyDateCreated", "PropertySectionCore"),
            new FileProperty("System.DateModified", "PropertyDateModified", "PropertySectionCore") ,
            new FileProperty("System.Image.BitDepth", "PropertyBitDepth", "PropertySectionImage"),
            new FileProperty("System.Image.Dimensions", "PropertyDimensions", "PropertySectionImage"),
            new FileProperty("System.Image.HorizontalSize", "PropertyHorizontalSize", "PropertySectionImage"),
            new FileProperty("System.Image.VerticalSize", "PropertyVerticalSize", "PropertySectionImage") ,
            new FileProperty("System.Image.HorizontalResolution", "PropertyHorizontalResolution", "PropertySectionImage") ,
            new FileProperty("System.Image.VerticalResolution", "PropertyVerticalResolution", "PropertySectionImage") ,
            new FileProperty("System.Image.CompressionText", "PropertyCompressionText", "PropertySectionImage") ,
            new FileProperty("System.Image.ColorSpace", "PropertyColorSpace", "PropertySectionImage") ,
            new FileProperty("System.GPS.LongitudeDecimal", "PropertyLongitudeDecimal", "PropertySectionGPS") ,
            new FileProperty("System.GPS.LatitudeDecimal", "PropertyLatitudeDecimal", "PropertySectionGPS") ,
            new FileProperty("System.GPS.Altitude", "PropertyAltitude", "PropertySectionGPS" ) ,
            new FileProperty("System.Photo.DateTaken", "PropertyDateTaken", "PropertySectionPhoto") ,
            new FileProperty("System.Photo.CameraManufacturer", "PropertyCameraManufacturer", "PropertySectionPhoto",  false) ,
            new FileProperty("System.Photo.CameraModel", "PropertyCameraModel", "PropertySectionPhoto",  false) ,
            new FileProperty("System.Photo.ExposureTime", "PropertyExposureTime", "PropertySectionPhoto") ,
            new FileProperty("System.Photo.FocalLength", "PropertyFocalLength", "PropertySectionPhoto") ,
            new FileProperty("System.Photo.Aperture", "PropertyAperture", "PropertySectionPhoto") ,
            new FileProperty("System.Photo.PeopleNames", "PropertyPeopleNames", "PropertySectionPhoto") ,
            new FileProperty("System.Audio.ChannelCount", "PropertyChannelCount", "PropertySectionAudio") ,
            new FileProperty("System.Audio.EncodingBitrate", "PropertyEncodingBitrate", "PropertySectionAudio") ,
            new FileProperty("System.Audio.Compression", "PropertyCompression", "PropertySectionAudio") ,
            new FileProperty("System.Audio.Format", "PropertyFormat", "PropertySectionAudio") ,
            new FileProperty("System.Audio.SampleRate", "PropertySampleRate", "PropertySectionAudio") ,
            new FileProperty("System.Music.DisplayArtist", "PropertyDisplayArtist", "PropertySectionMusic") ,
            new FileProperty("System.Music.AlbumArtist", "PropertyAlbumArtist", "PropertySectionMusic",  false) ,
            new FileProperty("System.Music.AlbumTitle", "PropertyAlbumTitle", "PropertySectionMusic",  false) ,
            new FileProperty("System.Music.Artist", "PropertyArtist", "PropertySectionMusic",  false) ,
            new FileProperty("System.Music.BeatsPerMinute", "PropertyBeatsPerMinute", "PropertySectionMusic") ,
            new FileProperty("System.Music.Composer", "PropertyComposer", "PropertySectionMusic",  false) ,
            new FileProperty("System.Music.Conductor", "PropertyConductor", "PropertySectionMusic",  false) ,
            new FileProperty("System.Music.DiscNumber", "PropertyDiscNumber", "PropertySectionMusic",  false) ,
            new FileProperty("System.Music.Genre", "PropertyGenre", "PropertySectionMusic",  false) ,
            new FileProperty("System.Music.TrackNumber", "PropertyTrackNumber", "PropertySectionMusic",  false) ,
            new FileProperty("System.Media.Duration", "PropertyDuration", "PropertySectionMedia") { DisplayFunction = input => new TimeSpan(Convert.ToInt64(input)).ToString("mm':'ss")},
            new FileProperty("System.Media.FrameCount", "PropertyFrameCount", "PropertySectionMedia") ,
            new FileProperty("System.Media.ProtectionType", "PropertyProtectionType", "PropertySectionMedia") ,
            new FileProperty("System.Media.AuthorUrl", "PropertyAuthorUrl", "PropertySectionMedia") ,
            new FileProperty("System.Media.ContentDistributor", "PropertyContentDistributor", "PropertySectionMedia") ,
            new FileProperty("System.Media.DateReleased", "PropertyDateReleased", "PropertySectionMedia") ,
            new FileProperty("System.Media.SeriesName", "PropertySeriesName", "PropertySectionMedia") ,
            new FileProperty("System.Media.SeasonNumber", "PropertySeasonNumber", "PropertySectionMedia") ,
            new FileProperty("System.Media.EpisodeNumber", "PropertyEpisodeNumber", "PropertySectionMedia") ,
            new FileProperty("System.Media.Producer", "PropertyProducer", "PropertySectionMedia") ,
            new FileProperty("System.Media.PromotionUrl", "PropertyPromotionUrl", "PropertySectionMedia") ,
            new FileProperty("System.Media.ProviderStyle", "PropertyProviderStyle", "PropertySectionMedia") ,
            new FileProperty("System.Media.Publisher", "PropertyPublisher", "PropertySectionMedia") ,
            new FileProperty("System.Media.ThumbnailLargePath", "PropertyThumbnailLargePath", "PropertySectionMedia") ,
            new FileProperty("System.Media.ThumbnailLargeUri", "PropertyThumbnailLargeUri", "PropertySectionMedia") ,
            new FileProperty("System.Media.ThumbnailSmallPath", "PropertyThumbnailSmallPath", "PropertySectionMedia") ,
            new FileProperty("System.Media.ThumbnailSmallUri", "PropertyThumbnailSmallUri", "PropertySectionMedia") ,
            new FileProperty("System.Media.UserWebUrl", "PropertyUserWebUrl", "PropertySectionMedia") ,
            new FileProperty("System.Media.Writer", "PropertyWriter", "PropertySectionMedia") ,
            new FileProperty("System.Media.Year", "PropertyYear", "PropertySectionMedia") ,
            new FileProperty("System.Document.Contributor", "PropertyContributor", "PropertySectionDocument") ,
            new FileProperty("System.Document.LastAuthor", "PropertyLastAuthor", "PropertySectionDocument") ,
            new FileProperty("System.Document.RevisionNumber", "PropertyRevisionNumber", "PropertySectionDocument") ,
            new FileProperty("System.Document.Version", "PropertyVersion", "PropertySectionDocument") ,
            new FileProperty("System.Document.TotalEditingTime", "PropertyTotalEditingTime", "PropertySectionDocument") ,
            new FileProperty("System.Document.Template", "PropertyTemplate", "PropertySectionDocument") ,
            new FileProperty("System.Document.WordCount", "PropertyWordCount", "PropertySectionDocument") ,
            new FileProperty("System.Document.CharacterCount", "PropertyCharacterCount", "PropertySectionDocument") ,
            new FileProperty("System.Document.LineCount", "PropertyLineCount", "PropertySectionDocument") ,
            new FileProperty("System.Document.ParagraphCount", "PropertyParagraphCount", "PropertySectionDocument") ,
            new FileProperty("System.Document.PageCount", "PropertyPageCount", "PropertySectionDocument") ,
            new FileProperty("System.Document.SlideCount", "PropertySlideCount", "PropertySectionDocument") ,
            // Frame rate unit is frames per 1000 seconds, so the display function divides it by 1000
            new FileProperty("System.Video.FrameRate", "PropertyFrameRate", "PropertySectionVideo") { DisplayFunction = input => (((UInt32) input)/1000).ToString()},
            new FileProperty("System.Video.EncodingBitrate", "PropertyEncodingBitrate", "PropertySectionVideo") ,
            new FileProperty("System.Video.Compression", "PropertyCompression", "PropertySectionVideo") ,
            new FileProperty("System.Video.FrameWidth", "PropertyFrameWidth", "PropertySectionVideo") ,
            new FileProperty("System.Video.FrameHeight", "PropertyFrameHeight", "PropertySectionVideo") ,
            new FileProperty("System.Video.Orientation", "PropertyOrientation", "PropertySectionVideo") ,
        };
    }
}