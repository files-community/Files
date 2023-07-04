// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Services
{
	public interface IThreadingService
	{
		Task ExecuteOnUiThreadAsync(Action action);

		Task<TResult?> ExecuteOnUiThreadAsync<TResult>(Func<TResult?> func);
	}
}
