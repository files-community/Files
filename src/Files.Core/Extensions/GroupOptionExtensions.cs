// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Extensions
{
	public static class GroupOptionExtensions
	{
		public static bool IsGroupByDate(this GroupOption groupOption)
			=> groupOption is GroupOption.DateModified or GroupOption.DateCreated or GroupOption.DateDeleted;
	}
}
