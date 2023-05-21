// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Filesystem
{
	public class FilesystemResult
	{
		public FileSystemStatusCode ErrorCode { get; }

		public FilesystemResult(FileSystemStatusCode errorCode)
		{
			ErrorCode = errorCode;
		}

		public static implicit operator FileSystemStatusCode(FilesystemResult res)
		{
			return res.ErrorCode;
		}

		public static implicit operator FilesystemResult(FileSystemStatusCode res)
		{
			return new(res);
		}

		public static implicit operator bool(FilesystemResult res)
		{
			return res is not null && res.ErrorCode is FileSystemStatusCode.Success;
		}

		public static explicit operator FilesystemResult(bool res)
		{
			return new(res ? FileSystemStatusCode.Success : FileSystemStatusCode.Generic);
		}
	}

	public class FilesystemResult<T> : FilesystemResult
	{
		public T Result { get; }

		public FilesystemResult(T result, FileSystemStatusCode errorCode) : base(errorCode)
		{
			Result = result;
		}

		public static implicit operator T(FilesystemResult<T> res)
		{
			return res.Result;
		}
	}
}
