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
	internal static class ResourceDictionaryExtensions
	{
		/// <summary>
		/// Copies  the <see cref="ResourceDictionary"/> provided as a parameter into the calling dictionary, includes overwriting the source location, theme dictionaries, and merged dictionaries.
		/// </summary>
		/// <param name="destination">ResourceDictionary to copy values to.</param>
		/// <param name="source">ResourceDictionary to copy values from.</param>
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
				{
					destination[item.Key] = item.Value;
				}
			}
		}
	}
}
