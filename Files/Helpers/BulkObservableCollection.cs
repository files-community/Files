using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Files.Helpers
{
    public class BulkObservableCollection<T> : ObservableCollection<T>
    {
        private bool _isBulkOperationStarted;

        public void BeginBulkOperation()
        {
            _isBulkOperationStarted = true;
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_isBulkOperationStarted)
            {
                base.OnCollectionChanged(e);
            }
        }

        public void EndBulkOperation()
        {
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            _isBulkOperationStarted = false;
        }
    }
}