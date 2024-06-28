// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	internal sealed class CopyItemAction : BaseTransferItemAction, IAction
	{
		public string Label
			=> "Copy".GetLocalizedResource();

		public string Description
			=> "CopyItemDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconCopy");

		public HotKey HotKey
			=> new(Keys.C, KeyModifiers.Ctrl);

		public CopyItemAction() : base()
		{
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			return ExecuteTransferAsync(DataPackageOperation.Copy);
		}
	}
}
