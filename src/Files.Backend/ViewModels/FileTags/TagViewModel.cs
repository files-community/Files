// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text.Json.Serialization;

namespace Files.Backend.ViewModels.FileTags
{
	[Serializable]
	public sealed partial class TagViewModel : ObservableObject
	{
		[JsonPropertyName("TagName")]
		public string Name { get; set; }

		[JsonPropertyName("ColorString")]
		public string Color { get; set; }

		[JsonPropertyName("Uid")]
		public string Uid { get; set; }

		[JsonConstructor]
		public TagViewModel(string name, string color, string uid)
		{
			Name = name;
			Color = color;
			Uid = uid;
		}
	}
}
