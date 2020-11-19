using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.ApplicationModel.Core;

namespace Files.Helpers
{
    public class BulkObservableCollection<T> : ObservableCollection<T>
    {
        private bool _isBlukOperationStarted;

        public void BeginBulkOperation()
        {
            _isBlukOperationStarted = true;
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            
                if (!_isBlukOperationStarted)
                {
                    base.OnCollectionChanged(e);
                }
           
        }

        public void EndBulkOperation()
        {
            CoreApplication.MainView.DispatcherQueue.TryEnqueue(Windows.System.DispatcherQueuePriority.Normal, () =>
            {
                base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                _isBlukOperationStarted = false;
            });
        }
    }
}