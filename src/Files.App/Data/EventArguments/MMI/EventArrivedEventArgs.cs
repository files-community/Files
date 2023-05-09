// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Management.Infrastructure;

namespace Files.App.Data.EventArguments
{
	/// <summary>
	/// CimWatcher event args, which contains CimSubscriptionResult
	/// </summary>
	public class EventArrivedEventArgs : EventArgs
	{
		public CimSubscriptionResult NewEvent { get; }

		public EventArrivedEventArgs(CimSubscriptionResult cimSubscriptionResult)
		{
			NewEvent = cimSubscriptionResult;
		}
	}
}
