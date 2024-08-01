using Files.App.Controls.Primitives;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Controls
{
	public partial class Toolbar
	{

		#region Items (object)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="Items"/> property.
		/// </summary>
		public static readonly DependencyProperty ItemsProperty =
			DependencyProperty.Register(
				nameof(Items),
				typeof(object),
				typeof(Toolbar),
				new PropertyMetadata(null, (d, e) => ((Toolbar)d).OnItemsPropertyChanged((object)e.OldValue, (object)e.NewValue)));



		/// <summary>
		/// Gets or sets the objects we use as Items for the Toolbar.
		/// </summary>
		public object Items
		{
			get => (object)GetValue( ItemsProperty );
			set => SetValue( ItemsProperty , value );
		}



		protected virtual void OnItemsPropertyChanged(object oldValue , object newValue)
		{
			UpdateItemsProperty( newValue );
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



		protected virtual void OnToolbarSizePropertyChanged(ToolbarSizes oldValue , ToolbarSizes newValue)
		{
			OnToolbarSizePropertyChanged( newValue );
		}

		#endregion

	}
}
