// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Controls
{
	/// <summary>
	/// The Blade is used as a child in the BladeView
	/// </summary>
	public partial class BladeItem
	{
		/// <summary>
		/// Identifies the <see cref="TitleBarVisibility"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TitleBarVisibilityProperty = DependencyProperty.Register(nameof(TitleBarVisibility), typeof(Visibility), typeof(BladeItem), new PropertyMetadata(default(Visibility)));

		/// <summary>
		/// Identifies the <see cref="TitleBarBackground"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TitleBarBackgroundProperty = DependencyProperty.Register(nameof(TitleBarBackground), typeof(Brush), typeof(BladeItem), new PropertyMetadata(default(Brush)));

		/// <summary>
		/// Identifies the <see cref="CloseButtonBackground"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty CloseButtonBackgroundProperty = DependencyProperty.Register(nameof(CloseButtonBackground), typeof(Brush), typeof(BladeItem), new PropertyMetadata(default(Brush)));

		/// <summary>
		/// Identifies the <see cref="IsOpen"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(BladeItem), new PropertyMetadata(true, IsOpenChangedCallback));

		/// <summary>
		/// Identifies the <see cref="CloseButtonForeground"/> dependency property
		/// </summary>
		public static readonly DependencyProperty CloseButtonForegroundProperty = DependencyProperty.Register(nameof(CloseButtonForeground), typeof(Brush), typeof(BladeItem), new PropertyMetadata(new SolidColorBrush(Colors.Black)));

		private WeakReference<BladeView> _parentBladeView;

		/// <summary>
		/// Gets or sets the foreground color of the close button
		/// </summary>
		public Brush CloseButtonForeground
		{
			get { return (Brush)GetValue(CloseButtonForegroundProperty); }
			set { SetValue(CloseButtonForegroundProperty, value); }
		}

		/// <summary>
		/// Gets or sets the visibility of the title bar for this blade
		/// </summary>
		public Visibility TitleBarVisibility
		{
			get { return (Visibility)GetValue(TitleBarVisibilityProperty); }
			set { SetValue(TitleBarVisibilityProperty, value); }
		}

		/// <summary>
		/// Gets or sets the background color of the title bar
		/// </summary>
		public Brush TitleBarBackground
		{
			get { return (Brush)GetValue(TitleBarBackgroundProperty); }
			set { SetValue(TitleBarBackgroundProperty, value); }
		}

		/// <summary>
		/// Gets or sets the background color of the default close button in the title bar
		/// </summary>
		public Brush CloseButtonBackground
		{
			get { return (Brush)GetValue(CloseButtonBackgroundProperty); }
			set { SetValue(CloseButtonBackgroundProperty, value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this blade is opened
		/// </summary>
		public bool IsOpen
		{
			get { return (bool)GetValue(IsOpenProperty); }
			set { SetValue(IsOpenProperty, value); }
		}

		internal BladeView ParentBladeView
		{
			get
			{
				this._parentBladeView.TryGetTarget(out var bladeView);
				return bladeView;
			}
			set => this._parentBladeView = new WeakReference<BladeView>(value);
		}

		private static void IsOpenChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
		{
			BladeItem bladeItem = (BladeItem)dependencyObject;
			bladeItem.Visibility = bladeItem.IsOpen ? Visibility.Visible : Visibility.Collapsed;
			bladeItem.VisibilityChanged?.Invoke(bladeItem, bladeItem.Visibility);
		}
	}
}
