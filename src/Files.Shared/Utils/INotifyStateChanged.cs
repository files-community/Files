using System;

namespace Files.Shared.Utils
{
	/// <summary>
	/// Provides a contract for objects that need to notify subscribers when their state changes.
	/// </summary>
	public interface INotifyStateChanged
	{
		/// <summary>
		/// Occurs when a state of an object changes.
		/// </summary>
		/// <remarks>
		/// Subscribers can register to this event in order to receive notifications when the state of the object changes.
		/// The <see cref="StateChanged"/> event may contain additional information about the new state in the event arguments.
		/// </remarks>
		event EventHandler<EventArgs>? StateChanged;
	}
}
