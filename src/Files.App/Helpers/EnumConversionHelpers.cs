using Files.Core.Enums;
using Windows.Storage;

namespace Files.App.Helpers
{
	public static class EnumConversionHelpers
	{
		public static CreationCollisionOption Convert(this NameCollisionOption option)
		{
			return option switch
			{
				NameCollisionOption.FailIfExists => CreationCollisionOption.FailIfExists,
				NameCollisionOption.GenerateUniqueName => CreationCollisionOption.GenerateUniqueName,
				NameCollisionOption.ReplaceExisting => CreationCollisionOption.ReplaceExisting,
				_ => CreationCollisionOption.GenerateUniqueName,
			};
		}

		public static NameCollisionOption Convert(this FileNameConflictResolveOptionType option)
		{
			return option switch
			{
				FileNameConflictResolveOptionType.Skip => NameCollisionOption.FailIfExists,
				FileNameConflictResolveOptionType.GenerateNewName => NameCollisionOption.GenerateUniqueName,
				FileNameConflictResolveOptionType.ReplaceExisting => NameCollisionOption.ReplaceExisting,
				_ => NameCollisionOption.GenerateUniqueName,
			};
		}

		public static FileNameConflictResolveOptionType ConvertBack(this NameCollisionOption option)
		{
			return option switch
			{
				NameCollisionOption.FailIfExists => FileNameConflictResolveOptionType.Skip,
				NameCollisionOption.GenerateUniqueName => FileNameConflictResolveOptionType.GenerateNewName,
				NameCollisionOption.ReplaceExisting => FileNameConflictResolveOptionType.ReplaceExisting,
				_ => FileNameConflictResolveOptionType.GenerateNewName,
			};
		}
	}
}