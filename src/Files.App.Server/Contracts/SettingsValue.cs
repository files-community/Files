// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

public enum SettingsValueKind
{
	Null,
	Object,
	Array,
	String,
	True,
	False,
	Int64,
	UInt64,
	Double,
}

// Containers precede their children, so the entire value crosses the ABI in one array.
public struct SettingsValue
{
	public SettingsValueKind Kind;
	public string Name;
	public string Text;
	public long Signed;
	public ulong Unsigned;
	public double Number;
	public int Children;
}
