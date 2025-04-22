// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using SevenZip;
using System.IO;
using System.Text;
using UtfUnknown;
using Windows.Storage;
using Windows.Win32;

namespace Files.App.Services
{
	/// <inheritdoc cref="IStorageArchiveService"/>
	public class StorageArchiveService : IStorageArchiveService
	{
		private StatusCenterViewModel StatusCenterViewModel { get; } = Ioc.Default.GetRequiredService<StatusCenterViewModel>();
		private IThreadingService ThreadingService { get; } = Ioc.Default.GetRequiredService<IThreadingService>();

		/// <inheritdoc/>
		public bool CanCompress(IReadOnlyList<ListedItem> items)
		{
			return
				CanDecompress(items) is false ||
				items.Count > 1;
		}

		/// <inheritdoc/>
		public bool CanDecompress(IReadOnlyList<ListedItem> items)
		{
			return
				items.Any() &&
				(items.All(x => x.IsArchive) ||
				items.All(x =>
					x.PrimaryItemAttribute == StorageItemTypes.File &&
					FileExtensionHelpers.IsZipFile(x.FileExtension)));
		}

		/// <inheritdoc/>
		public async Task<bool> CompressAsync(ICompressArchiveModel compressionModel)
		{
			var archivePath = compressionModel.GetArchivePath();

			int index = 1;

			while (SystemIO.File.Exists(archivePath) || SystemIO.Directory.Exists(archivePath))
				archivePath = compressionModel.GetArchivePath($" ({++index})");

			compressionModel.ArchivePath = archivePath;

			var banner = StatusCenterHelper.AddCard_Compress(
				compressionModel.Sources,
				archivePath.CreateEnumerable(),
				ReturnResult.InProgress,
				compressionModel.Sources.Count());

			compressionModel.Progress = banner.ProgressEventSource;
			compressionModel.CancellationToken = banner.CancellationToken;

			bool isSuccess = await compressionModel.RunCreationAsync();

			StatusCenterViewModel.RemoveItem(banner);

			if (isSuccess)
			{
				StatusCenterHelper.AddCard_Compress(
					compressionModel.Sources,
					archivePath.CreateEnumerable(),
					ReturnResult.Success,
					compressionModel.Sources.Count());
			}
			else
			{
				PInvoke.DeleteFileFromApp(archivePath);

				StatusCenterHelper.AddCard_Compress(
					compressionModel.Sources,
					archivePath.CreateEnumerable(),
					compressionModel.CancellationToken.IsCancellationRequested
						? ReturnResult.Cancelled
						: ReturnResult.Failed,
					compressionModel.Sources.Count());
			}

			return isSuccess;
		}

