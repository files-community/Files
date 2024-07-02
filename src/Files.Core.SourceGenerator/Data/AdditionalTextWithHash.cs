// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.SourceGenerator.Data
{
	/// <summary>
	/// Represents an additional text file paired with a unique identifier (hash).
	/// </summary>
	/// <remarks>
	/// This struct provides equality comparison based on the file path and hash value,
	/// a hash code calculation, and a string representation of its file path and hash.
	/// </remarks>
	internal readonly record struct AdditionalTextWithHash(AdditionalText File, Guid Hash)
	{
		public bool Equals(AdditionalTextWithHash other)
			=> File.Path.Equals(other.File.Path) && Hash.Equals(other.Hash);

		public override int GetHashCode()
		{
			unchecked
			{
				return (File.GetHashCode() * 397) ^ Hash.GetHashCode();
			}
		}

		public override string ToString()
			=> $"{nameof(File)}: {File?.Path}, {nameof(Hash)}: {Hash}";
	}
}
