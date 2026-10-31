// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32;
using System.Security;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Files.App.Server;

internal sealed class ShellNewRegistryMonitor
{
	private readonly RegistryChangeMonitor userClasses = new(Registry.CurrentUser, @"Software\Classes");
	private readonly RegistryChangeMonitor machineClasses = new(Registry.LocalMachine, @"Software\Classes");
	private int? previous;

	internal bool HasChanged()
	{
		if (!(userClasses.HasChanged() | machineClasses.HasChanged()))
			return false;

		// Ignore Explorer's frequent MuiCache/Bags writes outside the extension trees.
		var fingerprint = new HashCode();
		try
		{
			foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine })
			{
				fingerprint.Add(root.Name);
				using var classes = root.OpenSubKey(@"Software\Classes");
				if (classes is null)
					continue;
				foreach (var name in classes.GetSubKeyNames().Where(name => name.StartsWith('.')).Order(StringComparer.OrdinalIgnoreCase))
					AddKey(classes, name, ref fingerprint);
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
		{
			return true;
		}

		var current = fingerprint.ToHashCode();
		var changed = previous != current;
		previous = current;
		return changed;
	}

	private static unsafe void AddKey(RegistryKey parent, string name, ref HashCode fingerprint)
	{
		fingerprint.Add(name, StringComparer.OrdinalIgnoreCase);
		try
		{
			using var key = parent.OpenSubKey(name);
			if (key is null)
				return;

			FILETIME lastWriteTime = default;
			var error = PInvoke.RegQueryInfoKey(new HKEY(key.Handle.DangerousGetHandle()),
				null, null, null, null, null, null, null, null, null, null, &lastWriteTime);
			fingerprint.Add(error);
			if (error != WIN32_ERROR.ERROR_SUCCESS)
				return;
			fingerprint.Add(lastWriteTime.dwHighDateTime);
			fingerprint.Add(lastWriteTime.dwLowDateTime);

			foreach (var child in key.GetSubKeyNames().Order(StringComparer.OrdinalIgnoreCase))
				AddKey(key, child, ref fingerprint);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
		{
			fingerprint.Add(ex.HResult);
		}
	}
}
