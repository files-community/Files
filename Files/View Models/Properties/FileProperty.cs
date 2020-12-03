using Files.Converters;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.Extensions;
using Newtonsoft.Json;
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
        public string Name
        {
            get => NameResource.GetLocalized();
        }
        /// <summary>
        /// The name of the string resource for the property name
        /// </summary>
        public string NameResource { get; set; }
        /// <summary>
        /// The name of the section to display
        /// </summary>
        public string Section
        {
            get => SectionResource.GetLocalized();
        }
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
                {
                    Value = ConvertBack(value);
                    Modified = true;
                }
            }
        }

        /// <summary>
        /// This function is run on the value of the property before displaying it.
        /// Also serves as an alternative to the Converter property
        /// Note: should only be used on read only properties
        /// </summary>
        public Func<object, string> DisplayFunction { get; set; }

        /// <summary>
        /// The name of the display function to get from the display dictionary
        /// </summary>
        public string DisplayFunctionName { get; set; }

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

        /// <summary>
        /// True if the property value has been modified by the user
        /// </summary>
        public bool Modified { get; private set; }

        public Visibility Visibility { get; set; } = Visibility.Visible;

        public FileProperty()
        {
        }

        public FileProperty(string nameResource, string sectionResource)
        {
            NameResource = nameResource;
            SectionResource = sectionResource;
        }

        public FileProperty(string property, string nameResource, string sectionResource)
            : this(nameResource, sectionResource)
        {
            Property = property;
        }

        public FileProperty(string property, string nameResource, string sectionResource, bool isReadOnly)
            : this(property, nameResource, sectionResource)
        {
            IsReadOnly = isReadOnly;
        }

        /// <summary>
        /// Use this function when taking a file property from the base list and adding it to the view model list.
        /// </summary>
        /// <param name="fileProperty"></param>
        /// <returns></returns>
        public async Task InitializeProperty(StorageFile file)
        {
            Func<object, string> displayFunction;
            if (!string.IsNullOrEmpty(DisplayFunctionName) && DisplayFuncs.TryGetValue(DisplayFunctionName, out displayFunction))
            {
                DisplayFunction = displayFunction;
            }
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
            {
                return;
            }

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
            if (Value is uint)
            {
                return new UInt32ToString();
            }

            if (Value is double)
            {
                return new DoubleToString();
            }

            if (Value is DateTimeOffset)
            {
                return new DateTimeOffsetToString();
            }

            if (Value != null && Value.GetType().IsArray)
            {
                if (Value.GetType().GetElementType().Equals(typeof(string)))
                {
                    return new StringArrayToString();
                }

                if (Value.GetType().GetElementType().Equals(typeof(double)))
                {
                    return new DoubleArrayToString();
                }
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
            {
                return DisplayFunction.Invoke(Value);
            }

            if (Converter != null && Value != null)
            {
                return Converter.Convert(Value, typeof(string), null, null) as string;
            }

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
            {
                return Converter.ConvertBack(value, typeof(object), null, null);
            }

            return value;
        }

        public async static Task<List<FileProperty>> RetrieveAndInitializePropertiesAsync(StorageFile file)
        {
            var propertiesJsonFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(@"ms-appx:///Resources/PropertiesInformation.json"));
            var list = JsonConvert.DeserializeObject<List<FileProperty>>(await FileIO.ReadTextAsync(propertiesJsonFile));

            var propsToGet = new List<string>();

            foreach (var prop in list)
            {
                if (!string.IsNullOrEmpty(prop.Property))
                    propsToGet.Add(prop.Property);
            }

            var keyValuePairs = await file.Properties.RetrievePropertiesAsync(propsToGet);

            foreach (var prop in list)
            {
                if (!string.IsNullOrEmpty(prop.Property))
                    prop.Value = keyValuePairs[prop.Property];

                await prop.InitializeProperty(file);
            }

            return list;
        }

        /// <summary>
        /// Since you can't serialize lambdas from a json file, define them here
        /// </summary>
        private static readonly Dictionary<string, Func<object, string>> DisplayFuncs = new Dictionary<string, Func<object, string>>()
        {
            { "DivideBy100", input => (((uint) input)/1000).ToString() },
            { "FormatDuration", input => new TimeSpan(Convert.ToInt64(input)).ToString("mm':'ss")},
        };
    }
}