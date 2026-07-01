// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System;

namespace Files.Shared.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class GeneratedVTableFunctionAttribute : Attribute
{
	public required int Index { get; init; }
}
