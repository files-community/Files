// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.Threading.Tasks;

namespace Files.Backend.Services
{
	public interface IThreadingService
	{
		Task ExecuteOnUiThreadAsync(Action action);

		Task<TResult?> ExecuteOnUiThreadAsync<TResult>(Func<TResult?> func);
	}
}
