using LiteDB;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Common
{
	public class FileTagsDb : IDisposable
	{
		private readonly LiteDatabase db;
		private readonly IEnumerator mutexCoroutine;
		private static readonly Mutex dbMutex = new(false, "Files_FileTagDb");
		private static readonly ConcurrentQueue<Action> backgroundMutexOperationQueue = new();
		private const string TaggedFiles = "taggedfiles";

		static FileTagsDb()
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

		public FileTagsDb(string connection)
		{
			mutexCoroutine = MutexOperator().GetEnumerator();
			mutexCoroutine.MoveNext();
			db = new LiteDatabase(new ConnectionString(connection)
			{
				Connection = ConnectionType.Direct,
				Upgrade = true
			});
			UpdateDb();
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

		public void SetTags(string filePath, ulong? frn, string[]? tags)
		{
			// Get a collection (or create, if doesn't exist)
			var col = db.GetCollection<TaggedFile>(TaggedFiles);

			var tmp = _FindTag(filePath, frn);
			if (tmp is null)
			{
				if (tags is not null && tags.Any())
				{
					// Insert new tagged file (Id will be auto-incremented)
					var newTag = new TaggedFile()
					{
						FilePath = filePath,
						Frn = frn,
						Tags = tags
					};
					col.Insert(newTag);
					col.EnsureIndex(x => x.Frn);
					col.EnsureIndex(x => x.FilePath);
				}
			}
			else
			{
				if (tags is not null && tags.Any())
				{
					// Update file tag
					tmp.Tags = tags;
					col.Update(tmp);
				}
				else
				{
					// Remove file tag
					col.Delete(tmp.Id);
				}
			}
		}

		private TaggedFile? _FindTag(string? filePath = null, ulong? frn = null)
		{
			// Get a collection (or create, if doesn't exist)
			var col = db.GetCollection<TaggedFile>(TaggedFiles);

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

		public void UpdateTag(string oldFilePath, ulong? frn = null, string? newFilePath = null)
		{
			// Get a collection (or create, if doesn't exist)
			var col = db.GetCollection<TaggedFile>(TaggedFiles);
			var tmp = col.FindOne(x => x.FilePath == oldFilePath);
			if (tmp is not null)
			{
				if (frn is not null)
				{
					tmp.Frn = frn;
					col.Update(tmp);
				}

				if (newFilePath is not null)
				{
					tmp.FilePath = newFilePath;
					col.Update(tmp);
				}
			}
		}

		public void UpdateTag(ulong oldFrn, ulong? frn = null, string? newFilePath = null)
		{
			// Get a collection (or create, if doesn't exist)
			var col = db.GetCollection<TaggedFile>(TaggedFiles);
			var tmp = col.FindOne(x => x.Frn == oldFrn);
			if (tmp is not null)
			{
				if (frn is not null)
				{
					tmp.Frn = frn;
					col.Update(tmp);
				}

				if (newFilePath is not null)
				{
					tmp.FilePath = newFilePath;
					col.Update(tmp);
				}
			}
		}

		public string[]? GetTags(string? filePath = null, ulong? frn = null)
		{
			return _FindTag(filePath, frn)?.Tags;
		}

		public IEnumerable<TaggedFile> GetAll()
		{
			var col = db.GetCollection<TaggedFile>(TaggedFiles);
			return col.FindAll();
		}

		public IEnumerable<TaggedFile> GetAllUnderPath(string folderPath)
		{
			var col = db.GetCollection<TaggedFile>(TaggedFiles);
			return col.Find(x => x.FilePath.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase));
		}

		~FileTagsDb()
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
			var dataValues = JsonSerializer.Deserialize<TaggedFile[]>(json);
			var col = db.GetCollection<TaggedFile>(TaggedFiles);
			col.DeleteAll();
			col.InsertBulk(dataValues);
		}

		public string Export()
		{
			return JsonSerializer.Serialize(db.GetCollection<TaggedFile>(TaggedFiles).FindAll());
		}

		private void UpdateDb()
		{
			if (db.UserVersion == 0)
			{
				var col = db.GetCollection(TaggedFiles);
				foreach (var doc in col.FindAll())
				{
					doc["Tags"] = new BsonValue(new[] { doc["Tag"].AsString });
					doc.Remove("Tags");
					col.Update(doc);
				}
				db.UserVersion = 1;
			}
		}

		public class TaggedFile
		{
			[BsonId] public int Id { get; set; }
			public ulong? Frn { get; set; }
			public string FilePath { get; set; } = string.Empty;
			public string[] Tags { get; set; } = Array.Empty<string>();
		}
	}
}