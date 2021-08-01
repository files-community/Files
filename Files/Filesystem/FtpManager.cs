using Files.Helpers;
using Files.ViewModels;
using FluentFTP;
using System;
using System.Collections.Generic;

namespace Files.Filesystem
{
    public static class FtpManager
    {
        private readonly static List<WeakReference<ItemViewModel>> _viewModels = new List<WeakReference<ItemViewModel>>();
        private readonly static List<FtpClient> _ftpClients = new List<FtpClient>();
        private readonly static object _lock = new object();

        public static FtpClient GetFtpInstance(this ItemViewModel instance)
        {
            lock (_lock)
            {
                for (var i = _viewModels.Count - 1; i >= 0; i--)
                {
                    if (_viewModels[i].TryGetTarget(out var target) && target is not null)
                    {
                        if (target == instance)
                        {
                            return _ftpClients[i];
                        }
                    }
                    else
                    {
                        if (!_ftpClients[i].IsDisposed)
                        {
                            _ftpClients[i].Dispose();
                        }

                        _viewModels.RemoveAt(i);
                        _ftpClients.RemoveAt(i);
                    }
                }

                _viewModels.Add(new WeakReference<ItemViewModel>(instance));
                var client = new FtpClient();
                _ftpClients.Add(client);

                return client;
            }
        }

        public static void DisposeUnused()
        {
            lock (_lock)
            {
                for (var i = _viewModels.Count - 1; i >= 0; i--)
                {
                    if (!_viewModels[i].TryGetTarget(out var target) || target is null)
                    {
                        if (!_ftpClients[i].IsDisposed)
                        {
                            _ftpClients[i].Dispose();
                        }

                        _viewModels.RemoveAt(i);
                        _ftpClients.RemoveAt(i);
                    }
                }
            }
        }
    }
}
