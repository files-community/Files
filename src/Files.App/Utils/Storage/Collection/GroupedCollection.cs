// Copyright (c) Files Community
// Licensed under the MIT License.

[assembly: WinRT.GeneratedWinRTExposedExternalType(typeof(Files.App.Utils.Storage.GroupedCollection<Files.App.Utils.ListedItem>))]

namespace Files.App.Utils.Storage
{
	[WinRT.GeneratedWinRTExposedType]
	public sealed partial class GroupedCollection<T> : BulkConcurrentObservableCollection<T>, IGroupedCollectionHeader
	{
		private GroupedHeaderViewModel? model;
		public GroupedHeaderViewModel Model
		{
			get => model ?? throw new InvalidOperationException("The group header model has not been initialized.");
			set => model = value;
		}

		public GroupedCollection(IEnumerable<T> items) : base(items)
		{
			AddEvents();
		}

		public GroupedCollection(string key) : base()
		{
			AddEvents();
			Model = new GroupedHeaderViewModel()
			{
				Key = key,
				Text = key,
			};
		}

		public GroupedCollection(string key, string text) : base()
		{
			AddEvents();
			Model = new GroupedHeaderViewModel()
			{
				Key = key,
				Text = text,
			};
		}

		private void AddEvents()
		{
			PropertyChanged += GroupedCollection_PropertyChanged;
		}

		private void GroupedCollection_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Count))
			{
				Model.CountText = string.Format(
					Count > 1
						? Strings.GroupItemsCount_Plural.GetLocalizedResource()
						: Strings.GroupItemsCount_Singular.GetLocalizedResource(),
					Count);
			}
		}

		public void InitializeExtendedGroupHeaderInfoAsync()
		{
			if (GetExtendedGroupHeaderInfo is null)
				return;

			Model.ResumePropertyChangedNotifications(false);

			GetExtendedGroupHeaderInfo.Invoke(this);
			Model.Initialized = true;

			if (isBulkOperationStarted)
				Model.PausePropertyChangedNotifications();
		}

		public override void BeginBulkOperation()
		{
			base.BeginBulkOperation();

			Model.PausePropertyChangedNotifications();
		}

		public override void EndBulkOperation()
		{
			base.EndBulkOperation();

			Model.ResumePropertyChangedNotifications();
		}
	}
}
