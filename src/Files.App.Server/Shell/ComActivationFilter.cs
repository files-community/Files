// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Files.App.Server;

/// <summary>
/// Refuses the activation of some COM classes on the current thread, so it doesn't wait for their servers.
/// </summary>
internal sealed partial class ComActivationFilter : IDisposable
{
	// COM takes a single filter for the whole process
	private static readonly Filter? registeredFilter = Filter.Register();

	[ThreadStatic]
	private static Func<Guid, bool>? isRefused;

	private readonly Func<Guid, bool>? previous;

	// Must be created and disposed on the thread making the activations
	internal ComActivationFilter(Func<Guid, bool> isClassRefused)
	{
		previous = isRefused;
		if (registeredFilter is not null)
			isRefused = isClassRefused;
	}

	public void Dispose() => isRefused = previous;

	[GeneratedComClass]
	private sealed unsafe partial class Filter : IActivationFilter
	{
		internal static Filter? Register()
		{
			var filter = new Filter();
			var result = PInvoke.CoRegisterActivationFilter(filter);
			if (result.Succeeded)
				return filter;

			Program.Logger.LogDebug("Could not register the COM activation filter: {Result}.", result);
			return null;
		}

		public HRESULT HandleActivation(uint dwActivationType, Guid* rclsid, Guid* pReplacementClsId)
		{
			*pReplacementClsId = *rclsid;
			return isRefused?.Invoke(*rclsid) is true ? HRESULT.CLASS_E_CLASSNOTAVAILABLE : HRESULT.S_OK;
		}
	}
}
