// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Data.Contracts;
using Files.App.Data.Enums;
using Files.App.Data.Models;
using Files.App.Services.DateTimeFormatter;
using Files.App.Utils;
using Files.App.ViewModels.UserControls.Menus;
using Microsoft.Extensions.DependencyInjection;
using OwlCore.Storage;
using Windows.Storage;

namespace Files.InteractionTests.Tests
{
	[TestClass]
	public sealed class FileTagsContextMenuViewModelTests
	{
		private static readonly Lock SyncRoot = new();

		private static bool isServicesConfigured;

		[ClassInitialize]
		public static void ClassInitialize(TestContext _)
		{
			lock (SyncRoot)
			{
				if (isServicesConfigured)
					return;

				var services = new ServiceCollection()
					.AddSingleton<IUserSettingsService, TestUserSettingsService>()
					.AddSingleton<IStartMenuService, TestStartMenuService>()
					.AddSingleton<IDateTimeFormatter, TestDateTimeFormatter>()
					.AddSingleton<IFileTagsSettingsService>(_ => new TestFileTagsSettingsService())
					.BuildServiceProvider();

				Ioc.Default.ConfigureServices(services);
				isServicesConfigured = true;
			}
		}

		[TestMethod]
		public void RemoveAllTags_ClearsSelectedFilesAndFolders()
		{
			using var scope = new TestItemScope();
			var tagA = new TagViewModel("Tag A", "#FF0000", "tag-a");
			var tagB = new TagViewModel("Tag B", "#00FF00", "tag-b");
			var fileItem = scope.CreateItem(StorageItemTypes.File, [tagA.Uid, tagB.Uid]);
			var folderItem = scope.CreateItem(StorageItemTypes.Folder, [tagB.Uid]);
			var noTagsItem = scope.CreateItem(StorageItemTypes.File, []);

			var viewModel = new FileTagsContextMenuViewModel(
				[fileItem, folderItem, noTagsItem],
				new TestFileTagsSettingsService(tagA, tagB));

			Assert.IsTrue(viewModel.CanRemoveAllTags);

			var tagsChangedRaised = false;
			viewModel.TagsChanged += (_, _) => tagsChangedRaised = true;

			var changed = viewModel.RemoveAllTags();

			Assert.IsTrue(changed);
			Assert.IsTrue(tagsChangedRaised);
			CollectionAssert.AreEqual(Array.Empty<string>(), fileItem.FileTags);
			CollectionAssert.AreEqual(Array.Empty<string>(), folderItem.FileTags);
			CollectionAssert.AreEqual(Array.Empty<string>(), noTagsItem.FileTags);
			Assert.IsFalse(fileItem.HasTags);
			Assert.IsFalse(folderItem.HasTags);
			Assert.IsFalse(noTagsItem.HasTags);
			Assert.IsFalse(viewModel.CanRemoveAllTags);
			Assert.IsFalse(viewModel.RemoveAllTagsCommand.CanExecute(null));
		}

		[TestMethod]
		public void UpdateTagSelection_UpdatesMultipleSelectionWithoutDuplicates()
		{
			using var scope = new TestItemScope();
			var commonTag = new TagViewModel("Common", "#FF0000", "common");
			var extraTag = new TagViewModel("Extra", "#00FF00", "extra");
			var firstItem = scope.CreateItem(StorageItemTypes.File, [commonTag.Uid, extraTag.Uid]);
			var secondItem = scope.CreateItem(StorageItemTypes.Folder, [commonTag.Uid]);

			var viewModel = new FileTagsContextMenuViewModel(
				[firstItem, secondItem],
				new TestFileTagsSettingsService(commonTag, extraTag));

			Assert.IsTrue(viewModel.IsTagAppliedToAllSelectedItems(commonTag));
			Assert.IsFalse(viewModel.IsTagAppliedToAllSelectedItems(extraTag));

			viewModel.UpdateTagSelection(extraTag, true);

			CollectionAssert.AreEquivalent([commonTag.Uid, extraTag.Uid], firstItem.FileTags);
			CollectionAssert.AreEquivalent([commonTag.Uid, extraTag.Uid], secondItem.FileTags);
			Assert.IsTrue(viewModel.IsTagAppliedToAllSelectedItems(extraTag));

			viewModel.UpdateTagSelection(commonTag, false);

			CollectionAssert.AreEquivalent([extraTag.Uid], firstItem.FileTags);
			CollectionAssert.AreEquivalent([extraTag.Uid], secondItem.FileTags);
			Assert.IsFalse(viewModel.IsTagAppliedToAllSelectedItems(commonTag));
		}

