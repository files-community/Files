// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers
{
	internal sealed class IntervalSampler
	{
		private readonly TimeSpan sampleInterval;
		private DateTime nextRecordPoint;

		public IntervalSampler(int millisecondsInterval) : this(TimeSpan.FromMilliseconds(millisecondsInterval))
		{
		}

		public IntervalSampler(TimeSpan interval)
		{
			sampleInterval = interval;
			Reset();
		}

		public void Reset()
		{
			nextRecordPoint = DateTime.UtcNow + sampleInterval;
		}

		public bool CheckNow()
		{
			var utcNow = DateTime.UtcNow;
			if (utcNow >= nextRecordPoint)
			{
				Reset();
				return true;
			}
			return false;
		}
	}
}