// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.Properties;
using Files.App.Storage;
using Files.Sdk.Storage;
using Files.Sdk.Storage.LocatableStorage;
using System.Text;

namespace Files.App.Filesystem
{
	public class StandardItemViewModel : StorableViewModel
	{
		public IStorageProperties Properties { get; set; }

		public StandardItemViewModel(IStorable storable) : base(storable)
		{
			if (storable is ILocatableStorable ls)
			{
				this.Properties = new ShellItemProperties(ls);
			}
		}

		public async Task<string> GetSummaryAsync()
		{
			var displayType = await Properties.GetStoragePropertyAsync("System.ItemTypeText");

			var tooltipBuilder = new StringBuilder();
			tooltipBuilder.AppendLine($"{"ToolTipDescriptionName".GetLocalizedResource()} { Storable.Name}");
			tooltipBuilder.AppendLine($"{"ToolTipDescriptionType".GetLocalizedResource()} { displayType.Value as string }");
			tooltipBuilder.Append($"{"ToolTipDescriptionDate".GetLocalizedResource()} {Properties.DateModified}");
			if (Properties.Size > 0)
				tooltipBuilder.Append($"{Environment.NewLine}{"SizeLabel".GetLocalizedResource()} {Properties.Size}");

			return tooltipBuilder.ToString();
		}
	}
}