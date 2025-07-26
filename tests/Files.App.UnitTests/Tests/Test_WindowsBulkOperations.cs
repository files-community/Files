// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Com;

namespace Files.App.UnitTests.Tests
{
	[STATestClass]
	public unsafe class Test_WindowsBulkOperations
	{
		[STATestMethod]
		public void Test_WindowsBulkOperations_WithoutSink_AllOps()
		{
			PInvoke.CoInitializeEx(null, COINIT.COINIT_APARTMENTTHREADED);

			HRESULT hr = default;
			using var bulkOperations = new WindowsBulkOperations();
			using var desktopFolder = new WindowsFolder(new Guid("{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}"));
			Assert.IsNotNull(desktopFolder, $"\"{nameof(desktopFolder)}\" was null.");

			hr = bulkOperations.QueueCreateOperation(desktopFolder, 0, "text.txt", null);
			Assert.IsTrue(hr.Succeeded, $"Failed to queue the create operation for \"{nameof(desktopFolder)}\": {hr}");
			hr = bulkOperations.PerformAllOperations();
			Assert.IsTrue(hr.Succeeded, $"Failed to perform the create operation: {hr}");

			using var txtFile = WindowsStorable.TryParse("::{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}\\text.txt");
			Assert.IsNotNull(txtFile, $"\"{nameof(txtFile)}\" was null.");
			hr = bulkOperations.QueueRenameOperation(txtFile, "text_renamed.txt");
			Assert.IsTrue(hr.Succeeded, $"Failed to queue the rename operation for \"{nameof(txtFile)}\": {hr}");
			hr = bulkOperations.PerformAllOperations();
			Assert.IsTrue(hr.Succeeded, $"Failed to perform the rename operation: {hr}");

			using var renamedTxtFile = WindowsStorable.TryParse("::{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}\\text_renamed.txt");
			Assert.IsNotNull(renamedTxtFile, $"\"{nameof(renamedTxtFile)}\" was null.");
			using var downloadsFolder = new WindowsFolder(new Guid("{374DE290-123F-4565-9164-39C4925E467B}"));
			Assert.IsNotNull(downloadsFolder, $"\"{nameof(downloadsFolder)}\" was null.");
			hr = bulkOperations.QueueCopyOperation(renamedTxtFile, downloadsFolder, "text_renamed_copied.txt");
			Assert.IsTrue(hr.Succeeded, $"Failed to queue the copy operation for \"{nameof(renamedTxtFile)}\": {hr}");
			hr = bulkOperations.PerformAllOperations();
			Assert.IsTrue(hr.Succeeded, $"Failed to perform the copy operation: {hr}");

			hr = bulkOperations.QueueDeleteOperation(renamedTxtFile);
			Assert.IsTrue(hr.Succeeded, $"Failed to queue the delete operation for \"{nameof(renamedTxtFile)}\": {hr}");
			hr = bulkOperations.PerformAllOperations();
			Assert.IsTrue(hr.Succeeded, $"Failed to perform the delete operation: {hr}");

			using var renamedCopiedTxtFile = WindowsStorable.TryParse("::{374DE290-123F-4565-9164-39C4925E467B}\\text_renamed_copied.txt");
			Assert.IsNotNull(renamedCopiedTxtFile, $"\"{nameof(renamedCopiedTxtFile)}\" was null.");
			hr = bulkOperations.QueueMoveOperation(renamedCopiedTxtFile, desktopFolder, "text_renamed_moved.txt");
			Assert.IsTrue(hr.Succeeded, $"Failed to queue the move operation for \"{nameof(renamedCopiedTxtFile)}\": {hr}");
			hr = bulkOperations.PerformAllOperations();
			Assert.IsTrue(hr.Succeeded, $"Failed to perform the move operation: {hr}");

			using var renamedMovedTxtFile = WindowsStorable.TryParse("::{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}\\text_renamed_moved.txt");
			Assert.IsNotNull(renamedMovedTxtFile, $"\"{nameof(renamedMovedTxtFile)}\" was null.");
			hr = bulkOperations.QueueDeleteOperation(renamedMovedTxtFile);
			Assert.IsTrue(hr.Succeeded, $"Failed to queue delete operation for \"{nameof(renamedMovedTxtFile)}\": {hr}");
			hr = bulkOperations.PerformAllOperations();
			Assert.IsTrue(hr.Succeeded, $"Failed to perform the delete operation: {hr}");
		}
	}
}