		[TestMethod]
		public void RemoveAllTags_DoesNothingWhenSelectionHasNoTags()
		{
			using var scope = new TestItemScope();
			var tag = new TagViewModel("Tag", "#FF0000", "tag");
			var item = scope.CreateItem(StorageItemTypes.File, []);

			var viewModel = new FileTagsContextMenuViewModel([item], new TestFileTagsSettingsService(tag));

			Assert.IsFalse(viewModel.CanRemoveAllTags);
			Assert.IsFalse(viewModel.RemoveAllTagsCommand.CanExecute(null));

			var changed = viewModel.RemoveAllTags();

			Assert.IsFalse(changed);
			CollectionAssert.AreEqual(Array.Empty<string>(), item.FileTags);
		}

		[TestMethod]
		public void ViewModel_DoesNotLeakAfterRelease()
		{
			var weakReference = CreateViewModelReference();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			Assert.IsFalse(weakReference.IsAlive);
		}

		private static WeakReference CreateViewModelReference()
		{
			var tag = new TagViewModel("Tag", "#FF0000", "tag");
			var viewModel = new FileTagsContextMenuViewModel([], new TestFileTagsSettingsService(tag));
			return new WeakReference(viewModel);
		}

		private sealed class TestItemScope : IDisposable
		{
			private readonly string rootPath = Path.Combine(Path.GetTempPath(), "Files.TagTests", Guid.NewGuid().ToString("N"));

			public TestItemScope()
			{
				Directory.CreateDirectory(rootPath);
			}

			public ListedItem CreateItem(StorageItemTypes itemType, string[] tags)
			{
				var itemName = $"{Guid.NewGuid():N}{(itemType is StorageItemTypes.Folder ? string.Empty : ".txt")}";
				var itemPath = Path.Combine(rootPath, itemName);

				if (itemType is StorageItemTypes.Folder)
					Directory.CreateDirectory(itemPath);
				else
					File.WriteAllText(itemPath, string.Empty);

				var item = new ListedItem()
				{
					ItemPath = itemPath,
					PrimaryItemAttribute = itemType,
					FileTags = tags,
				};

				return item;
			}

			public void Dispose()
			{
				if (Directory.Exists(rootPath))
					Directory.Delete(rootPath, true);
			}
		}

		private sealed class TestFileTagsSettingsService(params TagViewModel[] tags) : IFileTagsSettingsService
		{
			public event EventHandler OnSettingImportedEvent = delegate { };

			public event EventHandler OnTagsUpdated = delegate { };

			public IList<TagViewModel> FileTagList { get; set; } = tags.ToList();

			public void CreateNewTag(string newTagName, string color)
				=> throw new NotSupportedException();

			public void DeleteTag(string uid)
				=> throw new NotSupportedException();

			public void EditTag(string uid, string name, string color)
				=> throw new NotSupportedException();

			public object ExportSettings()
				=> throw new NotSupportedException();

			public TagViewModel GetTagById(string uid)
				=> FileTagList.Single(tag => tag.Uid == uid);

			public IEnumerable<TagViewModel> GetTagsByName(string tagName)
				=> FileTagList.Where(tag => tag.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));

			public IList<TagViewModel>? GetTagsByIds(string[] uids)
				=> FileTagList.Where(tag => uids.Contains(tag.Uid)).ToList();

			public bool ImportSettings(object import)
				=> throw new NotSupportedException();
		}

		private sealed class TestUserSettingsService : IUserSettingsService
		{
			public event EventHandler<SettingChangedEventArgs> OnSettingChangedEvent = delegate { };

			public IGeneralSettingsService GeneralSettingsService => null!;

			public IFoldersSettingsService FoldersSettingsService => null!;

			public IAppearanceSettingsService AppearanceSettingsService => null!;

			public IApplicationSettingsService ApplicationSettingsService => null!;

			public IInfoPaneSettingsService InfoPaneSettingsService => null!;

			public ILayoutSettingsService LayoutSettingsService => null!;

			public IAppSettingsService AppSettingsService => null!;

			public object ExportSettings()
				=> throw new NotSupportedException();

			public bool ImportSettings(object import)
				=> throw new NotSupportedException();
		}

		private sealed class TestStartMenuService : IStartMenuService
		{
			public bool IsPinned(string itemPath)
				=> false;

			public Task<bool> IsPinnedAsync(IStorable storable)
				=> Task.FromResult(false);

			public Task PinAsync(IStorable storable, string? displayName = null)
				=> Task.CompletedTask;

			public Task UnpinAsync(IStorable storable)
				=> Task.CompletedTask;
		}

		private sealed class TestDateTimeFormatter : IDateTimeFormatter
		{
			public string Name => nameof(TestDateTimeFormatter);

			public string ToLongLabel(DateTimeOffset offset)
				=> offset.ToString("O");

			public string ToShortLabel(DateTimeOffset offset)
				=> offset.ToString("O");

			public ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset, GroupByDateUnit unit)
				=> throw new NotSupportedException();
		}
	}
}