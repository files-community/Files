using Files.App.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.UITests.Data
{
	internal class TableViewItemModel : ITableViewCellValueProvider
	{
		public string? Name { get; set; }

		public string? DateUpdated { get; set; }

		public string? Type { get; set; }

		public string? Size { get; set; }

		public string GetValue(string name)
		{
			return name switch
			{
				nameof(Name) => Name ?? string.Empty,
				nameof(DateUpdated) => DateUpdated ?? string.Empty,
				nameof(Type) => Type ?? string.Empty,
				nameof(Size) => Size ?? string.Empty,
				_ => string.Empty,
			};
		}
	}
}
