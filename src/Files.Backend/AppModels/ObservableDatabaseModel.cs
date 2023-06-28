// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.AppModels;
using Files.Shared.Utils;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;

namespace Files.Core.AppModels
{
	/// <inheritdoc cref="BaseDatabaseModel{TDictionaryValue}"/>
	public abstract class ObservableDatabaseModel<TDictionaryValue> : BaseDatabaseModel<TDictionaryValue>
	{
		/// <summary>
		/// Gets the <see cref="INotifyCollectionChanged"/> used to report database changes.
		/// </summary>
		protected abstract INotifyCollectionChanged? NotifyCollectionChanged { get; }

		protected ObservableDatabaseModel(IAsyncSerializer<Stream> serializer)
			: base(serializer)
		{
		}

		private async void Settings_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			await OnCollectionChangedAsync(e);
		}

		/// <summary>
		/// Starts capturing any changes that occur to the database storage using <see cref="NotifyCollectionChanged"/>.
		/// </summary>
		/// <remarks>
		/// If the <see cref="NotifyCollectionChanged"/> is null, <see cref="NullReferenceException"/> is raised.
		/// </remarks>
		protected void StartCapturingChanges()
		{
			_ = NotifyCollectionChanged ?? throw new NullReferenceException($"{nameof(NotifyCollectionChanged)} was null.");
			NotifyCollectionChanged.CollectionChanged += Settings_CollectionChanged;
		}

		/// <summary>
		/// Captures the recent changes of the database storage.
		/// </summary>
		/// <param name="e">The <see cref="NotifyCollectionChangedEventArgs"/> class which represents the change that occurred.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		protected virtual async Task OnCollectionChangedAsync(NotifyCollectionChangedEventArgs e)
		{
			if (e.Action != NotifyCollectionChangedAction.Replace)
				return;

			if (e.NewItems?[0] is not string changedItem)
				return;

			await ProcessChangeAsync(changedItem);
		}

		/// <summary>
		/// Updates the state of this database model based on the recent storage changes.
		/// </summary>
		/// <param name="changedItem">The item that was changed.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		protected abstract Task ProcessChangeAsync(string changedItem);

		/// <inheritdoc/>
		public override void Dispose()
		{
			if (NotifyCollectionChanged is null)
				return;

			NotifyCollectionChanged.CollectionChanged -= Settings_CollectionChanged;
		}
	}
}
