// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;

namespace Files.Shared;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RegistrySerializableAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public sealed class RegistryIgnoreAttribute : Attribute { }
