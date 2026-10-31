// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Git
{
	public enum GitStatusCenterOperationKind
	{
		Clone,
		Fetch,
		Pull,
		Push,
		Sync,
		Checkout,
	}

	internal sealed class GitStatusCenterOperation
	{
		private readonly GitStatusCenterOperationKind _operationKind;
		private readonly string? _operationTarget;
		private readonly string _repositoryPath;
		private readonly CancellationTokenSource? _cancellationTokenSource;
		private readonly StatusCenterViewModel _statusCenterViewModel;
		private readonly StatusCenterItem _statusItem;
		private readonly StatusCenterItemProgressModel _progress;

		private int _lastReportedPercentage = -1;
		private int _isCompleted;
		private long _totalItemsCount;

		public CancellationToken CancellationToken
			=> _cancellationTokenSource?.Token ?? default;

		public GitStatusCenterOperation(
			GitStatusCenterOperationKind operationKind,
			string repositoryPath,
			bool canProvideProgress,
			string? operationTarget = null,
			bool isCancelable = false)
		{
			_operationKind = operationKind;
			_operationTarget = operationTarget;
			_repositoryPath = repositoryPath;
			_cancellationTokenSource = isCancelable ? new() : null;
			_statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();
			_statusItem = StatusCenterHelper.AddCard_GitOperation(
				operationKind,
				repositoryPath,
				ReturnResult.InProgress,
				canProvideProgress,
				operationTarget,
				operationCancellationToken: _cancellationTokenSource);
			_progress = new(
				_statusItem.ProgressEventSource,
				enumerationCompleted: true,
				FileSystemStatusCode.InProgress);
		}

		public void ReportProgress(
			long completed,
			long total,
			string? currentItem = null,
			double startPercentage = 0,
			double endPercentage = 100,
			bool reportTotalItems = false)
		{
			if (total <= 0)
				return;

			if (reportTotalItems)
			{
				_progress.ItemsCount = total;
				Interlocked.Exchange(ref _totalItemsCount, total);
			}

			var percentage = startPercentage +
				Math.Clamp(completed / (double)total, 0, 1) * (endPercentage - startPercentage);

			_progress.FileName = currentItem;
			_progress.Report(UpdateReportedPercentage(percentage));
		}

		public void Complete(ReturnResult result)
		{
			if (Interlocked.Exchange(ref _isCompleted, 1) != 0)
				return;

			_statusCenterViewModel.RemoveItem(_statusItem);
			StatusCenterHelper.AddCard_GitOperation(
				_operationKind,
				_repositoryPath,
				result,
				operationTarget: _operationTarget,
				itemsCount: Interlocked.Read(ref _totalItemsCount));
			_cancellationTokenSource?.Dispose();
		}

		private int UpdateReportedPercentage(double percentage)
		{
			var newPercentage = Math.Clamp((int)percentage, 0, 99);

			while (true)
			{
				var currentPercentage = Volatile.Read(ref _lastReportedPercentage);
				if (newPercentage <= currentPercentage)
					return currentPercentage;

				if (Interlocked.CompareExchange(ref _lastReportedPercentage, newPercentage, currentPercentage) == currentPercentage)
					return newPercentage;
			}
		}
	}
}
