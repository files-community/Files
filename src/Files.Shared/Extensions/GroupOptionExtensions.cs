// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Enums;

namespace Files.Shared.Extensions
{
	public static class GroupOptionExtensions
	{
		public static bool IsGroupByDate(this GroupOption groupOption)
		{
			return groupOption is GroupOption.DateModified or GroupOption.DateCreated or GroupOption.DateDeleted;
		}
	}
}
