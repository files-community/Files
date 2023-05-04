// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments
{
	public class ItemLoadStatusChangedEventArgs : EventArgs
	{
		public enum ItemLoadStatus
		{
			/// <summary>
			/// Load is starting.
			/// </summary>
			Starting,

			/// <summary>
			/// Load is in progress.
			/// </summary>
			InProgress,

			/// <summary>
			/// Load is competed.
			/// </summary>
			Complete
		}

		public ItemLoadStatus Status { get; set; }

		/// <summary>
		/// This property may not be provided consistently if Status is not Complete
		/// </summary>
		public string? PreviousDirectory { get; set; }

		/// <summary>
		/// This property may not be provided consistently if Status is not Complete
		/// </summary>
		public string? Path { get; set; }
	}
}
