// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Core.SourceGenerator.Data
{
	internal record VTableFunctionInfo(
		string FullyQualifiedParentTypeName,
		string ParentTypeNamespace,
		string ParentTypeName,
		string Name,
		string ReturnTypeName,
		int Index,
		EquatableArray<ParameterTypeNamePair> Parameters);
}
