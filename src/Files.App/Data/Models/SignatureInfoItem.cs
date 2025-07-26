// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Windows.Input;

namespace Files.App.Data.Models
{
    public sealed partial class SignatureInfoItem : ObservableObject
    {
        private string _Version = string.Empty;
        public string Version
        {
            get => _Version;
            set => SetProperty(ref _Version, value);
        }

        private string _IssuedBy = string.Empty;
        public string IssuedBy
        {
            get => _IssuedBy;
            set => SetProperty(ref _IssuedBy, value);
        }

        private string _IssuedTo = string.Empty;
        public string IssuedTo
        {
            get => _IssuedTo;
            set => SetProperty(ref _IssuedTo, value);
        }

        private string _ValidFromTimestamp = string.Empty;
        public string ValidFromTimestamp
        {
            get => _ValidFromTimestamp;
            set => SetProperty(ref _ValidFromTimestamp, value);
        }

        private string _ValidToTimestamp = string.Empty;
        public string ValidToTimestamp
        {
            get => _ValidToTimestamp;
            set => SetProperty(ref _ValidToTimestamp, value);
        }

        private string _VerifiedTimestamp = string.Empty;
        public string VerifiedTimestamp
        {
            get => _VerifiedTimestamp;
            set => SetProperty(ref _VerifiedTimestamp, value);
        }

        private bool _Verified = false;
        public bool Verified
        {
            get => _Verified;
            set
            {
                if (SetProperty(ref _Verified, value))
                    OnPropertyChanged(nameof(Glyph));
            }
        }

        public List<CertNodeInfoItem> SignChain { get; }

        public string Glyph => Verified ? "\uE930" : "\uEA39";

        public ICommand OpenDetailsCommand { get; }

        public SignatureInfoItem(List<CertNodeInfoItem> chain)
        {
            SignChain = chain ?? new List<CertNodeInfoItem>();
            OpenDetailsCommand = new AsyncRelayCommand(DoOpenDetails);
        }

        private Task DoOpenDetails()
        {
            return Task.CompletedTask;
        }
    }
}
