using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class FileTagsDb : IDisposable
    {
        private readonly LiteDatabase db;

        public FileTagsDb(string connection)
        {
            db = new LiteDatabase(connection);
        }

        public void SetTag(string filePath, ulong? frn, string tag)
        {
            // Get a collection (or create, if doesn't exist)
            var col = db.GetCollection<TaggedFile>("taggedfiles");

            var tmp = _FindTag(filePath, frn);
            if (tmp == null)
            {
                if (tag != null)
                {
                    // Insert new tagged file (Id will be auto-incremented)
                    var newTag = new TaggedFile
                    {
                        FilePath = filePath,
                        Frn = frn,
                        Tag = tag
                    };
                    col.Insert(newTag);
                }
            }
            else
            {
                if (tag != null)
                {
                    // Update file tag
                    tmp.Tag = tag;
                    col.Update(tmp);
                }
                else
                {
                    // Remove file tag
                    col.Delete(tmp.Id);
                }
            }

            // Index document using frn and path property
            col.EnsureIndex(x => x.Frn);
            col.EnsureIndex(x => x.FilePath);
        }

        private TaggedFile _FindTag(string filePath = null, ulong? frn = null)
        {
            // Get a collection (or create, if doesn't exist)
            var col = db.GetCollection<TaggedFile>("taggedfiles");
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

        public string GetTag(string filePath = null, ulong? frn = null)
        {
            return _FindTag(filePath, frn)?.Tag;
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public class TaggedFile
        {
            [BsonId]
            public int Id { get; set; }
            public ulong? Frn { get; set; }
            public string FilePath { get; set; }
            public string Tag { get; set; }
        }
    }
}
