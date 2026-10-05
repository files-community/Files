// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.IO;
using System.Xml;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Lists the context menu commands that installed packages register with the
	/// <c>windows.fileExplorerContextMenus</c> extension, i.e. the items of the Windows 11 context menu.
	/// </summary>
	internal static class ExplorerCommandCatalog
	{
		internal sealed record Verb(Guid Clsid, string ItemType);

		private static readonly Lock _lock = new();
		private static Task<IReadOnlyList<Verb>>? _verbs;
		private static PackageCatalog? _packageCatalog;

		public static Task<IReadOnlyList<Verb>> GetVerbsAsync()
		{
			lock (_lock)
			{
				if (_verbs is null)
				{
					WatchPackageChanges();
					_verbs = Task.Run(LoadVerbs);
				}
				return _verbs;
			}
		}

		private static void Invalidate()
		{
			lock (_lock)
				_verbs = null;
		}

		private static void WatchPackageChanges()
		{
			if (_packageCatalog is not null)
				return;

			try
			{
				_packageCatalog = PackageCatalog.OpenForCurrentUser();
				_packageCatalog.PackageInstalling += (_, e) => { if (e.IsComplete) Invalidate(); };
				_packageCatalog.PackageUninstalling += (_, e) => { if (e.IsComplete) Invalidate(); };
				_packageCatalog.PackageUpdating += (_, e) => { if (e.IsComplete) Invalidate(); };
				_packageCatalog.PackageStatusChanged += (_, _) => Invalidate();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Failed to watch package changes for context menu commands.");
			}
		}

		private static IReadOnlyList<Verb> LoadVerbs()
		{
			var verbs = new List<Verb>();
			try
			{
				var packages = new PackageManager().FindPackagesForUserWithPackageTypes(string.Empty, PackageTypes.Main | PackageTypes.Optional);
				foreach (var package in packages)
				{
					try
					{
						var manifestPath = Path.Combine(package.InstalledPath, "AppxManifest.xml");
						if (File.Exists(manifestPath))
							ReadManifest(manifestPath, verbs);
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex);
					}
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Failed to list packaged context menu commands.");
			}

			return verbs;
		}

		private static void ReadManifest(string manifestPath, List<Verb> verbs)
		{
			var text = File.ReadAllText(manifestPath);
			if (!text.Contains("FileExplorerContextMenus", StringComparison.Ordinal))
				return;

			var document = new XmlDocument();
			document.LoadXml(text);

			// Match by local name: the elements live in the desktop4, desktop5 or desktop10 namespaces.
			var menus = document.SelectNodes("//*[local-name()='FileExplorerContextMenus']");
			if (menus is null)
				return;

			foreach (XmlNode menu in menus)
			{
				foreach (XmlNode itemType in menu.ChildNodes)
				{
					if (itemType.LocalName != "ItemType" || itemType.Attributes?["Type"]?.Value is not { Length: > 0 } type)
						continue;

					foreach (XmlNode verb in itemType.ChildNodes)
					{
						if (verb.LocalName == "Verb" && Guid.TryParse(verb.Attributes?["Clsid"]?.Value, out var clsid))
							verbs.Add(new(clsid, type));
					}
				}
			}
		}
	}
}
