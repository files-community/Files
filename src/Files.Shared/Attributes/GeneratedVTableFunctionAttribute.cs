// Copyright (c) Files Community
// Licensed under the MIT License.

using System;

namespace Files.Shared.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class GeneratedVTableFunctionAttribute : Attribute
{
	public required int Index { get; init; }
}
