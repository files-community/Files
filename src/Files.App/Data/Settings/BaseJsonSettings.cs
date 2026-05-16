// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Windows.Storage;

namespace Files.App.Data.Settings;

public abstract class BaseJsonSettings : IDisposable, INotifyPropertyChanged
{
	private readonly object gate = new();
	private readonly string filePath;
	private readonly TimeSpan saveDelay;
	private Timer? saveTimer;
	private bool isDisposed;
	private bool isLoaded;
	private bool isDirty;
	private bool isHydrating = true;

	public event PropertyChangedEventHandler? PropertyChanged;

	protected BaseJsonSettings(string fileName, TimeSpan? saveDelay = null)
	{
		var folderPath = SystemIO.Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName);
		filePath = SystemIO.Path.Combine(folderPath, fileName);
		this.saveDelay = saveDelay ?? TimeSpan.FromMilliseconds(250);
	}

	protected void Initialize()
	{
		lock (gate)
		{
			ThrowIfDisposed();
			if (isLoaded)
				return;

			var directory = SystemIO.Path.GetDirectoryName(filePath);
			if (!string.IsNullOrEmpty(directory))
				SystemIO.Directory.CreateDirectory(directory);

			if (!SystemIO.File.Exists(filePath))
			{
				isLoaded = true;
				isHydrating = false;
				return;
			}

			var raw = SystemIO.File.ReadAllText(filePath);
			if (!string.IsNullOrWhiteSpace(raw))
			{
				DeserializeCore(raw);
			}

			isLoaded = true;
			isHydrating = false;
		}
	}

	protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
	{
		lock (gate)
		{
			ThrowIfDisposed();
			if (EqualityComparer<T>.Default.Equals(storage, value))
				return false;

			storage = value;
			if (!isHydrating)
			{
				isDirty = true;
				QueueSave_NoLock();
			}
		}

		if (!isHydrating && !string.IsNullOrEmpty(propertyName))
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		return true;
	}

	protected IDisposable BeginHydrationScope()
	{
		return new HydrationScope(this);
	}

	public void SaveNow()
	{
		lock (gate)
		{
			ThrowIfDisposed();
			SaveCore_NoLock();
		}
	}

	public string ExportSettings()
	{
		lock (gate)
		{
			ThrowIfDisposed();
			return ExportCore();
		}
	}

	public bool ImportSettings(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
			return false;

		lock (gate)
		{
			ThrowIfDisposed();
			try
			{
				return ImportCore(json);
			}
			catch (JsonException)
			{
				return false;
			}
		}
	}

	private void QueueSave_NoLock()
	{
		saveTimer ??= new Timer(static s =>
		{
			var self = (BaseJsonSettings)s!;
			lock (self.gate)
			{
				if (self.isDisposed)
					return;
				self.SaveCore_NoLock();
			}
		}, this, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

		saveTimer.Change(saveDelay, Timeout.InfiniteTimeSpan);
	}

	private void SaveCore_NoLock()
	{
		if (!isDirty)
			return;

		var json = SerializeCore();
		SystemIO.File.WriteAllText(filePath, json);
		isDirty = false;
	}

	protected abstract string SerializeCore();
	protected abstract void DeserializeCore(string json);
	protected abstract string ExportCore();
	protected abstract bool ImportCore(string json);

	public void Dispose()
	{
		lock (gate)
		{
			if (isDisposed)
				return;

			SaveCore_NoLock();
			saveTimer?.Dispose();
			saveTimer = null;
			isDisposed = true;
		}

		GC.SuppressFinalize(this);
	}

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
	}

	private sealed class HydrationScope : IDisposable
	{
		private readonly BaseJsonSettings owner;
		private readonly bool previous;
		private bool disposed;

		public HydrationScope(BaseJsonSettings owner)
		{
			this.owner = owner;
			previous = owner.isHydrating;
			owner.isHydrating = true;
		}

		public void Dispose()
		{
			if (disposed)
				return;

			owner.isHydrating = previous;
			disposed = true;
		}
	}
}
