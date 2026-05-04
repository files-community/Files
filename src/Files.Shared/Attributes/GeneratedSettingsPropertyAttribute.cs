using System;

namespace Files.Shared.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class GeneratedSettingsPropertyAttribute : Attribute
{
	public object? DefaultValue { get; set; }
	public string? DefaultValueCallback { get; set; }
	public string? GetValueCallback { get; set; }
}
