// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils;
using Microsoft.UI.Dispatching;
using System.Collections.Generic;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Manages a single, application-wide timer for updating relative date strings in file properties.
	/// This centralizes the logic for date updates, reducing overhead compared to individual timers.
	/// </summary>
	public sealed class FileProperties
	{
		private static readonly List<ListedItem> _items = new();
		private static readonly DispatcherQueueTimer _timer = MainWindow.Instance.DispatcherQueue.CreateTimer();
		private static readonly object _lock = new();

		static FileProperties()
		{
			_timer.Interval = TimeSpan.FromSeconds(1);
			_timer.Tick += Timer_Tick;
			_timer.Start();
		}

		/// <summary>
		/// Registers a ListedItem for potential future relative date updates.
		/// </summary>
		/// <param name="item">The item to register for updates</param>
		public static void TryAdd(ListedItem item)
		{
			if (item is null)
				return;

			lock (_lock)
			{
				if (!_items.Contains(item))
					_items.Add(item);
			}
		}

		/// <summary>
		/// Removes a ListedItem from the update list.
		/// </summary>
		/// <param name="item">The item to remove</param>
		public static void Remove(ListedItem item)
		{
			if (item is null)
				return;

			lock (_lock)
			{
				_items.Remove(item);
			}
		}

		private static void Timer_Tick(object? sender, object e)
		{
			if (App.AppModel.IsMainWindowClosed)
				return;

			List<ListedItem> itemsToUpdate;
			lock (_lock)
			{
				// Only update items that have date differences (within the last 7 days)
				itemsToUpdate = _items.FindAll(item => 
					IsDateDiff(item.ItemDateAccessedReal) ||
					IsDateDiff(item.ItemDateCreatedReal) ||
					IsDateDiff(item.ItemDateModifiedReal) ||
					(item is RecycleBinItem recycleBinItem && IsDateDiff(recycleBinItem.ItemDateDeletedReal)) ||
					(item is IGitItem gitItem && gitItem.GitLastCommitDate is DateTimeOffset offset && IsDateDiff(offset)));
			}

			// Update the date strings for items that need it
			foreach (var item in itemsToUpdate)
			{
				item.UpdateDateModified();
				item.UpdateDateCreated();
				item.UpdateDateAccessed();

				if (item is RecycleBinItem recycleBinItem)
				{
					// Trigger property change for deleted date
					recycleBinItem.ItemDateDeletedReal = recycleBinItem.ItemDateDeletedReal;
				}

				if (item is IGitItem gitItem && gitItem.GitLastCommitDate.HasValue)
				{
					// Trigger property change for git commit date
					gitItem.GitLastCommitDate = gitItem.GitLastCommitDate;
				}
			}
		}

		private static bool IsDateDiff(DateTimeOffset offset) => (DateTimeOffset.Now - offset).TotalDays < 7;
	}
}