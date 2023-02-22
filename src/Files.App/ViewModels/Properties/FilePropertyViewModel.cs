using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Converters;
using Files.App.Extensions;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.ViewModels.Properties
{
	/// <summary>
	/// This class is represents a system file property from the Windows.Storage API
	/// </summary>
	public class FilePropertyViewModel : ObservableObject
	{
		/// <summary>
		/// The name to display
		/// </summary>
		public string Name
			=> LocalizedName ?? NameResource.GetLocalizedResource();

		/// <summary>
		/// The name of the string resource for the property name
		/// </summary>
		public string NameResource { get; set; }

		public string LocalizedName { get; set; }

		/// <summary>
		/// The name of the section to display
		/// </summary>
		public string Section
			=> SectionResource?.GetLocalizedResource();

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
			=> GetConverter();

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

		public bool InProgress { get; set; }

		/// <summary>
		/// If a property has an enumerated list of strings to display, add a dictionary in the JSON file that has the number as it's key
		/// and the string resource as its value
		/// </summary>
		public Dictionary<int, string> EnumeratedList { get; set; }

		public FilePropertyViewModel()
		{
		}

		public FilePropertyViewModel(string nameResource, string sectionResource)
		{
			NameResource = nameResource;
			SectionResource = sectionResource;
		}

		public FilePropertyViewModel(string property, string nameResource, string sectionResource)
			: this(nameResource, sectionResource)
		{
			Property = property;
		}

		public FilePropertyViewModel(string property, string nameResource, string sectionResource, bool isReadOnly)
			: this(property, nameResource, sectionResource)
		{
			IsReadOnly = isReadOnly;
		}

		/// <summary>
		/// This is called before properties are displayed
		/// </summary>
		public void InitializeProperty()
		{
			Func<object, string> displayFunction;
			if (!string.IsNullOrEmpty(DisplayFunctionName) && DisplayFuncs.TryGetValue(DisplayFunctionName, out displayFunction))
			{
				DisplayFunction = displayFunction;
			}
		}

		/// <summary>
		/// Saves the property to a given file
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public Task SaveValueToFile(BaseStorageFile file)
		{
			if (!string.IsNullOrEmpty(Property) || file.Properties is null)
			{
				return Task.CompletedTask;
			}

			var propsToSave = new Dictionary<string, object>();
			propsToSave.Add(Property, Converter.ConvertBack(Value, null, null, null));

			return file.Properties.SavePropertiesAsync(propsToSave).AsTask();
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

			if (Value is not null && Value.GetType().IsArray)
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
			// Don't attempt any convert null values
			if (Value is null)
			{
				return null;
			}

			if (EnumeratedList is not null)
			{
				var value = "";
				return EnumeratedList.TryGetValue(Convert.ToInt32(Value), out value) ? value.GetLocalizedResource() : null;
			}

			if (DisplayFunction is not null)
			{
				return DisplayFunction.Invoke(Value);
			}

			if (Converter is not null)
			{
				return Converter.Convert(Value, typeof(string), null, null) as string;
			}

			return Value.ToString();
		}

		/// <summary>
		/// Converts a string from a text block back to it's original type
		/// </summary>
		/// <param name="value">The input string</param>
		/// <returns></returns>
		private object ConvertBack(string value)
		{
			if (Converter is not null && value is not null)
			{
				return Converter.ConvertBack(value, typeof(object), null, null);
			}

			return value;
		}

		private static Dictionary<string, string> cachedPropertiesListFiles = new Dictionary<string, string>();

		/// <summary>
		/// This function retrieves the list of properties to display from the PropertiesInformation.json
		/// file, then initializes them.
		/// If you would like to add more properties, define them in the PropertiesInformation file, then
		/// add the string resources to Strings/en-Us/Resources.resw file
		/// A full list of file properties and their information can be found here
		/// <a href="https://docs.microsoft.com/windows/win32/properties/props"/>.
		/// </summary>
		/// <param name="file">The file whose properties you wish to obtain</param>
		/// <param name="path">The path to the json file of properties to be loaded</param>
		/// <returns>A list if FileProperties containing their values</returns>
		public async static Task<List<FilePropertyViewModel>> RetrieveAndInitializePropertiesAsync(BaseStorageFile file, string path = Core.Constants.ResourceFilePaths.DetailsPagePropertiesJsonPath)
		{
			// cache the contents of the file to avoid repeatedly reading the file
			string text;
			if (!cachedPropertiesListFiles.TryGetValue(path, out text))
			{
				var propertiesJsonFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(path));
				text = await FileIO.ReadTextAsync(propertiesJsonFile);
				cachedPropertiesListFiles[path] = text;
			}

			List<FilePropertyViewModel> list = JsonSerializer.Deserialize<List<FilePropertyViewModel>>(text);

			var propsToGet = new List<string>();

			foreach (var prop in list)
			{
				if (!string.IsNullOrEmpty(prop.Property))
				{
					propsToGet.Add(prop.Property);
				}
			}

#if DEBUG
			// This makes it much easier to debug issues with the property list
			var keyValuePairs = new Dictionary<string, object>();
			foreach (var prop in propsToGet)
			{
				object val = null;
				try
				{
					if (file.Properties is not null)
					{
						val = (await file.Properties.RetrievePropertiesAsync(new string[] { prop })).First().Value;
					}
				}
				catch (ArgumentException e)
				{
					Debug.WriteLine($"Unable to retrieve system file property {prop}.\n{e}");
				}
				keyValuePairs.Add(prop, val);
			}
#else
            IDictionary<string, object> keyValuePairs = new Dictionary<string, object>();
            if (file.Properties is not null)
            {
                keyValuePairs = await file.Properties.RetrievePropertiesAsync(propsToGet);
            }
#endif

			foreach (var prop in list)
			{
				if (!string.IsNullOrEmpty(prop.Property))
				{
					prop.Value = keyValuePairs[prop.Property];
				}

				prop.InitializeProperty();
			}

			return list;
		}

		/// <summary>
		/// Since you can't serialize lambdas from a json file, define them here
		/// </summary>
		private static readonly Dictionary<string, Func<object, string>> DisplayFuncs = new Dictionary<string, Func<object, string>>()
		{
			{ "DivideBy1000", input => (((uint) input)/1000).ToString() },
			{ "FormatDuration", input => new TimeSpan(Convert.ToInt64(input)).ToString("hh':'mm':'ss")},
			{ "Fraction" , input => ((double)input).ToFractions(2000)},
			{ "AddF" , input => $"f/{(double)input}"},
			{ "AddISO" , input => $"ISO-{(UInt16)input}"},
			{ "RoundDouble" , input => $"{Math.Round((double)input)}"},
			{ "UnitMM" , input => $"{(double)input} mm"},
		};
	}
}