		/// <inheritdoc/>
		public Task<bool> DecompressAsync(string archiveFilePath, string destinationFolderPath, string password = "", Encoding? encoding = null)
		{
			if (encoding == null)
			{
				return DecompressAsyncWithSevenZip(archiveFilePath, destinationFolderPath, password);
			}
			else
			{
				return DecompressAsyncWithSharpZipLib(archiveFilePath, destinationFolderPath, password, encoding);
			}
		}
		async Task<bool> DecompressAsyncWithSevenZip(string archiveFilePath, string destinationFolderPath, string password = "")
		{
			if (string.IsNullOrEmpty(archiveFilePath) ||
				string.IsNullOrEmpty(destinationFolderPath))
				return false;

			using var zipFile = await GetSevenZipExtractorAsync(archiveFilePath, password);
			if (zipFile is null)
				return false;

			// Initialize a new in-progress status card
			var statusCard = StatusCenterHelper.AddCard_Decompress(
				archiveFilePath.CreateEnumerable(),
				destinationFolderPath.CreateEnumerable(),
				ReturnResult.InProgress);

			// Check if the decompress operation canceled
			if (statusCard.CancellationToken.IsCancellationRequested)
				return false;

			StatusCenterItemProgressModel fsProgress = new(
				statusCard.ProgressEventSource,
				enumerationCompleted: true,
				FileSystemStatusCode.InProgress,
				zipFile.ArchiveFileData.Count(x => !x.IsDirectory));

			fsProgress.TotalSize = zipFile.ArchiveFileData.Select(x => (long)x.Size).Sum();
			fsProgress.Report();

			bool isSuccess = false;

			try
			{
				// TODO: Get this method return result
				await zipFile.ExtractArchiveAsync(destinationFolderPath);

				isSuccess = true;
			}
			catch
			{
				isSuccess = false;
			}
			finally
			{
				// Remove the in-progress status card
				StatusCenterViewModel.RemoveItem(statusCard);

				if (isSuccess)
				{
					// Successful
					StatusCenterHelper.AddCard_Decompress(
						archiveFilePath.CreateEnumerable(),
						destinationFolderPath.CreateEnumerable(),
						ReturnResult.Success);
				}
				else
				{
					// Error
					StatusCenterHelper.AddCard_Decompress(
						archiveFilePath.CreateEnumerable(),
						destinationFolderPath.CreateEnumerable(),
						statusCard.CancellationToken.IsCancellationRequested
							? ReturnResult.Cancelled
							: ReturnResult.Failed);
				}
			}

			zipFile.Extracting += (s, e) =>
			{
				if (fsProgress.TotalSize > 0)
					fsProgress.Report(e.BytesProcessed / (double)fsProgress.TotalSize * 100);
			};

			zipFile.FileExtractionStarted += (s, e) =>
			{
				if (statusCard.CancellationToken.IsCancellationRequested)
					e.Cancel = true;

				if (!e.FileInfo.IsDirectory)
				{
					ThreadingService.ExecuteOnUiThreadAsync(() =>
					{
						fsProgress.FileName = e.FileInfo.FileName;
						fsProgress.Report();
					});
				}
			};

			zipFile.FileExtractionFinished += (s, e) =>
			{
				if (!e.FileInfo.IsDirectory)
				{
					fsProgress.AddProcessedItemsCount(1);
					fsProgress.Report();
				}
			};
			return isSuccess;
		}

		async Task<bool> DecompressAsyncWithSharpZipLib(string archiveFilePath, string destinationFolderPath, string password, Encoding encoding)
		{
			if (string.IsNullOrEmpty(archiveFilePath) ||
				string.IsNullOrEmpty(destinationFolderPath))
				return false;
			using var zipFile = new ZipFile(archiveFilePath, StringCodec.FromEncoding(encoding));
			if (zipFile is null)
				return false;

			if (!string.IsNullOrEmpty(password))
				zipFile.Password = password;

			// Initialize a new in-progress status card
			var statusCard = StatusCenterHelper.AddCard_Decompress(
				archiveFilePath.CreateEnumerable(),
				destinationFolderPath.CreateEnumerable(),
				ReturnResult.InProgress);

			// Check if the decompress operation canceled
			if (statusCard.CancellationToken.IsCancellationRequested)
				return false;

			StatusCenterItemProgressModel fsProgress = new(
				statusCard.ProgressEventSource,
				enumerationCompleted: true,
				FileSystemStatusCode.InProgress,
				zipFile.Cast<ZipEntry>().Count<ZipEntry>(x => !x.IsDirectory));
			fsProgress.TotalSize = zipFile.Cast<ZipEntry>().Select(x => (long)x.Size).Sum();
			fsProgress.Report();

			bool isSuccess = false;

			try
			{
				long processedBytes = 0;
				int processedFiles = 0;
				await Task.Run(async () =>
				{
					foreach (ZipEntry zipEntry in zipFile)
					{
						if (statusCard.CancellationToken.IsCancellationRequested)
						{
							isSuccess = false;
							break;
						}

						if (!zipEntry.IsFile)
						{
							continue; // Ignore directories
						}

						string entryFileName = zipEntry.Name;
						string fullZipToPath = Path.Combine(destinationFolderPath, entryFileName);
						string directoryName = Path.GetDirectoryName(fullZipToPath);

						if (!Directory.Exists(directoryName))
						{
							Directory.CreateDirectory(directoryName);
						}

						byte[] buffer = new byte[4096]; // 4K is a good default
						using (Stream zipStream = zipFile.GetInputStream(zipEntry))
						using (FileStream streamWriter = File.Create(fullZipToPath))
						{
							await ThreadingService.ExecuteOnUiThreadAsync(() =>
							{
								fsProgress.FileName = entryFileName;
								fsProgress.Report();
							});

							StreamUtils.Copy(zipStream, streamWriter, buffer);
						}
						processedBytes += zipEntry.Size;
						if (fsProgress.TotalSize > 0)
						{
							fsProgress.Report(processedBytes / (double)fsProgress.TotalSize * 100);
						}
						processedFiles++;
						fsProgress.AddProcessedItemsCount(1);
						fsProgress.Report();
					}
				});
				if (!statusCard.CancellationToken.IsCancellationRequested)
				{
					isSuccess = true;
				}
			}
			catch (Exception ex)
			{
				isSuccess = false;
				Console.WriteLine($"Error during decompression: {ex.Message}");
			}
			finally
			{
				// Remove the in-progress status card
				StatusCenterViewModel.RemoveItem(statusCard);

				if (isSuccess)
				{
					// Successful
					StatusCenterHelper.AddCard_Decompress(
						archiveFilePath.CreateEnumerable(),
						destinationFolderPath.CreateEnumerable(),
						ReturnResult.Success);
				}
				else
				{
					// Error
					StatusCenterHelper.AddCard_Decompress(
						archiveFilePath.CreateEnumerable(),
						destinationFolderPath.CreateEnumerable(),
						statusCard.CancellationToken.IsCancellationRequested
							? ReturnResult.Cancelled
							: ReturnResult.Failed);
				}

				if (zipFile != null)
				{
					zipFile.IsStreamOwner = true; // Makes close also close the underlying stream
					zipFile.Close();
				}
			}
			return isSuccess;
		}


