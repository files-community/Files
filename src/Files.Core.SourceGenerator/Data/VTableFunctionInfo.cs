// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.Core.SourceGenerator.Data
{
	internal record VTableFunctionInfo(
		string FullyQualifiedParentTypeName,
		string ParentTypeNamespace,
		string ParentTypeName,
		bool IsReturnTypeVoid,
		string Name,
		string ReturnTypeName,
		int Index,
		EquatableArray<ParameterTypeNamePair> Parameters);
}
