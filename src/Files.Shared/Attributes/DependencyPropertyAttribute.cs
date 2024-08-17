// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;

namespace Files.Shared.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class DependencyPropertyAttribute<T> : Attribute where T : notnull
{
	public DependencyPropertyAttribute(string name, string propertyChanged = "")
	{
		Name = name;
		PropertyChanged = propertyChanged;
	}

	public string Name { get; }

	public string PropertyChanged { get; }

	public bool IsSetterPrivate { get; init; }

	public bool IsNullable { get; init; }

	public string DefaultValue { get; init; } = "DependencyProperty.UnsetValue";
}
