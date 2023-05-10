// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Extensions;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Files.App.Helpers
{
	public class GroupedCollection<T> : BulkConcurrentObservableCollection<T>, IGroupedCollectionHeader
	{
		public GroupedHeaderViewModel Model { get; set; }

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

		private void GroupedCollection_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Count))
			{
				Model.CountText = string.Format(Count > 1 ? "GroupItemsCount_Plural".GetLocalizedResource() : "GroupItemsCount_Singular".GetLocalizedResource(), Count);
			}
		}

		public void InitializeExtendedGroupHeaderInfoAsync()
		{
			if (GetExtendedGroupHeaderInfo is null)
			{
				return;
			}

			Model.ResumePropertyChangedNotifications(false);

			GetExtendedGroupHeaderInfo.Invoke(this);
			Model.Initialized = true;
			if (isBulkOperationStarted)
			{
				Model.PausePropertyChangedNotifications();
			}
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

	/// <summary>
	/// This interface is used to allow using x:Bind for the group header template.
	/// <br/>
	/// This is needed because x:Bind does not work with generic types, however it does work with interfaces.
	/// that are implemented by generic types.
	/// </summary>
	public interface IGroupedCollectionHeader
	{
		public GroupedHeaderViewModel Model { get; set; }
	}

	public class GroupedHeaderViewModel : ObservableObject
	{
		public string Key { get; set; }
		public bool Initialized { get; set; }
		public int SortIndexOverride { get; set; }

		private string text;

		public string Text
		{
			get => text ?? ""; // Text is bound to AutomationProperties.Name and can't be null
			set => SetPropertyWithUpdateDelay(ref text, value);
		}

		private string subtext;

		public string Subtext
		{
			get => subtext;
			set => SetPropertyWithUpdateDelay(ref subtext, value);
		}

		private string countText;

		public string CountText
		{
			get => countText;
			set => SetPropertyWithUpdateDelay(ref countText, value);
		}

		private bool showCountTextBelow;

		public bool ShowCountTextBelow
		{
			get => showCountTextBelow;
			set => SetProperty(ref showCountTextBelow, value);
		}

		private ImageSource imageSource;

		public ImageSource ImageSource
		{
			get => imageSource;
			set => SetPropertyWithUpdateDelay(ref imageSource, value);
		}

		private string icon;

		public string Icon
		{
			get => icon;
			set => SetPropertyWithUpdateDelay(ref icon, value);
		}

		private void SetPropertyWithUpdateDelay<T>(ref T field, T newVal, [CallerMemberName] string propName = null)
		{
			if (propName is null)
			{
				return;
			}
			var name = propName.StartsWith("get_", StringComparison.OrdinalIgnoreCase)
				? propName.Substring(4)
				: propName;

			if (!deferPropChangedNotifs)
			{
				SetProperty<T>(ref field, newVal, name);
			}
			else
			{
				field = newVal;
				if (!changedPropQueue.Contains(name))
				{
					changedPropQueue.Add(name);
				}
			}
		}

		public void PausePropertyChangedNotifications()
		{
			deferPropChangedNotifs = true;
		}

		public void ResumePropertyChangedNotifications(bool triggerUpdates = true)
		{
			if (deferPropChangedNotifs == false)
			{
				return;
			}
			deferPropChangedNotifs = false;
			if (triggerUpdates)
			{
				changedPropQueue.ForEach(prop => OnPropertyChanged(prop));
				changedPropQueue.Clear();
			}
		}

		private List<string> changedPropQueue = new List<string>();

		// This is true by default to make it easier to initialize groups from a different thread
		private bool deferPropChangedNotifs = true;
	}
}