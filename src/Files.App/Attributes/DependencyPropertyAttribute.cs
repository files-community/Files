using Microsoft.UI.Xaml;

namespace Files.App.Attributes
{
	/// <summary>
	/// Generate:
	/// <code>
	/// <see langword="public static readonly"/> <see cref="DependencyProperty"/> Property = <see cref="DependencyProperty"/>.Register(<see langword="nameof"/>(Field), <see langword="typeof"/>(<typeparamref name="T"/>), <see langword="typeof"/>(TClass), <see langword="new"/> <see cref="PropertyMetadata"/>(DefaultValue, OnPropertyChanged));
	/// <br/>
	/// <see langword="public"/> <typeparamref name="T"/> Field { <see langword="get"/> => (<typeparamref name="T"/>)GetValue(Property); <see langword="set"/> => SetValue(Property, <see langword="value"/>); }
	/// </code>
	/// </summary>
	/// <typeparam name="T">property type (nullable value type are not allowed)</typeparam>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class DependencyPropertyAttribute<T> : Attribute where T : notnull
	{
		/// <inheritdoc cref="DependencyPropertyAttribute{T}"/>
		/// <param name="name">Property name</param>
		/// <param name="propertyChanged">The name of the method, which called when property changed</param>
		public DependencyPropertyAttribute(string name, string propertyChanged = "")
		{
			Name = name;
			PropertyChanged = propertyChanged;
		}

		/// <summary>
		/// Property name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The name of the method, which called when property changed
		/// </summary>
		public string PropertyChanged { get; }

		/// <summary>
		/// Whether property setter is private
		/// </summary>
		/// <remarks>default: <see langword="false"/></remarks>
		public bool IsSetterPrivate { get; init; } = false;

		/// <summary>
		/// Whether property type is nullable (nullable value type are not allowed)
		/// </summary>
		/// <remarks>default: <see langword="false"/></remarks>
		public bool IsNullable { get; init; }

		/// <summary>
		/// Default value of property
		/// </summary>
		/// <remarks>default: <see cref="DependencyProperty.UnsetValue"/></remarks>
		public string DefaultValue { get; init; } = "DependencyProperty.UnsetValue";
	}
}
