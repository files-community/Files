﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Server.Data.Enums;

namespace Files.App.Extensions
{
	public static class GroupOptionExtensions
	{
		public static bool IsGroupByDate(this GroupOption groupOption)
			=> groupOption is GroupOption.DateModified or GroupOption.DateCreated or GroupOption.DateDeleted;
	}
}
