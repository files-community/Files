using Files.App.Controls.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Controls
{
	public partial class Toolbar : Control
	{

		#region ItemsSource (object)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="ItemsSource"/> property.
		/// </summary>
		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register(
				nameof(ItemsSource),
				typeof(object),
				typeof(Toolbar),
				new PropertyMetadata(null, (d, e) => ((Toolbar)d).OnItemsSourcePropertyChanged((object)e.OldValue, (object)e.NewValue)));



		/// <summary>
		/// Gets or sets the objects we use as ItemsSource for the Toolbar.
		/// </summary>
		public object ItemsSource
		{
			get => (object)GetValue( ItemsSourceProperty );
			set => SetValue( ItemsSourceProperty , value );
		}



		protected virtual void OnItemsSourcePropertyChanged(object oldValue , object newValue)
		{
			if ( newValue != oldValue )
			{
				ItemsSourceChanged( newValue );
			}
		}

		#endregion



		#region ItemTemplate

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="ItemTemplate"/> property.
		/// </summary>
		public static readonly DependencyProperty ItemTemplateProperty =
			DependencyProperty.Register(
				nameof(ItemTemplate),
				typeof(DataTemplate),
				typeof(Toolbar),
				new PropertyMetadata(null, (d, e) => ((Toolbar)d).OnItemTemplatePropertyChanged((DataTemplate)e.OldValue, (DataTemplate)e.NewValue)));



		/// <summary>
		/// Gets or sets the ItemTemplate we use for the ItemsSource for the Toolbar.
		/// </summary>
		public DataTemplate ItemTemplate
		{
			get { return (DataTemplate)GetValue( ItemTemplateProperty ); }
			set { SetValue( ItemTemplateProperty , value ); }
		}



		protected virtual void OnItemTemplatePropertyChanged(DataTemplate oldValue , DataTemplate newValue)
		{
			if ( newValue != oldValue )
			{
				ItemTemplateChanged( newValue );
			}
		}

		#endregion



		#region ToolbarSize (enum ToolbarSizes)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="ToolbarSize"/> property.
		/// </summary>
		public static readonly DependencyProperty ToolbarSizeProperty =
			DependencyProperty.Register(
				nameof(ToolbarSize),
				typeof(ToolbarSizes),
				typeof(Toolbar),
				new PropertyMetadata(ToolbarSizes.Medium, (d, e) => ((Toolbar)d).OnToolbarSizePropertyChanged((ToolbarSizes)e.OldValue, (ToolbarSizes)e.NewValue)));



		/// <summary>
		/// Gets or sets an Enum value to choose from our ToolbarSizes. (Small, Medium, Large)
		/// </summary>
		public ToolbarSizes ToolbarSize
		{
			get => (ToolbarSizes)GetValue( ToolbarSizeProperty );
			set => SetValue( ToolbarSizeProperty , value );
		}


		/// <summary>
		/// Triggers when the ToolbarSize property changes
		/// </summary>
		/// <param name="oldValue"></param>
		/// <param name="newValue"></param>
		protected virtual void OnToolbarSizePropertyChanged(ToolbarSizes oldValue , ToolbarSizes newValue)
		{
			if ( newValue != oldValue )
			{
				ToolbarSizeChanged( newValue );
			}
		}

		#endregion

	}
}
