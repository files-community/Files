using System.Threading.Tasks;

namespace Files.Extensions
{
    internal static class TaskExtensions
    {
#pragma warning disable RCS1175 // Unused this parameter.
#pragma warning disable IDE0060 // Remove unused parameter

        /// <summary>
        /// This function is to explicitly state that we know that we're running task without awaiting.
        /// This makes Visual Studio drop the warning, but the programmer intent is still clearly stated.
        /// </summary>
        /// <param name="task"></param>
        internal static void Forget(this Task task)
        {
            // do nothing, just forget about the task
        }

#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1175 // Unused this parameter.
    }
}