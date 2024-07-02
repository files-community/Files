// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	internal sealed class CutItemAction : BaseTransferItemAction, IAction
	{
		public string Label
			=> "Cut".GetLocalizedResource();

		public string Description
			=> "CutItemDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconCut");

		public HotKey HotKey
			=> new(Keys.X, KeyModifiers.Ctrl);

		public CutItemAction() : base()
		{
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			return ExecuteTransferAsync(DataPackageOperation.Move);
		}
	}
}
