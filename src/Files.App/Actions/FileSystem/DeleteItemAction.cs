// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Extensions;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class DeleteItemAction : BaseDeleteAction, IAction
	{
		public string Label { get; } = "Delete".GetLocalizedResource();

		public string Description => "DeleteItemDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconDelete");

		public HotKey HotKey { get; } = new(Keys.Delete);

		public HotKey SecondHotKey { get; } = new(Keys.D, KeyModifiers.Ctrl);

		public Task ExecuteAsync()
		{
			return DeleteItems(false);
		}
	}
}
