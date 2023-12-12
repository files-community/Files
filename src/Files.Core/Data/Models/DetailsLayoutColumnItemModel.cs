// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Models
{
	public class DetailsLayoutColumnItemModel : IDetailsLayoutColumnItem
	{
		public DetailsLayoutColumnKind Kind { get; set; }

		public double Width { get; set; }

		public bool IsVisible { get; set; }
	}
}
