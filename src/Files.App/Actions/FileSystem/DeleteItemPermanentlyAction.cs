// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Extensions;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class DeleteItemPermanentlyAction : BaseDeleteAction, IAction
	{
		public string Label
			=> "DeletePermanently".GetLocalizedResource();

		public string Description
			=> "DeleteItemPermanentlyDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Delete, KeyModifiers.Shift);

		public Task ExecuteAsync()
		{
			return DeleteItems(true);
		}
	}
}