		/// <inheritdoc/>
		public string GenerateArchiveNameFromItems(IReadOnlyList<ListedItem> items)
		{
			if (!items.Any())
				return string.Empty;

			return
				SystemIO.Path.GetFileName(
					items.Count is 1
						? items[0].ItemPath
						: SystemIO.Path.GetDirectoryName(items[0].ItemPath))
					?? string.Empty;
		}

		/// <inheritdoc/>
		public async Task<bool> IsEncryptedAsync(string archiveFilePath)
		{
			using SevenZipExtractor? zipFile = await GetSevenZipExtractorAsync(archiveFilePath);
			if (zipFile is null)
				return true;

			return zipFile.ArchiveFileData.Any(file => file.Encrypted || file.Method.Contains("Crypto") || file.Method.Contains("AES"));
		}

		/// <inheritdoc/>
		public async Task<bool> IsEncodingUndeterminedAsync(string archiveFilePath)
		{
			if (archiveFilePath is null) return false;
			if (Path.GetExtension(archiveFilePath) != ".zip") return false;
			try
			{
				using (ZipFile zipFile = new ZipFile(archiveFilePath))
				{
					return !zipFile.Cast<ZipEntry>().All(entry => entry.IsUnicodeText);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"SharpZipLib error: {ex.Message}");
				return true;
			}
		}

		public async Task<Encoding?> DetectEncodingAsync(string archiveFilePath)
		{
			//Temporarily using cp437 to decode zip file
			//because SharpZipLib requires an encoding when decoding
			//and cp437 contains all bytes as character
			//which means that we can store any byte array as cp437 string losslessly
			var cp437 = Encoding.GetEncoding(437);
			try
			{
				using (ZipFile zipFile = new ZipFile(archiveFilePath, StringCodec.FromEncoding(cp437)))
				{
					var fileNameBytes = cp437.GetBytes(
						String.Join("\n",
							zipFile.Cast<ZipEntry>()
								.Where(e => !e.IsUnicodeText)
								.Select(e => e.Name)
						)
					);
					var detectionResult = CharsetDetector.DetectFromBytes(fileNameBytes);
					if (detectionResult.Detected != null && detectionResult.Detected.Confidence > 0.5)
					{
						return detectionResult.Detected.Encoding;
					}
					else
					{
						return null;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"SharpZipLib error: {ex.Message}");
				return null;
			}
		}

		/// <inheritdoc/>
		public async Task<SevenZipExtractor?> GetSevenZipExtractorAsync(string archiveFilePath, string password = "")
		{
			return await FilesystemTasks.Wrap(async () =>
			{
				BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(archiveFilePath);

				var extractor = new SevenZipExtractor(await archive.OpenStreamForReadAsync(), password);

				// Force to load archive (1665013614u)
				return extractor?.ArchiveFileData is null ? null : extractor;
			});
		}
	}
}
