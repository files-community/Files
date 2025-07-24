// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Signatures;

namespace Files.App.ViewModels.Properties
{
    public sealed partial class SignaturesViewModel : ObservableObject, IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;

        public ObservableCollection<SignatureInfoItem> Signatures { get; set; }

        public SignaturesViewModel(ListedItem item)
        {
            _cancellationTokenSource = new();
            Signatures = new();

            var signatures = DigitalSignaturesUtil.GetSignaturesOfItem(item.ItemPath);
            signatures.ForEach(s => Signatures.Add(s));
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
