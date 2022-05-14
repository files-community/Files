using LiteDB;
using System;

namespace Files.Uwp.Helpers.LayoutPreferences
{
    public class LayoutPrefsDb : IDisposable
    {
        private readonly LiteDatabase db;

        public LayoutPrefsDb(string connection, bool shared = false)
        {
            db = new LiteDatabase(new ConnectionString(connection)
            {
                Mode = shared ? FileMode.Shared : FileMode.Exclusive
            }, new BsonMapper() { IncludeFields = true });
        }

        public void SetPreferences(string filePath, ulong? frn, LayoutPreferences prefs)
        {
            // Get a collection (or create, if doesn't exist)
            var col = db.GetCollection<LayoutDbPrefs>("layoutprefs");

            var tmp = _FindPreferences(filePath, frn);
            if (tmp == null)
            {
                if (prefs != null)
                {
                    // Insert new tagged file (Id will be auto-incremented)
                    var newPref = new LayoutDbPrefs
                    {
                        FilePath = filePath,
                        Frn = frn,
                        Prefs = prefs
                    };
                    col.Insert(newPref);
                }
            }
            else
            {
                if (prefs != null)
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

        public LayoutPreferences GetPreferences(string filePath = null, ulong? frn = null)
        {
            return _FindPreferences(filePath, frn)?.Prefs;
        }

        private LayoutDbPrefs _FindPreferences(string filePath = null, ulong? frn = null)
        {
            // Get a collection (or create, if doesn't exist)
            var col = db.GetCollection<LayoutDbPrefs>("layoutprefs");
            if (filePath != null)
            {
                var tmp = col.FindOne(x => x.FilePath == filePath);
                if (tmp != null)
                {
                    if (frn != null)
                    {
                        // Keep entry updated
                        tmp.Frn = frn;
                        col.Update(tmp);
                    }
                    return tmp;
                }
            }
            if (frn != null)
            {
                var tmp = col.FindOne(x => x.Frn == frn);
                if (tmp != null)
                {
                    if (filePath != null)
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

        public void Dispose()
        {
            db.Dispose();
        }

        public class LayoutDbPrefs
        {
            [BsonId]
            public int Id { get; set; }
            public ulong? Frn { get; set; }
            public string FilePath { get; set; }
            public LayoutPreferences Prefs { get; set; }
        }
    }
}
