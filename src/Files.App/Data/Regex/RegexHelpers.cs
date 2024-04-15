// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Text.RegularExpressions;
using RegularExpression = System.Text.RegularExpressions.Regex;

namespace Files.App.Data.Regex;

public static partial class RegexHelpers
{
	[GeneratedRegex(@"\w:\w")]
	public static partial RegularExpression AlternateStream();
	
	[GeneratedRegex("(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)")]
	public static partial RegularExpression SpaceSplit();
	
	[GeneratedRegex(@"^\\\\\?\\[^\\]*\\?")]
	public static partial RegularExpression WindowsPath();
	
	[GeneratedRegex(@"^[A-Z]:\\\$Recycle\.Bin\\", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	public static partial RegularExpression RecycleBinPath();
	
	[GeneratedRegex(@"^(?!/)(?!.*//)[^\000-\037\177 ~^:?*[]+(?!.*\.\.)(?!.*@\{)(?!.*\\)(?<!/\.)(?<!\.)(?<!/)(?<!\.lock)$")]
	public static partial RegularExpression GitBranchName();
	
	[GeneratedRegex(@"\s+")]
	public static partial RegularExpression WhitespaceAtLeastOnce();
	
	[GeneratedRegex(@"\s*\(\w:\)$")]
	public static partial RegularExpression DriveLetterRegex();
}