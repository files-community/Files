// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.IO;
using Windows.ApplicationModel;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides package identity values, with fallbacks that allow the app to run
	/// without package identity (portable distribution).
	/// </summary>
	public static class AppRuntimeHelper
	{
		private const string PortableName = "FilesPortable";

		/// <summary>
		/// Gets the value that indicates whether the process runs with package identity.
		/// </summary>
		public static bool IsPackaged { get; }

		/// <summary>
		/// Gets the package name, or a stable substitute when unpackaged.
		/// </summary>
		public static string PackageName { get; }

		/// <summary>
		/// Gets the package family name, or a stable substitute when unpackaged.
		/// </summary>
		public static string PackageFamilyName { get; }

		/// <summary>
		/// Gets the app display name.
		/// </summary>
		public static string DisplayName { get; }

		/// <summary>
		/// Gets the app install folder path, without a trailing separator.
		/// </summary>
		public static string InstalledPath { get; }

		/// <summary>
		/// Gets the folder path the app executable runs from.
		/// </summary>
		public static string EffectivePath { get; }

		/// <summary>
		/// Gets the app version.
		/// </summary>
		public static Version AppVersion { get; }

		static AppRuntimeHelper()
		{
			try
			{
				var package = Package.Current;

				IsPackaged = true;
				PackageName = package.Id.Name;
				PackageFamilyName = package.Id.FamilyName;
				DisplayName = package.DisplayName;
				InstalledPath = package.InstalledLocation.Path;
				EffectivePath = package.EffectivePath;
				AppVersion = new(package.Id.Version.Major, package.Id.Version.Minor, package.Id.Version.Build, package.Id.Version.Revision);
			}
			catch (Exception) // Package.Current throws (InvalidOperationException or COMException 0x80073D54) when the process has no package identity
			{
				IsPackaged = false;
				PackageName = PortableName;
				PackageFamilyName = PortableName;
				DisplayName = "Files";
				// Single-file publish extracts the payload to a temp folder: BaseDirectory points
				// at the extracted assets while ProcessPath stays at the folder the user runs from
				InstalledPath = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
				EffectivePath = (Path.GetDirectoryName(Environment.ProcessPath) ?? InstalledPath).TrimEnd(Path.DirectorySeparatorChar);
				AppVersion = typeof(AppRuntimeHelper).Assembly.GetName().Version ?? new Version(1, 0, 0, 0);
			}
		}
	}
}
