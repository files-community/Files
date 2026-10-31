// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Files.InteractionTests.Tests
{
	[TestClass]
	public sealed class ArchiveReadProgressStreamTests
	{
		[TestMethod]
		public void MetadataReadsDoNotReportProgress()
		{
			var reports = new List<double>();
			using var stream = new ArchiveReadProgressStream(new MemoryStream(new byte[100]), reports.Add, CancellationToken.None);
			stream.ReadByte();
			Assert.AreEqual(0, reports.Count);
		}

		[TestMethod]
		public void ReadsReportProgressButSeeksAndBackwardsReadsDoNot()
		{
			var reports = new List<double>();
			using var stream = new ArchiveReadProgressStream(new MemoryStream(new byte[100]), reports.Add, CancellationToken.None);
			stream.StartReporting();
			stream.Seek(49, SeekOrigin.Begin);
			Assert.AreEqual(0, reports.Count);
			stream.ReadByte();
			stream.Seek(0, SeekOrigin.Begin);
			stream.ReadByte();
			CollectionAssert.AreEqual(new[] { 50d }, reports);
		}

		[TestMethod]
		public void ReadingToTheEndDoesNotReportCompletion()
		{
			var reports = new List<double>();
			using var stream = new ArchiveReadProgressStream(new MemoryStream(new byte[100]), reports.Add, CancellationToken.None);
			stream.StartReporting();
			stream.Read(new byte[50]);
			stream.Read(new byte[50]);
			stream.ReadByte();
			CollectionAssert.AreEqual(new[] { 50d, 99d }, reports);
		}

		[TestMethod]
		public void EmptyInputDoesNotReportInvalidPercentage()
		{
			var reports = new List<double>();
			using var stream = new ArchiveReadProgressStream(new MemoryStream(), reports.Add, CancellationToken.None);
			stream.StartReporting();
			Assert.AreEqual(-1, stream.ReadByte());
			Assert.AreEqual(0, reports.Count);
		}

		[TestMethod]
		public void CancellationStopsTheNextRead()
		{
			using var cancellation = new CancellationTokenSource();
			using var stream = new ArchiveReadProgressStream(new MemoryStream(new byte[100]), _ => { }, cancellation.Token);
			stream.StartReporting();
			cancellation.Cancel();
			Assert.Throws<OperationCanceledException>(() => stream.ReadByte());
		}

		[TestMethod]
		public void DisposeClosesTheOwnedStream()
		{
			var input = new MemoryStream(new byte[100]);
			var stream = new ArchiveReadProgressStream(input, _ => { }, CancellationToken.None);
			stream.Dispose();
			stream.Dispose();
			Assert.IsFalse(input.CanRead);
		}
	}
}
