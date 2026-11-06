// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Storage.Interop;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Windows.Foundation.Collections;
using Windows.Win32.Foundation;
using WinRT;
using IInspectable = Files.Storage.Interop.IInspectable;

namespace Files.App.Server;

internal readonly record struct ExplorerCommandRegistration(Guid ClassId, string Verb, string DisplayName, string GroupId, bool IsCloudProvider = false, string? GroupIcon = null);

internal static class FileExplorerAppExtensions
{
	// This undocumented API has multiple ABI revisions. Query only the known interface IIDs.
	// Flag 1's cache-wait behavior is inferred from Explorer, not a public contract.
	private const uint WaitForStateRepository = 1;

	// One discovery STA serves the process, including calls still pending after a menu times out.
	private static readonly ThreadWithMessageQueue discoveryThread = new();
	private static readonly ConcurrentDictionary<string, Lazy<Task<HashSet<ExplorerCommandRegistration>>>> pending = new(StringComparer.OrdinalIgnoreCase);

	internal static async Task<HashSet<ExplorerCommandRegistration>> GetCommandsAsync(string[] itemTypes)
	{
		var results = await Task.WhenAll(itemTypes.Distinct(StringComparer.OrdinalIgnoreCase).Select(GetCommandsAsync));
		return [.. results.SelectMany(commands => commands)];
	}

	private static async Task<HashSet<ExplorerCommandRegistration>> GetCommandsAsync(string itemType)
	{
		var entry = pending.GetOrAdd(itemType, static type => new(() => discoveryThread.PostMethod(() => GetCommands([type]))));
		try
		{
			return await entry.Value;
		}
		finally
		{
			pending.TryRemove(KeyValuePair.Create(itemType, entry));
		}
	}

	private static HashSet<ExplorerCommandRegistration> GetCommands(string[] itemTypes)
	{
		var commands = new HashSet<ExplorerCommandRegistration>();
		using var factoryReference = ActivationFactory.Get("Windows.Internal.FileExplorerAppExtension", typeof(IFileExplorerAppExtensionStatics).GUID);
		var factory = factoryReference.AsInterface<IFileExplorerAppExtensionStatics>();
		foreach (var itemType in itemTypes.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			try
			{
				if (factory.GetExtensions(itemType, WaitForStateRepository, out var vector).Failed)
					continue;

				if (vector is IFileExplorerAppExtensionVectorView2 current && current.GetSize(out var count).Succeeded)
				{
					for (uint index = 0; index < count; index++)
					{
						if (current.GetAt(index, out var extension).Succeeded)
							ReadExtension(extension, commands);
					}
				}
				else if (vector is IFileExplorerAppExtensionVectorView original && original.GetSize(out count).Succeeded)
				{
					for (uint index = 0; index < count; index++)
					{
						if (original.GetAt(index, out var extension).Succeeded)
							ReadExtension(extension, commands);
					}
				}
			}
			catch (Exception ex)
			{
				Program.Logger.LogDebug(ex, "Could not read packaged context menu registrations for {ItemType}.", itemType);
			}
		}

		return commands;
	}

	[DynamicWindowsRuntimeCast(typeof(ValueSet))]
	private static void ReadExtension(IInspectable? extension, HashSet<ExplorerCommandRegistration> commands)
	{
		try
		{
			IValueSetIterable? verbs;
			string? displayName;
			string? packageFullName;
			HRESULT result;
			if (extension is IFileExplorerAppExtension2 current)
			{
				current.GetDisplayName(out displayName);
				current.GetPackageFullName(out packageFullName).ThrowOnFailure();
				result = current.GetVerbs(WaitForStateRepository, out verbs);
			}
			else if (extension is IFileExplorerAppExtension original)
			{
				original.GetDisplayName(out displayName);
				original.GetPackageFullName(out packageFullName).ThrowOnFailure();
				result = original.GetVerbs(WaitForStateRepository, out verbs);
			}
			else
			{
				return;
			}

			if (result.Failed || verbs is null || string.IsNullOrEmpty(packageFullName))
				return;

			var groupId = $"package:{packageFullName}";
			verbs.First(out var iterator).ThrowOnFailure();
			if (iterator is null)
				return;

			iterator.GetHasCurrent(out var hasCurrent).ThrowOnFailure();
			while (hasCurrent)
			{
				iterator.GetCurrent(out var valueSet).ThrowOnFailure();
				if (valueSet is ValueSet properties && properties.TryGetValue("Verb", out var value) && value is Guid classId && classId != Guid.Empty)
				{
					properties.TryGetValue("Id", out var id);
					commands.Add(new(classId, id as string ?? string.Empty, displayName ?? string.Empty, groupId));
				}

				iterator.MoveNext(out hasCurrent).ThrowOnFailure();
			}
		}
		catch (Exception ex)
		{
			Program.Logger.LogDebug(ex, "Could not read a packaged context menu extension.");
		}
	}
}
