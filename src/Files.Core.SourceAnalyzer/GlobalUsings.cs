// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

// Files
global using global::Files.Core.SourceAnalyzer.Analyzers;

// Microsoft
global using global::Microsoft.CodeAnalysis;
global using global::Microsoft.CodeAnalysis.CodeActions;
global using global::Microsoft.CodeAnalysis.CodeFixes;
global using global::Microsoft.CodeAnalysis.CSharp;
global using global::Microsoft.CodeAnalysis.CSharp.Syntax;
global using global::Microsoft.CodeAnalysis.Diagnostics;

// System
global using global::System.Collections.Generic;
global using global::System.Collections.Immutable;
global using global::System.Composition;
global using global::System.Linq;
global using global::System.Threading.Tasks;