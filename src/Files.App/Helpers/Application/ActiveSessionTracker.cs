// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Sentry;
using Windows.Storage;

namespace Files.App.Helpers
{
	/// <summary>
	/// Accumulates the time the main window is in the foreground so active usage can be
	/// measured separately from how long the process stays alive. Totals are persisted
	/// on deactivation and reported on the next launch, so a crash cannot lose them and
	/// nothing has to run during teardown.
	/// </summary>
	public static class ActiveSessionTracker
	{
		public const string TransactionName = "active-session";
		public const string TransactionOperation = "app.active";

		private const string ActiveTimeKey = "ActiveSessionTimeSeconds";
		private const string StretchCountKey = "ActiveSessionStretchCount";

		private static readonly Stopwatch _stretchStopwatch = new();

		/// <summary>
		/// Records main window activation changes. Must be called from the UI thread.
		/// </summary>
		public static void OnActivationChanged(bool isActive)
		{
			if (isActive)
			{
				if (!_stretchStopwatch.IsRunning)
					_stretchStopwatch.Restart();

				return;
			}

			if (!_stretchStopwatch.IsRunning)
				return;

			var elapsed = _stretchStopwatch.Elapsed;
			_stretchStopwatch.Reset();

			var values = ApplicationData.Current.LocalSettings.Values;
			values.TryGetValue(ActiveTimeKey, out var totalTime);
			values.TryGetValue(StretchCountKey, out var totalStretches);
			values[ActiveTimeKey] = (totalTime as double? ?? 0d) + elapsed.TotalSeconds;
			values[StretchCountKey] = (totalStretches as int? ?? 0) + 1;
		}

		/// <summary>
		/// Reports the totals persisted by previous runs to Sentry and clears them.
		/// </summary>
		public static void ReportPersistedTime()
		{
			if (!SentrySdk.IsEnabled)
				return;

			var values = ApplicationData.Current.LocalSettings.Values;
			values.TryGetValue(ActiveTimeKey, out var totalTime);
			if (totalTime is not double activeTimeSeconds || activeTimeSeconds <= 0d)
				return;

			var transaction = SentrySdk.StartTransaction(TransactionName, TransactionOperation);
			transaction.SetMeasurement("active_time", activeTimeSeconds, MeasurementUnit.Duration.Second);
			if (values.TryGetValue(StretchCountKey, out var totalStretches) && totalStretches is int stretchCount)
				transaction.SetMeasurement("active_stretches", stretchCount);
			transaction.Finish();

			values.Remove(ActiveTimeKey);
			values.Remove(StretchCountKey);
		}
	}
}
