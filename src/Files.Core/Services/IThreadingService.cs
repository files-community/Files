using System;
using System.Threading.Tasks;

namespace Files.Core.Services
{
	public interface IThreadingService
	{
		Task ExecuteOnUiThreadAsync(Action action);

		Task<TResult?> ExecuteOnUiThreadAsync<TResult>(Func<TResult?> func);
	}
}
