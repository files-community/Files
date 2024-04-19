﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Server.Utils;

internal sealed class RegistryUtils
{
	internal static string CombineKeys(string key1, string key2)
	{
		key1 = key1.Replace('/', '\\').Trim('\\');
		key2 = key2.Replace('/', '\\').Trim('\\');
		return $"{key1}\\{key2}";
	}

	internal static string CombineKeys(string key1, string key2, string key3)
	{
		key1 = key1.Replace('/', '\\').Trim('\\');
		key2 = key2.Replace('/', '\\').Trim('\\');
		key3 = key3.Replace('/', '\\').Trim('\\');
		return $"{key1}\\{key2}\\{key3}";
	}
}
