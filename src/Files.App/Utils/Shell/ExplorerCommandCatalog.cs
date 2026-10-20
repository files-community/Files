// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.IO;
using Windows.Win32;
using Windows.Win32.System.WinRT;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Lists the context menu commands that installed packages register with the
	/// <c>windows.fileExplorerContextMenus</c> extension, i.e. the items of the Windows 11 context menu.
	/// </summary>
	/// <remarks>
	/// The registrations come from <c>Windows.Internal.FileExplorerAppExtension</c>, the undocumented class File Explorer
	/// reads them from; its interface revision is picked by querying for it, and if the class is missing no command is listed.
	/// It already leaves out the commands listed as blocked shell extensions. It reads the registrations once per process,
	/// so packages installed or removed while Files is running are only reflected after a restart.
	/// </remarks>
	internal static class ExplorerCommandCatalog
	{
		internal sealed record Verb(Guid Clsid, string ItemType);

		private const string RuntimeClassName = "Windows.Internal.FileExplorerAppExtension";

		// Wait until the registrations have been read, instead of returning what has been read so far.
		private const uint WaitForRegistrations = 1;

		private static readonly Lazy<IFileExplorerAppExtensionStatics?> _statics = new(GetStatics);

		/// <summary>
		/// Reads the registrations, which takes a few hundred milliseconds the first time.
		/// </summary>
		public static Task WarmUpAsync()
			=> GetVerbsAsync(["*"]);

		/// <summary>
		/// Gets the commands registered for the given item types, e.g. <c>*</c>, <c>.txt</c>, <c>Directory</c> or <c>Directory\Background</c>.
		/// </summary>
		public static Task<IReadOnlyList<Verb>> GetVerbsAsync(string[] itemTypes)
			=> Task.Run(() => GetVerbs(itemTypes));

		/// <summary>
		/// Gets the item types a selection can match: each extension and every kind of item, since the selection is not
		/// inspected yet. Whether a command applies to each item is decided once the items are known.
		/// </summary>
		public static string[] GetItemTypes(string[] filePaths, bool isBackground)
		{
			if (isBackground)
				return ["Directory\\Background"];

			var extensions = filePaths.Select(Path.GetExtension).Where(extension => !string.IsNullOrEmpty(extension)).Cast<string>();
			return [.. extensions, "*", "Directory", "Drive", "Folder"];
		}

		private static IReadOnlyList<Verb> GetVerbs(string[] itemTypes)
		{
			var verbs = new List<Verb>();
			if (_statics.Value is not { } statics)
				return verbs;

			foreach (var itemType in itemTypes.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				try
				{
					AppendVerbs(statics, itemType, verbs);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
				}
			}

			return verbs;
		}

		private static IFileExplorerAppExtensionStatics? GetStatics()
		{
			try
			{
				PInvoke.WindowsCreateString(RuntimeClassName, (uint)RuntimeClassName.Length, out var className).ThrowOnFailure();
				using (className)
				{
					PInvoke.RoGetActivationFactory(className, out IFileExplorerAppExtensionStatics statics).ThrowOnFailure();
					return statics;
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Windows 11 context menu commands are not available on this version of Windows.");
				return null;
			}
		}

		private static void AppendVerbs(IFileExplorerAppExtensionStatics statics, string itemType, List<Verb> verbs)
		{
			if (statics.GetExtensions(itemType, WaitForRegistrations, out var extensions).Failed)
				return;

			// Newer builds of Windows return the second revision of the interfaces; the first one has no GetIsL1Default.
			if (extensions is IFileExplorerAppExtensionVectorView2 vector2 && vector2.GetSize(out var count2).Succeeded)
			{
				for (uint i = 0; i < count2; i++)
				{
					if (vector2.GetAt(i, out var extension).Succeeded && extension?.GetVerbs(WaitForRegistrations, out var extensionVerbs).Succeeded is true)
						AppendVerbs(extensionVerbs, itemType, verbs);
				}
			}
			else if (extensions is IFileExplorerAppExtensionVectorView vector && vector.GetSize(out var count).Succeeded)
			{
				for (uint i = 0; i < count; i++)
				{
					if (vector.GetAt(i, out var extension).Succeeded && extension?.GetVerbs(WaitForRegistrations, out var extensionVerbs).Succeeded is true)
						AppendVerbs(extensionVerbs, itemType, verbs);
				}
			}
		}

		private static void AppendVerbs(IPropertySetVectorView? extensionVerbs, string itemType, List<Verb> verbs)
		{
			if (extensionVerbs is null || extensionVerbs.GetSize(out var count).Failed)
				return;

			for (uint i = 0; i < count; i++)
			{
				if (extensionVerbs.GetAt(i, out var verb).Succeeded &&
					verb is IStringInspectableMap properties &&
					properties.Lookup("Verb", out var value).Succeeded &&
					value is IPropertyValue propertyValue &&
					propertyValue.GetGuid(out var clsid).Succeeded)
				{
					verbs.Add(new(clsid, itemType));
				}
			}
		}
	}
}
