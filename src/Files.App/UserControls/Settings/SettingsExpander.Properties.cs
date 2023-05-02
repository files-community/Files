// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Files.App.UserControls.Settings
{
	public partial class SettingsExpander
	{
		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="Header"/> property.
		/// </summary>
		public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
			nameof(Header),
			typeof(object),
			typeof(SettingsExpander),
			new PropertyMetadata(defaultValue: null));

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="Description"/> property.
		/// </summary>
		public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
			nameof(Description),
			typeof(object),
			typeof(SettingsExpander),
			new PropertyMetadata(defaultValue: null));

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="HeaderIcon"/> property.
		/// </summary>
		public static readonly DependencyProperty HeaderIconProperty = DependencyProperty.Register(
			nameof(HeaderIcon),
			typeof(IconElement),
			typeof(SettingsExpander),
			new PropertyMetadata(defaultValue: null));


		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="Content"/> property.
		/// </summary>
		public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
			nameof(Content),
			typeof(object),
			typeof(SettingsExpander),
			new PropertyMetadata(defaultValue: null));

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="Content"/> property.
		/// </summary>
		public static readonly DependencyProperty ItemsHeaderProperty = DependencyProperty.Register(
			nameof(ItemsHeader),
			typeof(UIElement),
			typeof(SettingsExpander),
			new PropertyMetadata(defaultValue: null));

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="Content"/> property.
		/// </summary>
		public static readonly DependencyProperty ItemsFooterProperty = DependencyProperty.Register(
			nameof(ItemsFooter),
			typeof(UIElement),
			typeof(SettingsExpander),
			new PropertyMetadata(defaultValue: null));

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="IsExpanded"/> property.
		/// </summary>
		public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
		 nameof(IsExpanded),
		 typeof(bool),
		 typeof(SettingsExpander),
		 new PropertyMetadata(defaultValue: false, (d, e) => ((SettingsExpander)d).OnIsExpandedPropertyChanged((bool)e.OldValue, (bool)e.NewValue)));

		/// <summary>
		/// 
		/// <summary>
		/// Gets or sets the Header.
		/// </summary>
		public object Header
		{
			get => (object)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		/// <summary>
		/// Gets or sets the Description.
		/// </summary>
		public new object Description
		{
			get => (object)GetValue(DescriptionProperty);
			set => SetValue(DescriptionProperty, value);
		}

		/// <summary>
		/// Gets or sets the HeaderIcon.
		/// </summary>
		public IconElement HeaderIcon
		{
			get => (IconElement)GetValue(HeaderIconProperty);
			set => SetValue(HeaderIconProperty, value);
		}

		/// <summary>
		/// Gets or sets the Content.
		/// </summary>
		public object Content
		{
			get => (object)GetValue(ContentProperty);
			set => SetValue(ContentProperty, value);
		}

		/// <summary>
		/// Gets or sets the ItemsFooter.
		/// </summary>
		public UIElement ItemsHeader
		{
			get => (UIElement)GetValue(ItemsHeaderProperty);
			set => SetValue(ItemsHeaderProperty, value);
		}

		/// <summary>
		/// Gets or sets the ItemsFooter.
		/// </summary>
		public UIElement ItemsFooter
		{
			get => (UIElement)GetValue(ItemsFooterProperty);
			set => SetValue(ItemsFooterProperty, value);
		}

		/// <summary>
		/// Gets or sets the IsExpanded state.
		/// </summary>
		public bool IsExpanded
		{
			get => (bool)GetValue(IsExpandedProperty);
			set => SetValue(IsExpandedProperty, value);
		}

		protected virtual void OnIsExpandedPropertyChanged(bool oldValue, bool newValue)
		{
			OnIsExpandedChanged(oldValue, newValue);

			if (newValue)
			{
				Expanded?.Invoke(this, EventArgs.Empty);
			}
			else
			{
				Collapsed?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}
