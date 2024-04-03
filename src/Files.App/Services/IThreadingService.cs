// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services
{
	public interface IThreadingService
	{
		Task ExecuteOnUiThreadAsync(Action action);

		Task<TResult?> ExecuteOnUiThreadAsync<TResult>(Func<TResult?> func);
	}
}
