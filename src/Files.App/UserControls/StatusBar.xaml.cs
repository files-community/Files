// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public sealed partial class StatusBar : UserControl
	{
		private const double SectionGapWidth = 8d;

		// Extra width required before expanding again, to avoid oscillation at the boundary
		private const double ExpandHysteresisWidth = 16d;

		// Ordered from fully expanded to fully collapsed
		private static readonly string[] _stateNames = ["NormalState", "CompactActionsState", "CompactState", "CompactIconsState"];

		private int _currentStateRank;

		// Collapsed elements measure as zero, so the last visible widths are cached
		private double _lastInfoNaturalWidth;
		private double _lastActionsNaturalWidth;
		private double _lastStatusButtonWidth = 90d;
		private double _lastGitButtonWidth = 80d;

		private bool _remeasureQueued;

		public ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		public StatusBarViewModel? StatusBarViewModel
		{
			get => (StatusBarViewModel)GetValue(StatusBarViewModelProperty);
			set => SetValue(StatusBarViewModelProperty, value);
		}

		// Using a DependencyProperty as the backing store for StatusBarViewModel.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty StatusBarViewModelProperty =
			DependencyProperty.Register(nameof(StatusBarViewModel), typeof(StatusBarViewModel), typeof(StatusBar), new PropertyMetadata(null, OnViewModelChanged));

		public SelectedItemsPropertiesViewModel? SelectedItemsPropertiesViewModel
		{
			get => (SelectedItemsPropertiesViewModel)GetValue(SelectedItemsPropertiesViewModelProperty);
			set => SetValue(SelectedItemsPropertiesViewModelProperty, value);
		}

		public static readonly DependencyProperty SelectedItemsPropertiesViewModelProperty =
			DependencyProperty.Register(nameof(SelectedItemsPropertiesViewModel), typeof(SelectedItemsPropertiesViewModel), typeof(StatusBar), new PropertyMetadata(null, OnViewModelChanged));

		public bool ShowInfoText
		{
			get => (bool)GetValue(ShowInfoTextProperty);
			set => SetValue(ShowInfoTextProperty, value);
		}

		// Using a DependencyProperty as the backing store for HideInfoText.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowInfoTextProperty =
			DependencyProperty.Register(nameof(ShowInfoText), typeof(bool), typeof(StatusBar), new PropertyMetadata(false));

		public StatusBar()
		{
			InitializeComponent();
		}

		public Visibility ToVisibility(bool value)
			=> value ? Visibility.Visible : Visibility.Collapsed;

		public Visibility GetOpenRepoInIDEItemVisibility(bool showOpenInIDEButton, string? gitBranchDisplayName)
			=> ToVisibility(showOpenInIDEButton && gitBranchDisplayName is not null);

		public Visibility GetActionsAreaVisibility(bool showOpenInIDEButton, string? gitBranchDisplayName)
			=> ToVisibility(showOpenInIDEButton || gitBranchDisplayName is not null);

		private void Content_SizeChanged(object sender, SizeChangedEventArgs e)
			=> UpdateResponsiveState();

		private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var statusBar = (StatusBar)d;

			if (e.OldValue is INotifyPropertyChanged oldViewModel)
				oldViewModel.PropertyChanged -= statusBar.ViewModel_PropertyChanged;
			if (e.NewValue is INotifyPropertyChanged newViewModel)
				newViewModel.PropertyChanged += statusBar.ViewModel_PropertyChanged;

			statusBar.QueueRemeasure();
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
			=> QueueRemeasure();

		private void QueueRemeasure()
		{
			if (_remeasureQueued)
				return;

			_remeasureQueued = true;

			// Deferred so bindings apply the changed content before measuring
			var enqueued = DispatcherQueue.TryEnqueue(() =>
			{
				_remeasureQueued = false;

				if (!IsLoaded)
					return;

				// UpdateLayout throws COMException when the pane hosting the bar is being
				// torn down; skipping leaves the bar in its previous, still-valid state
				SafetyExtensions.IgnoreExceptions(() =>
				{
					// Reveal everything to measure the new content, then re-decide;
					// no frame renders in between
					VisualStateManager.GoToState(this, _stateNames[0], false);
					UpdateLayout();
					UpdateResponsiveState();
				}, App.Logger);
			});

			// TryEnqueue returns false once the dispatcher shuts down at app close
			if (!enqueued)
				_remeasureQueued = false;
		}

		private void UpdateResponsiveState()
		{
			// The info panel's own size is clamped by its star column, so sum its children
			if (InfoTextPanel.Visibility == Visibility.Visible)
			{
				var visibleChildren = InfoTextPanel.Children.Where(child => child.Visibility == Visibility.Visible).ToList();
				_lastInfoNaturalWidth = visibleChildren.Sum(child => child.DesiredSize.Width) + InfoTextPanel.Spacing * Math.Max(0, visibleChildren.Count - 1);
			}

			if (StandardActions.Visibility == Visibility.Visible)
				_lastActionsNaturalWidth = StandardActions.DesiredSize.Width;
			if (StatusButton.Visibility == Visibility.Visible && StatusButtonLabel.Visibility == Visibility.Visible && StatusButton.DesiredSize.Width > 0)
				_lastStatusButtonWidth = StatusButton.DesiredSize.Width;
			if (GitOverflowButton.Visibility == Visibility.Visible && GitOverflowButtonLabel.Visibility == Visibility.Visible && GitOverflowButton.DesiredSize.Width > 0)
				_lastGitButtonWidth = GitOverflowButton.DesiredSize.Width;

			var hasActions = GetActionsAreaVisibility(StatusBarViewModel?.ShowOpenInIDEButton ?? false, StatusBarViewModel?.GitBranchDisplayName) == Visibility.Visible;
			var infoWidth = ShowInfoText ? _lastInfoNaturalWidth : 0d;
			var statusButtonWidth = ShowInfoText ? _lastStatusButtonWidth : 0d;
			var actionsWidth = hasActions ? _lastActionsNaturalWidth : 0d;
			var gitButtonWidth = hasActions ? _lastGitButtonWidth : 0d;

			// The widest state that fits wins; expanding also requires the hysteresis width
			double[] requiredWidths =
			[
				infoWidth + SectionGapWidth + actionsWidth,
				infoWidth + SectionGapWidth + gitButtonWidth,
				statusButtonWidth + SectionGapWidth + gitButtonWidth,
				0d,
			];

			var availableWidth = Math.Max(0d, ActualWidth - RootGrid.Padding.Left - RootGrid.Padding.Right);
			var targetRank = Enumerable.Range(0, requiredWidths.Length).First(rank =>
				requiredWidths[rank] + (rank < _currentStateRank ? ExpandHysteresisWidth : 0d) <= availableWidth);

			_currentStateRank = targetRank;
			VisualStateManager.GoToState(this, _stateNames[targetRank], false);
		}

		private void GitBranchCompact_Click(object sender, RoutedEventArgs e)
		{
			ActionsOverflowFlyout.Hide();

			// ShowAt throws ArgumentException when a queued remeasure expanded the bar and
			// removed the anchor from the tree between the click and this handler
			SafetyExtensions.IgnoreExceptions(() => BranchesFlyout.ShowAt(GitOverflowButton), App.Logger);
		}

		private void GitNetworkActionsCompact_Click(object sender, RoutedEventArgs e)
		{
			ActionsOverflowFlyout.Hide();

			// Same anchor-removed ArgumentException risk as in GitBranchCompact_Click
			SafetyExtensions.IgnoreExceptions(() => GitNetworkActions?.Flyout?.ShowAt(GitOverflowButton), App.Logger);
		}

		private async void BranchesFlyout_Opening(object _, object e)
		{
			if (StatusBarViewModel is null)
				return;

			StatusBarViewModel.IsBranchesFlyoutExpanded = true;
			StatusBarViewModel.ShowLocals = true;
			await StatusBarViewModel.LoadBranches();
			StatusBarViewModel.SelectedBranchIndex = StatusBarViewModel.ACTIVE_BRANCH_INDEX;
		}

		private void BranchesList_ItemClick(object sender, ItemClickEventArgs e)
		{
			BranchesFlyout.Hide();
		}

		private void BranchesFlyout_Closing(object _, object e)
		{
			if (StatusBarViewModel is null)
				return;

			StatusBarViewModel.IsBranchesFlyoutExpanded = false;
		}

		private async void DeleteBranch_Click(object sender, RoutedEventArgs e)
		{
			if (StatusBarViewModel is null)
				return;

			BranchesFlyout.Hide();
			await StatusBarViewModel.ExecuteDeleteBranch(((BranchItem)((Button)sender).DataContext).Name);
		}
	}
}
