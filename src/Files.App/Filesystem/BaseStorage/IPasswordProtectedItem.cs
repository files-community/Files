using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Filesystem.StorageItems
{
	public enum AccessResult
	{
		Success,
		NeedsAuth,
		Failed
	}

	public interface IPasswordProtectedItem<T>
	{
		T Credentials { set; }
		Task<AccessResult> CheckAccess(T credentials);
	}
}
