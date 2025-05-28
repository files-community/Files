using System.Runtime.InteropServices;
using Windows.AI.Actions;
using Windows.AI.Actions.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using WinRT;

namespace Files.App.Extensions
{
	internal class ActionManager
	{
		internal static ActionManager Instance => _instance ??= new();

		private static ActionManager? _instance;

		// NOTE: This Guid is subject to change in the future
		private static readonly Guid IActionRuntimeIID = Guid.Parse("206EFA2C-C909-508A-B4B0-9482BE96DB9C");

		// Public API usage (ActionCatalog)
		private const string ActionRuntimeClsidStr = "C36FEF7E-35F3-4192-9F2C-AF1FD425FB85";

		internal ActionEntityFactory EntityFactory => ActionRuntime.EntityFactory;

		internal ActionRuntime? ActionRuntime;
		internal ActionCatalog ActionCatalog => ActionRuntime.ActionCatalog;

		private ActionManager()
		{
			ActionRuntime = CreateActionRuntime();
		}

		public static unsafe Windows.AI.Actions.ActionRuntime? CreateActionRuntime()
		{
			IntPtr abiPtr = default;
			try
			{
				Guid classId = Guid.Parse(ActionRuntimeClsidStr);
				Guid iid = IActionRuntimeIID;

				HRESULT hresult = PInvoke.CoCreateInstance(&classId, null, CLSCTX.CLSCTX_LOCAL_SERVER, &iid, (void**)&abiPtr);
				Marshal.ThrowExceptionForHR((int)hresult);

				return MarshalInterface<Windows.AI.Actions.ActionRuntime>.FromAbi(abiPtr);
			}
			catch
			{
				return null;
			}
			finally
			{
				MarshalInspectable<object>.DisposeAbi(abiPtr);
			}
		}
	}
}