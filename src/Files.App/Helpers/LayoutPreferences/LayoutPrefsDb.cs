using Files.Shared.Extensions;
using LiteDB;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Files.App.Helpers.LayoutPreferences
{
	public class LayoutPrefsDb : IDisposable
	{
		private readonly LiteDatabase db;
		private readonly IEnumerator mutexCoroutine;
		private static readonly Mutex dbMutex = new(false, "Files_LayoutSettingsDb");
		private static readonly ConcurrentQueue<Action> backgroundMutexOperationQueue = new();

		static LayoutPrefsDb()
		{
			new Thread(OperationQueueWorker) { IsBackground = true }.Start();
		}

		private static void OperationQueueWorker()
		{
			while (!Environment.HasShutdownStarted)
			{
				SpinWait.SpinUntil(() => !backgroundMutexOperationQueue.IsEmpty);
				while (backgroundMutexOperationQueue.TryDequeue(out var action))
				{
					action();
				}
			}
		}

		public LayoutPrefsDb(string connection)
		{
			mutexCoroutine = MutexOperator().GetEnumerator();
			mutexCoroutine.MoveNext();
			db = new LiteDatabase(new ConnectionString(connection)
			{
				Connection = ConnectionType.Direct,
				Upgrade = true
			}, new BsonMapper() { IncludeFields = true });
		}

		private IEnumerable MutexOperator()
		{
			var e1 = new ManualResetEventSlim();
			var e2 = new ManualResetEventSlim();
			backgroundMutexOperationQueue.Enqueue(() =>
			{
				dbMutex.WaitOne();
				e1.Set();
				e2.Wait();
				e2.Dispose();
				dbMutex.ReleaseMutex();
			});
			e1.Wait();
			e1.Dispose();
			yield return default;
			e2.Set();
		}

		public void SetPreferences(string filePath, ulong? frn, LayoutPreferences? prefs)
		{
			// Get a collection (or create, if doesn't exist)
			var col = db.GetCollection<LayoutDbPrefs>("layoutprefs");

			var tmp = _FindPreferences(filePath, frn);
			if (tmp is null)
			{
				if (prefs is not null)
				{
					// Insert new tagged file (Id will be auto-incremented)
					var newPref = new LayoutDbPrefs()
					{
						FilePath = filePath,
						Frn = frn,
						Prefs = prefs
					};
					col.Insert(newPref);
					col.EnsureIndex(x => x.Frn);
					col.EnsureIndex(x => x.FilePath);
				}
			}
			else
			{
				if (prefs is not null)
				{
					// Update file tag
					tmp.Prefs = prefs;
					col.Update(tmp);
				}
				else
				{
					// Remove file tag
					col.Delete(tmp.Id);
				}
			}
		}

		public LayoutPreferences? GetPreferences(string? filePath = null, ulong? frn = null)
		{
			return _FindPreferences(filePath, frn)?.Prefs;
		}

		private LayoutDbPrefs? _FindPreferences(string? filePath = null, ulong? frn = null)
		{
			// Get a collection (or create, if doesn't exist)
			var col = db.GetCollection<LayoutDbPrefs>("layoutprefs");

			if (filePath is not null)
			{
				var tmp = col.FindOne(x => x.FilePath == filePath);
				if (tmp is not null)
				{
					if (frn is not null)
					{
						// Keep entry updated
						tmp.Frn = frn;
						col.Update(tmp);
					}
					return tmp;
				}
			}
			if (frn is not null)
			{
				var tmp = col.FindOne(x => x.Frn == frn);
				if (tmp is not null)
				{
					if (filePath is not null)
					{
						// Keep entry updated
						tmp.FilePath = filePath;
						col.Update(tmp);
					}
					return tmp;
				}
			}
			return null;
		}

		public void ResetAll(Func<LayoutDbPrefs, bool>? predicate = null)
		{
			var col = db.GetCollection<LayoutDbPrefs>("layoutprefs");
			if (predicate is null)
			{
				col.DeleteAll();
			}
			else
			{
				col.DeleteMany(x => predicate(x));
			}
		}

		public void ApplyToAll(Action<LayoutDbPrefs> updateAction, Func<LayoutDbPrefs, bool>? predicate = null)
		{
			var col = db.GetCollection<LayoutDbPrefs>("layoutprefs");
			var allDocs = predicate is null ? col.FindAll() : col.Find(x => predicate(x));
			allDocs.ForEach(x => updateAction(x));
			col.Update(allDocs);
		}

		~LayoutPrefsDb()
		{
			Dispose();
		}

		public void Dispose()
		{
			db.Dispose();
			mutexCoroutine.MoveNext();
		}

		public void Import(string json)
		{
			var dataValues = JsonSerializer.Deserialize<LayoutDbPrefs[]>(json);
			var col = db.GetCollection<LayoutDbPrefs>("layoutprefs");
			col.DeleteAll();
			col.InsertBulk(dataValues);
		}

		public string Export()
		{
			return JsonSerializer.Serialize(db.GetCollection<LayoutDbPrefs>("layoutprefs").FindAll());
		}

		public class LayoutDbPrefs
		{
			[BsonId]
			public int Id { get; set; }
			public ulong? Frn { get; set; }
			public string FilePath { get; set; } = string.Empty;
			public LayoutPreferences Prefs { get; set; } = LayoutPreferences.DefaultLayoutPreferences;
		}
	}
}
