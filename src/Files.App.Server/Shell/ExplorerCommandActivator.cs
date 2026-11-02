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
	private static readonly ConcurrentDictionary<Guid, Task<ClassObject?>> classObjects = new();

	// The command belongs to the calling thread's apartment
	internal static IExplorerCommand? Create(Guid classId)
	{
		var classObject = classObjects.GetOrAdd(classId, LockClassObjectAsync);
		if (classObject.IsCompleted)
		{
			if (classObject.Result?.CreateCommand() is { } command)
				return command;

			// Not a local server, or it stopped, for example for an app update
			if (classObjects.TryRemove(KeyValuePair.Create(classId, classObject)))
				classObject.Result?.Unlock();
		}

		return CreateInstance(classId);
	}

	internal static async Task PrepareAsync(IEnumerable<Guid> classIds)
	{
		foreach (var classId in classIds)
			await classObjects.GetOrAdd(classId, LockClassObjectAsync);
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

	private sealed class ClassObject(IClassFactory classFactory, IAgileReference reference)
	{
		// On an MTA thread
		internal static unsafe ClassObject? Create(Guid classId)
		{
			Guid classFactoryId = typeof(IClassFactory).GUID;
			if (PInvoke.CoGetClassObject(&classId, CLSCTX.CLSCTX_LOCAL_SERVER, null, &classFactoryId, out var value).Failed
				|| value is not IClassFactory classFactory
				|| classFactory.LockServer(true).Failed)
				return null;

			if (PInvoke.RoGetAgileReference(AgileReferenceOptions.AGILEREFERENCE_DEFAULT, classFactoryId, classFactory, out var reference).Failed)
			{
				classFactory.LockServer(false);
				return null;
			}

			return new(classFactory, reference);
		}

		internal unsafe IExplorerCommand? CreateCommand()
		{
			Guid classFactoryId = typeof(IClassFactory).GUID;
			if (reference.Resolve(&classFactoryId, out var classFactoryPointer).Failed)
				return null;

			var localClassFactory = UniqueComInterfaceMarshaller<IClassFactory>.ConvertToManaged(classFactoryPointer);
			UniqueComInterfaceMarshaller<IClassFactory>.Free(classFactoryPointer);
			try
			{
				Guid commandId = typeof(IExplorerCommand).GUID;
				if (localClassFactory is null || localClassFactory.CreateInstance(null, &commandId, out var commandPointer).Failed)
					return null;

				try
				{
					return UniqueComInterfaceMarshaller<IExplorerCommand>.ConvertToManaged(commandPointer);
				}
				finally
				{
					UniqueComInterfaceMarshaller<IExplorerCommand>.Free(commandPointer);
				}
			}
			finally
			{
				if ((object?)localClassFactory is ComObject comObject)
					comObject.FinalRelease();
			}
		}

		internal void Unlock()
			=> _ = Task.Run(() => classFactory.LockServer(false));
	}
}
