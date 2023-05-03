﻿namespace Files.App.Data.EventArguments
{
	public class ItemLoadStatusChangedEventArgs : EventArgs
	{
		public enum ItemLoadStatus
		{
			Starting,

			InProgress,

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
