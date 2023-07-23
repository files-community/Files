// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Files.App.UserControls
{
	public static partial class StyleExtensions
	{
		// Used to distinct normal ResourceDictionary and the one we add.
		private sealed class StyleExtensionResourceDictionary : ResourceDictionary
		{
		}

		public static ResourceDictionary GetResources(Style obj)
		{
			return (ResourceDictionary)obj.GetValue(ResourcesProperty);
		}

		public static void SetResources(Style obj, ResourceDictionary value)
		{
			obj.SetValue(ResourcesProperty, value);
		}

		public static readonly DependencyProperty ResourcesProperty =
			DependencyProperty.RegisterAttached("Resources", typeof(ResourceDictionary), typeof(StyleExtensions), new PropertyMetadata(null, ResourcesChanged));

		private static void ResourcesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (!(sender is FrameworkElement frameworkElement))
			{
				return;
			}

			var mergedDictionaries = frameworkElement.Resources?.MergedDictionaries;
			if (mergedDictionaries == null)
			{
				return;
			}

			var existingResourceDictionary =
				mergedDictionaries.FirstOrDefault(c => c is StyleExtensionResourceDictionary);
			if (existingResourceDictionary != null)
			{
				// Remove the existing resource dictionary
				mergedDictionaries.Remove(existingResourceDictionary);
			}

			if (e.NewValue is ResourceDictionary resource)
			{
				var clonedResources = new StyleExtensionResourceDictionary();
				clonedResources.CopyFrom(resource);
				mergedDictionaries.Add(clonedResources);
			}

			if (frameworkElement.IsLoaded)
			{
				// Only force if the style was applied after the control was loaded
				ForceControlToReloadThemeResources(frameworkElement);
			}
		}

		private static void ForceControlToReloadThemeResources(FrameworkElement frameworkElement)
		{
			// To force the refresh of all resource references.
			// Note: Doesn't work when in high-contrast.
			var currentRequestedTheme = frameworkElement.RequestedTheme;
			frameworkElement.RequestedTheme = currentRequestedTheme == ElementTheme.Dark
				? ElementTheme.Light
				: ElementTheme.Dark;
			frameworkElement.RequestedTheme = currentRequestedTheme;
		}
	}
}
