using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using System.Linq;

namespace Files.App.UserControls.Settings
{
	public static partial class StyleExtensions
	{
		// Used to distinct normal ResourceDictionary and the one we add.
		private sealed class StyleExtensionResourceDictionary : ResourceDictionary
		{
		}

		public static ResourceDictionary GetResources(Style obj)
			=> (ResourceDictionary)obj.GetValue(ResourcesProperty);

		public static void SetResources(Style obj, ResourceDictionary value)
			=> obj.SetValue(ResourcesProperty, value);

		public static readonly DependencyProperty ResourcesProperty = DependencyProperty.RegisterAttached(
			"Resources",
			typeof(ResourceDictionary),
			typeof(StyleExtensions),
			new PropertyMetadata(null, ResourcesChanged));

		private static void ResourcesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is not FrameworkElement frameworkElement)
				return;

			var mergedDictionaries = frameworkElement.Resources?.MergedDictionaries;
			if (mergedDictionaries == null)
				return;

			var existingResourceDictionary = mergedDictionaries.FirstOrDefault(c => c is StyleExtensionResourceDictionary);
			if (existingResourceDictionary != null)
				// Remove the existing resource dictionary
				mergedDictionaries.Remove(existingResourceDictionary);

			if (e.NewValue is ResourceDictionary resource)
			{
				var clonedResources = new StyleExtensionResourceDictionary();
				clonedResources.CopyFrom(resource);
				mergedDictionaries.Add(clonedResources);
			}

			if (frameworkElement.IsLoaded)
				// Only force if the style was applied after the control was loaded
				ForceControlToReloadThemeResources(frameworkElement);
		}

		private static void ForceControlToReloadThemeResources(FrameworkElement frameworkElement)
		{
			// Note:
			//  To force the refresh of all resource references.
			//  Doesn't work when in high-contrast.
			var currentRequestedTheme = frameworkElement.RequestedTheme;

			frameworkElement.RequestedTheme = currentRequestedTheme == ElementTheme.Dark
				? ElementTheme.Light
				: ElementTheme.Dark;

			frameworkElement.RequestedTheme = currentRequestedTheme;
		}
	}

	internal static class ResourceDictionaryExtensions
	{
		internal static void CopyFrom(this ResourceDictionary destination, ResourceDictionary source)
		{
			if (source.Source != null)
			{
				destination.Source = source.Source;
			}
			else
			{
				// Clone theme dictionaries
				if (source.ThemeDictionaries != null)
				{
					foreach (var theme in source.ThemeDictionaries)
					{
						if (theme.Value is ResourceDictionary themedResource)
						{
							var themeDictionary = new ResourceDictionary();
							themeDictionary.CopyFrom(themedResource);
							destination.ThemeDictionaries[theme.Key] = themeDictionary;
						}
						else
						{
							destination.ThemeDictionaries[theme.Key] = theme.Value;
						}
					}
				}

				// Clone merged dictionaries
				if (source.MergedDictionaries != null)
				{
					foreach (var mergedResource in source.MergedDictionaries)
					{
						var themeDictionary = new ResourceDictionary();
						themeDictionary.CopyFrom(mergedResource);
						destination.MergedDictionaries.Add(themeDictionary);
					}
				}

				// Clone all contents
				foreach (var item in source)
					destination[item.Key] = item.Value;
			}
		}
	}
}
