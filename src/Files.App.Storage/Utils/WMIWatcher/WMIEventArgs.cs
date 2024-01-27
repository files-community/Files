// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Management.Infrastructure;

namespace Files.App.Storage
{
	/// <summary>
	/// Represents <see cref="EventArgs"/> contains event data, which contains <see cref="CimSubscriptionResult"/>.
	/// </summary>
	public class WMIEventArgs : EventArgs
	{
		public CimSubscriptionResult CimSubscriptionResult { get; }

		public WMIEventArgs(CimSubscriptionResult cimSubscriptionResult)
		{
			CimSubscriptionResult = cimSubscriptionResult;
		}
	}
}
