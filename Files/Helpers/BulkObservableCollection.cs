using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Files.Helpers
{
    public class BulkObservableCollection<T> : ObservableCollection<T>
    {
        private bool isBulkOperationStarted;

        public void BeginBulkOperation()
        {
            isBulkOperationStarted = true;
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!isBulkOperationStarted)
            {
                base.OnCollectionChanged(e);
            }
        }

        public void EndBulkOperation()
        {
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            isBulkOperationStarted = false;
        }
    }
}