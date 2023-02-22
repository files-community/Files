using System;

namespace Files.Core.Models
{
	public struct VolumeInfo : IEquatable<VolumeInfo>
	{
		public static VolumeInfo Empty { get; } = new VolumeInfo(Guid.Empty);

		public bool IsEmpty => Guid == Guid.Empty;

		public string Id => $@"\\?\Volume{{{Guid}}}";

		public Guid Guid { get; }

		public VolumeInfo(Guid guid) => Guid = guid;
		public VolumeInfo(string id) => Guid = ToGuid(id);

		public static implicit operator string(VolumeInfo info) => info.Id;
		public static implicit operator Guid(VolumeInfo info) => info.Guid;

		public static bool operator ==(VolumeInfo a, VolumeInfo b) => a.Guid == b.Guid;
		public static bool operator !=(VolumeInfo a, VolumeInfo b) => a.Guid != b.Guid;

		public override string ToString() => Id;
		public override int GetHashCode() => Guid.GetHashCode();
		public override bool Equals(object? other) => other is VolumeInfo info && Equals(info);
		public bool Equals(VolumeInfo other) => other.Guid.Equals(Guid);

		private static Guid ToGuid(string id)
		{
			if (string.IsNullOrEmpty(id) || !id.StartsWith(@"\\?\Volume{"))
			{
				return Guid.Empty;
			}

			int guidLength = Guid.Empty.ToString().Length;
			string guid = id.Substring(@"\\?\Volume{".Length, guidLength);

			return Guid.Parse(guid);
		}
	}
}
