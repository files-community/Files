// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	public interface IThreadingService
	{
		Task ExecuteOnUiThreadAsync(Action action);

		Task<TResult?> ExecuteOnUiThreadAsync<TResult>(Func<TResult?> func);
	}
}
