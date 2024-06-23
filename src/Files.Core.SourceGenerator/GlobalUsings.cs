// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

// System
global using global::System;
global using global::System.Collections;
global using global::System.Collections.Generic;
global using global::System.Collections.Immutable;
global using global::System.Collections.ObjectModel;
global using global::System.Linq;
global using global::System.Text;
global using global::System.Threading;
global using global::System.Threading.Tasks;
global using global::System.ComponentModel;
global using global::System.Diagnostics;
global using SystemIO = global::System.IO;

global using global::Microsoft.CodeAnalysis;
global using global::Microsoft.CodeAnalysis.CSharp;
global using global::Microsoft.CodeAnalysis.CSharp.Syntax;
global using global::Files.Core.SourceGenerator.Data;
global using global::Microsoft.CodeAnalysis.Text;

global using global::Files.Core.SourceGenerator.Utilities;
global using global::Files.Core.SourceGenerator.Parser;