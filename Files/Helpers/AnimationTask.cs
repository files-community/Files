using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace Files.Helpers
{
    static class AnimationTask
    {
        public static Task<bool> TryExecuteAsync(this ConnectedAnimation animation, UIElement destination)
        {
            var tsc = new TaskCompletionSource<bool>();
            animation.Completed += (sender, args) =>
            {
                tsc.SetResult(true);
            };
            animation.TryStart(destination);

            return tsc.Task;
        }
    }
}
