// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.ViewModels.Settings
{
	/// <summary>
	/// ViewModel for toolbar customization. Manages item reordering, addition/removal,
	/// preview updates, and save/cancel/reset workflow.
	/// </summary>
	public sealed partial class ToolbarCustomizationViewModel : ObservableObject
	{
		private readonly IUserSettingsService UserSettingsService;
		private readonly ICommandManager CommandManager;
		private readonly Dictionary<string, ObservableCollection<ToolbarItemDescriptor>> toolbarItemsByContext = new(StringComparer.Ordinal);
		private IEnumerable<ToolbarItemDescriptor> AllItems => toolbarItemsByContext.Values.SelectMany(items => items);
		private IEnumerable<string> ContextKeys => ToolbarContexts.Select(c => c.Key);
		private bool isApplyingItems;
		private bool isSessionActive;

		public ObservableCollection<KeyValuePair<string, string>> ToolbarContexts { get; } = [];
		public ObservableCollection<ToolbarAvailableTreeItem> AvailableToolbarTreeItems { get; } = [];
		public ObservableCollection<ToolbarItemDescriptor> ToolbarItems => GetOrCreateItems(ResolveContextId());
		public ObservableCollection<ToolbarItemDescriptor> AlwaysVisibleToolbarItems => GetOrCreateItems(ToolbarDefaultsTemplate.AlwaysVisibleContextId);
		public string SelectedToolbarContextName => ToolbarItemDescriptor.GetContextDisplayName(ResolveContextId());
		public bool IsSelectedContextAlwaysVisible => ResolveContextId() == ToolbarDefaultsTemplate.AlwaysVisibleContextId;

		[ObservableProperty] public partial string? SelectedToolbarContextId { get; set; }
		[ObservableProperty] public partial bool HasToolbarChanges { get; set; }

		public event EventHandler? CloseRequested;
		public event EventHandler? PreviewChanged;

		public ToolbarCustomizationViewModel(IUserSettingsService userSettingsService, ICommandManager commandManager)
		{
			UserSettingsService = userSettingsService;
			CommandManager = commandManager;
			foreach (var ctxId in ToolbarItemDescriptor.BuildKnownContextIds(CommandManager))
			{
				ToolbarContexts.Add(new(ctxId, ToolbarItemDescriptor.GetContextDisplayName(ctxId)));
				_ = GetOrCreateItems(ctxId);
			}
			SelectedToolbarContextId = ToolbarDefaultsTemplate.AlwaysVisibleContextId;
			LoadToolbarItems();
		}

		public void BeginToolbarCustomizationSession()
		{
			if (isSessionActive) return;
			SelectedToolbarContextId = ToolbarDefaultsTemplate.AlwaysVisibleContextId;
			isSessionActive = true;
			HasToolbarChanges = false;
		}

		public void FinishCustomizationSession(bool persistChanges)
		{
			if (!isSessionActive) return;
			if (persistChanges)
				SaveToolbarItems();
			else
				LoadToolbarItems();
			isSessionActive = false;
			HasToolbarChanges = false;
		}

		partial void OnSelectedToolbarContextIdChanged(string? value) => RefreshAvailableAndNotify(notifyPreview: true);

		private void LoadToolbarItems()
		{
			var saved = UserSettingsService.AppearanceSettingsService.CustomToolbarItems;
			ApplyItems(saved is { Count: > 0 }
				? ToolbarItemDescriptor.NormalizeSettingsByContext(ContextKeys, saved)
				: ToolbarDefaultsTemplate.CreateDefaultItemsByContext(), saveChanges: false);
		}

		private void ApplyItems(IReadOnlyDictionary<string, List<ToolbarItemSettingsEntry>> itemsByContext, bool saveChanges)
		{
			isApplyingItems = true;
			SetItemSubscriptions(AllItems, subscribe: false);
			try
			{
				foreach (var ctxId in ContextKeys) GetOrCreateItems(ctxId).Clear();
				foreach (var pair in itemsByContext)
				{
					var ctxId = ToolbarDefaultsTemplate.NormalizeContextId(pair.Key);
					var items = GetOrCreateItems(ctxId);
					foreach (var entry in pair.Value)
						if (ToolbarItemDescriptor.Resolve(entry, CommandManager, ctxId) is { } desc)
							items.Add(desc);
				}
			}
			finally
			{
				SetItemSubscriptions(AllItems, subscribe: true);
				isApplyingItems = false;
			}
			RefreshAvailableAndNotify();
			if (saveChanges) SaveToolbarItems();
		}

		private void RefreshAvailableAndNotify(bool notifyPreview = false)
		{
			AvailableToolbarTreeItems.Clear();
			foreach (var node in ToolbarItemDescriptor.BuildAvailableTree(
				ToolbarItemDescriptor.GetAvailableItems(ResolveContextId(), CommandManager)))
				AvailableToolbarTreeItems.Add(node);
			OnPropertyChanged(nameof(ToolbarItems));
			OnPropertyChanged(nameof(SelectedToolbarContextName));
			OnPropertyChanged(nameof(IsSelectedContextAlwaysVisible));
			if (notifyPreview) PreviewChanged?.Invoke(this, EventArgs.Empty);
		}

		private void Items_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (isApplyingItems) return;
			SetItemSubscriptions(e.OldItems, subscribe: false);
			SetItemSubscriptions(e.NewItems, subscribe: true);
			TrackAndNotify();
		}

		private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(ToolbarItemDescriptor.ShowIcon) or nameof(ToolbarItemDescriptor.ShowLabel))
				TrackAndNotify();
		}

		private void TrackAndNotify()
		{
			if (isSessionActive)
				HasToolbarChanges = true;
			else
				SaveToolbarItems();
			PreviewChanged?.Invoke(this, EventArgs.Empty);
		}

		private void SetItemSubscriptions(System.Collections.IEnumerable? items, bool subscribe)
		{
			if (items is null) return;
			foreach (var item in items.OfType<ToolbarItemDescriptor>())
			{
				if (subscribe)
					item.PropertyChanged += Item_PropertyChanged;
				else
					item.PropertyChanged -= Item_PropertyChanged;
			}
		}

		private void SaveToolbarItems()
			=> UserSettingsService.AppearanceSettingsService.CustomToolbarItems =
				ContextKeys.ToDictionary(id => id,
					id => GetOrCreateItems(id).Select(i => i.ToSettingsEntry()).ToList(), StringComparer.Ordinal);

		public void InsertAvailableToolbarItemAt(ToolbarItemDescriptor source, int index)
		{
			var ctxId = ResolveContextId();
			if (ToolbarItemDescriptor.Resolve(source.ToSettingsEntry(), CommandManager, ctxId) is not { } clone) return;
			clone.ShowLabel = true;
			var items = GetOrCreateItems(ctxId);
			items.Insert(Math.Clamp(index, 0, items.Count), clone);
		}

		[RelayCommand]
		private void RemoveToolbarItem(ToolbarItemDescriptor? item)
		{
			if (item is not null)
				GetOrCreateItems(item.ContextId).Remove(item);
		}

		[RelayCommand]
		private void ResetToolbar()
		{
			ApplyItems(ToolbarDefaultsTemplate.CreateDefaultItemsByContext(), saveChanges: false);
			TrackAndNotify();
		}

		[RelayCommand]
		private void SaveToolbar()
		{
			FinishCustomizationSession(persistChanges: true);
			CloseRequested?.Invoke(this, EventArgs.Empty);
		}

		[RelayCommand]
		private void CancelToolbar()
		{
			FinishCustomizationSession(persistChanges: false);
			CloseRequested?.Invoke(this, EventArgs.Empty);
		}

		private ObservableCollection<ToolbarItemDescriptor> GetOrCreateItems(string contextId)
		{
			if (!toolbarItemsByContext.TryGetValue(contextId, out var items))
			{
				items = [];
				items.CollectionChanged += Items_CollectionChanged;
				toolbarItemsByContext[contextId] = items;
			}
			return items;
		}

		private string ResolveContextId()
			=> ToolbarDefaultsTemplate.NormalizeContextId(SelectedToolbarContextId,
				nullFallback: ToolbarDefaultsTemplate.AlwaysVisibleContextId);
	}
}
