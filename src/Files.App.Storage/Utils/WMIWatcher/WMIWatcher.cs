// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Generic;

namespace Files.App.Storage
{
	public class WMIWatcher : IDisposable, IObserver<CimSubscriptionResult>
	{
		public delegate void WMIEventHandler(object sender, WMIEventArgs e);

		// Events

		public event WMIEventHandler EventArrived = delegate { };

		// Fields

		internal static readonly string DefaultNameSpace = @"root\cimv2";
		internal static readonly string DefaultQueryDialect = "WQL";

		private readonly string _computerName;
		private readonly string _nameSpace;
		private readonly string _queryDialect;
		private readonly string _queryExpression;

		private object _myLock;
		private CimWatcherStatus _cimWatcherStatus;
		private CimSession _cimSession;
		private CimAsyncMultipleResults<CimSubscriptionResult> _cimObservable;
		private IDisposable _subscription;

		private bool _isDisposed;

		// Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="WMIWatcher" /> class.
		/// </summary>
		public WMIWatcher(WMIQuery query)
		{
			string queryExpression = query.QueryExpression;

			if (string.IsNullOrWhiteSpace(queryExpression))
				throw new ArgumentNullException(nameof(queryExpression));

			_nameSpace = DefaultNameSpace;
			_queryDialect = DefaultQueryDialect;
			_queryExpression = queryExpression;

			Initialize();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WMIWatcher" /> class.
		/// </summary>
		public WMIWatcher(string queryDialect, string queryExpression)
		{
			if (string.IsNullOrWhiteSpace(queryExpression))
				throw new ArgumentNullException(nameof(queryExpression));

			_nameSpace = DefaultNameSpace;
			_queryDialect = queryDialect ?? DefaultQueryDialect;
			_queryExpression = queryExpression;

			Initialize();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WMIWatcher" /> class.
		/// </summary>
		public WMIWatcher(string nameSpace, string queryDialect, string queryExpression)
		{
			if (string.IsNullOrWhiteSpace(queryExpression))
				throw new ArgumentNullException(nameof(queryExpression));

			_nameSpace = nameSpace ?? DefaultNameSpace;
			_queryDialect = queryDialect ?? DefaultQueryDialect;
			_queryExpression = queryExpression;

			Initialize();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WMIWatcher" /> class.
		/// </summary>
		public WMIWatcher(string computerName, string nameSpace, string queryDialect, string queryExpression)
		{
			if (string.IsNullOrWhiteSpace(queryExpression))
				throw new ArgumentNullException(nameof(queryExpression));

			_computerName = computerName;
			_nameSpace = nameSpace ?? DefaultNameSpace;
			_queryDialect = queryDialect ?? DefaultQueryDialect;
			_queryExpression = queryExpression;

			Initialize();
		}

		// Methods

		public void Initialize()
		{
			_cimWatcherStatus = CimWatcherStatus.Default;
			_myLock = new object();

			_cimSession = CimSession
				.Create(_computerName);

			_cimObservable = _cimSession
				.SubscribeAsync(_nameSpace, _queryDialect, _queryExpression);
		}

		private void OnEventArrived(WMIEventArgs cimWatcherEventArgs)
		{
			Volatile
				.Read(ref EventArrived)
				.Invoke(this, cimWatcherEventArgs);
		}

		public void OnCompleted()
		{
		}

		public void OnError(Exception error)
		{
		}

		public void OnNext(CimSubscriptionResult cimSubscriptionResult)
		{
			OnEventArrived(new WMIEventArgs(cimSubscriptionResult));
		}

		public void Start()
		{
			lock (_myLock)
			{
				if (_isDisposed)
					throw new ObjectDisposedException(nameof(WMIWatcher));

				if (_cimWatcherStatus != CimWatcherStatus.Default &&
					_cimWatcherStatus != CimWatcherStatus.Stopped)
					return;

				_subscription = _cimObservable.Subscribe(this);

				_cimWatcherStatus = CimWatcherStatus.Started;
			}
		}

		public void Stop()
		{
			lock (_myLock)
			{
				if (_isDisposed)
					throw new ObjectDisposedException(nameof(WMIWatcher));

				if (_cimWatcherStatus != CimWatcherStatus.Started)
					return;

				_subscription?.Dispose();

				_cimWatcherStatus = CimWatcherStatus.Stopped;
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					_subscription?.Dispose();
					_cimSession?.Dispose();
				}

				_isDisposed = true;
			}
		}

		// Disposer

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
