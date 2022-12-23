using FluentFTP;
using System;
using Windows.Storage;
using Option = Files.Sdk.Storage.Enums.CreationCollisionOption;

namespace Files.App.Storage.Extensions
{
	internal static class CreationCollisionOptionExtensions
	{
		public static CreationCollisionOption ToWindowsCreationCollisionOption(this Option option) => option switch
		{
			Option.GenerateUniqueName => CreationCollisionOption.GenerateUniqueName,
			Option.ReplaceExisting => CreationCollisionOption.ReplaceExisting,
			Option.OpenIfExists => CreationCollisionOption.OpenIfExists,
			Option.FailIfExists => CreationCollisionOption.FailIfExists,
			_ => throw new ArgumentOutOfRangeException(nameof(option))
		};

		public static NameCollisionOption ToWindowsNameCollisionOption(this Option option) => option switch
		{
			Option.GenerateUniqueName => NameCollisionOption.GenerateUniqueName,
			Option.ReplaceExisting => NameCollisionOption.ReplaceExisting,
			Option.FailIfExists => NameCollisionOption.FailIfExists,
			_ => throw new ArgumentOutOfRangeException(nameof(option))
		};

		public static FtpRemoteExists ToFtpRemoteExists(this Option option)
			=> option is Option.ReplaceExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;
	}
}
