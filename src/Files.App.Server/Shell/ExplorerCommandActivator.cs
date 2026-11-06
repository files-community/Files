// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Storage.Interop;
using System.Collections.Concurrent;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.System.WinRT;

namespace Files.App.Server;

internal static class ExplorerCommandActivator
{
	private static readonly ConcurrentDictionary<Guid, Lazy<Task<ClassObject?>>> classObjects = new();

	// The command belongs to the calling thread's apartment
	internal static IExplorerCommand? Create(Guid classId)
	{
		var entry = classObjects.GetOrAdd(classId, static id => new(() => LockClassObjectAsync(id)));
		var classObject = entry.Value;
		if (classObject.IsCompleted)
		{
			if (classObject.Result?.CreateCommand() is { } command)
				return command;

			// Not a local server, or it stopped, for example for an app update
			if (classObjects.TryRemove(KeyValuePair.Create(classId, entry)))
				classObject.Result?.Unlock();
		}

		return CreateInstance(classId);
	}

	internal static async Task PrepareAsync(IEnumerable<Guid> classIds)
	{
		foreach (var classId in classIds)
			await classObjects.GetOrAdd(classId, static id => new(() => LockClassObjectAsync(id))).Value;
	}

	// In the MTA, as the STA workers come and go
	private static Task<ClassObject?> LockClassObjectAsync(Guid classId)
		=> Task.Run(() => SafetyExtensions.IgnoreExceptions(() => ClassObject.Create(classId), Program.Logger));

	private static unsafe IExplorerCommand? CreateInstance(Guid classId)
	{
		// Activate through a raw pointer so the command gets a uniquely owned wrapper
		Guid commandId = typeof(IExplorerCommand).GUID;
		MULTI_QI[] results = [new() { pIID = &commandId }];
		if (PInvoke.CoCreateInstanceEx(classId, null, CLSCTX.CLSCTX_INPROC_SERVER | CLSCTX.CLSCTX_LOCAL_SERVER, null, results).Failed || results[0].hr.Failed)
			return null;

		try
		{
			return UniqueComInterfaceMarshaller<IExplorerCommand>.ConvertToManaged(results[0].pItf);
		}
		finally
		{
			UniqueComInterfaceMarshaller<IExplorerCommand>.Free(results[0].pItf);
		}
	}

	private sealed class ClassObject(IUniqueClassFactory classFactory, IUniqueAgileReference reference)
	{
		// On an MTA thread
		internal static unsafe ClassObject? Create(Guid classId)
		{
			if (PInvoke.CoGetClassObject(classId, CLSCTX.CLSCTX_LOCAL_SERVER, null, out IUniqueClassFactory classFactory).Failed
				|| classFactory.LockServer(true).Failed)
				return null;

			if (PInvoke.RoGetAgileReference(AgileReferenceOptions.AGILEREFERENCE_DEFAULT, typeof(IUniqueClassFactory).GUID, classFactory, out var reference).Failed)
			{
				classFactory.LockServer(false);
				return null;
			}

			return new(classFactory, (IUniqueAgileReference)reference);
		}

		internal IExplorerCommand? CreateCommand()
		{
			if (reference.Resolve(typeof(IUniqueClassFactory).GUID, out var resolved).Failed || resolved is not IUniqueClassFactory localClassFactory)
			{
				Release(resolved);
				return null;
			}

			try
			{
				if (localClassFactory.CreateInstance(null, typeof(IExplorerCommand).GUID, out var instance).Succeeded && instance is IExplorerCommand command)
					return command;

				Release(instance);
				return null;
			}
			finally
			{
				Release(localClassFactory);
			}
		}

		internal void Unlock() => _ = Task.Run(() => classFactory.LockServer(false));

		private static void Release(object? value)
		{
			if (value is ComObject comObject)
				comObject.FinalRelease();
		}
	}
}
