// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Data.Attributes
{
	/// <summary>
	/// Provides an attribute for generation of <see cref="DependencyProperty"/> and its Property.
	/// </summary>
	/// <remarks>
	/// Following code will be generated:
	/// <code>
	/// <see langword="public static readonly"/> <see cref="DependencyProperty"/> Property =
	/// 	<see cref="DependencyProperty"/>.Register(
	/// 		<see langword="nameof"/>(Field),
	/// 		<see langword="typeof"/>(<typeparamref name="T"/>),
	/// 		<see langword="typeof"/>(TClass),
	/// 		<see langword="new"/> <see cref="PropertyMetadata"/>(DefaultValue, OnPropertyChanged));
	/// <br/>
	/// <br/>
	/// <see langword="public"/> <typeparamref name="T"/> Field
	/// {
	/// 	<see langword="get"/> => (<typeparamref name="T"/>)GetValue(Property);
	/// 	<see langword="set"/> => SetValue(Property, <see langword="value"/>);
	/// }
	/// </code>
	/// <typeparam name="T">property type (nullable value type are not allowed)</typeparam>
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class DependencyPropertyAttribute<T> : Attribute where T : notnull
	{
		/// <inheritdoc cref="DependencyPropertyAttribute{T}"/>
		/// <param name="name">The name of the property.</param>
		/// <param name="propertyChanged">The name of the method, which called when property changed.</param>
		public DependencyPropertyAttribute(string name, string propertyChanged = "")
		{
			Name = name;
			PropertyChanged = propertyChanged;
		}

		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the name of the method, which called when property changed.
		/// </summary>
		public string PropertyChanged { get; }

		/// <summary>
		/// Gets or initializes a value whether property setter is private.
		/// </summary>
		/// <remarks>
		/// Default value is <see langword="false"/>.
		/// </remarks>
		public bool IsSetterPrivate { get; init; }

		/// <summary>
		/// Gets or initializes a value whether property type is nullable (nullable value type are not allowed).
		/// </summary>
		/// <remarks>
		/// Default value is <see langword="false"/>.
		/// </remarks>
		public bool IsNullable { get; init; }

		/// <summary>
		/// Gets or initializes a default value of property.
		/// </summary>
		/// <remarks>default: <see cref="DependencyProperty.UnsetValue"/></remarks>
		public string DefaultValue { get; init; } = "DependencyProperty.UnsetValue";
	}
}
