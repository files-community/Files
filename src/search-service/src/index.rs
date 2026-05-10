use std::path::{Path, PathBuf};
use std::sync::mpsc;
use std::time::{Instant, UNIX_EPOCH};

use anyhow::Result;
use parking_lot::Mutex;
use tantivy::collector::TopDocs;
use tantivy::query::{BooleanQuery, FuzzyTermQuery, Occur, Query};
use tantivy::schema::{Field, Schema, INDEXED, STORED, STRING, TEXT};
use tantivy::{doc, Index, IndexReader, IndexWriter, ReloadPolicy, TantivyDocument, Term};
use tracing::info;

use crate::enumerate;

pub struct SearchIndex {
    // Held so reader/writer stay valid; Tantivy's writer/reader keep
    // their own clones internally but we keep the handle in case future
    // code needs to open additional readers.
    #[allow(dead_code)]
    index: Index,
    reader: IndexReader,
    writer: Mutex<IndexWriter>,
    fields: Fields,
}

#[derive(Clone, Copy)]
struct Fields {
    path: Field,
    filename: Field,
    size_bytes: Field,
    modified_unix_ms: Field,
}

#[derive(Debug, Clone)]
pub struct Hit {
    pub path: String,
    pub filename: String,
    pub size_bytes: u64,
    pub modified_unix_ms: i64,
    pub score: f32,
}

impl SearchIndex {
    /// Open an existing index at `dir`, or build a fresh one by walking `root`
    /// and indexing every file. v0 has no startup re-sync — if the directory
    /// already contains an index, it's reused as-is regardless of how stale.
    /// The watcher closes that gap *while the service is running*, but
    /// changes that happen while the service is offline still need a manual
    /// rebuild until step 5 of the roadmap (full restart-time reconcile).
    pub fn open_or_build(dir: &Path, root: &Path) -> Result<Self> {
        std::fs::create_dir_all(dir)?;
        let schema = build_schema();
        let fields = fields_of(&schema);

        let exists = std::fs::read_dir(dir)?.next().is_some();
        let index = if exists {
            info!(dir = %dir.display(), "opening existing index");
            Index::open_in_dir(dir)?
        } else {
            info!(dir = %dir.display(), "creating new index");
            Index::create_in_dir(dir, schema.clone())?
        };

        let writer = index.writer(50_000_000)?;
        let reader = index
            .reader_builder()
            .reload_policy(ReloadPolicy::Manual)
            .try_into()?;

        let this = Self {
            index,
            reader,
            writer: Mutex::new(writer),
            fields,
        };

        if !exists {
            this.full_rebuild(root)?;
        }
        Ok(this)
    }

    pub fn doc_count(&self) -> u64 {
        self.reader.searcher().num_docs()
    }

    /// Drops every document and re-walks `root` from scratch. Used by the
    /// initial cold-start build and exposed for tests.
    pub fn full_rebuild(&self, root: &Path) -> Result<()> {
        let started = Instant::now();
        {
            let mut w = self.writer.lock();
            w.delete_all_documents()?;

            // Producer/consumer: a rayon-fanned-out FindFirstFileEx walk
            // feeds entries through a channel; this thread drains and
            // writes to Tantivy. Keeps disk reads and index inserts
            // overlapped on different cores.
            let (tx, rx) = mpsc::channel();
            let root_owned = root.to_path_buf();
            let producer = std::thread::spawn(move || {
                enumerate::enumerate(&root_owned, tx);
            });

            let mut count = 0u64;
            for entry in rx {
                let Some(name) = entry.path.file_name().and_then(|s| s.to_str()) else {
                    continue;
                };
                w.add_document(doc!(
                    self.fields.path => entry.path.to_string_lossy().into_owned(),
                    self.fields.filename => name.to_string(),
                    self.fields.size_bytes => entry.size_bytes,
                    self.fields.modified_unix_ms => entry.modified_unix_ms,
                ))?;
                count += 1;
            }
            let _ = producer.join();

            w.commit()?;
            info!(
                root = %root.display(),
                count,
                elapsed_ms = started.elapsed().as_millis() as u64,
                "index built"
            );
        }
        self.reader.reload()?;
        Ok(())
    }

    /// Stat `path` and replace its index entry. Removes any existing doc
    /// with the same path first so this is idempotent (good for both
    /// CREATE and MODIFY events from the watcher).
    pub fn upsert(&self, path: &Path) -> Result<()> {
        let Some(name) = path.file_name().and_then(|s| s.to_str()) else {
            return Ok(());
        };
        let metadata = match std::fs::metadata(path) {
            Ok(m) => m,
            // Race: file was deleted between the watcher event and the
            // stat. Treat as a delete so the index doesn't end up with a
            // stale doc.
            Err(_) => return self.delete(path),
        };
        if !metadata.is_file() {
            return Ok(());
        }

        let path_str = path.to_string_lossy().into_owned();
        let size_bytes = metadata.len();
        let modified_unix_ms = metadata
            .modified()
            .ok()
            .and_then(|t| t.duration_since(UNIX_EPOCH).ok())
            .map(|d| d.as_millis() as i64)
            .unwrap_or(0);

        let w = self.writer.lock();
        w.delete_term(Term::from_field_text(self.fields.path, &path_str));
        w.add_document(doc!(
            self.fields.path => path_str,
            self.fields.filename => name.to_string(),
            self.fields.size_bytes => size_bytes,
            self.fields.modified_unix_ms => modified_unix_ms,
        ))?;
        Ok(())
    }

    /// Drop any doc whose path equals `path`. Path is a STRING field
    /// (single token), so `delete_term` is exact-match.
    pub fn delete(&self, path: &Path) -> Result<()> {
        let path_str = path.to_string_lossy().into_owned();
        let w = self.writer.lock();
        w.delete_term(Term::from_field_text(self.fields.path, &path_str));
        Ok(())
    }

    /// Commit pending writes and refresh the reader. Watcher debounces
    /// to keep this cost amortized across bursts of events.
    pub fn commit(&self) -> Result<()> {
        let mut w = self.writer.lock();
        w.commit()?;
        drop(w);
        self.reader.reload()?;
        Ok(())
    }

    /// Per-token prefix query against the filename field, optionally filtered
    /// to results whose path starts with one of `scope_paths`. Tokens are
    /// lowercased; the schema's TEXT field uses the default tokenizer
    /// (lowercase + word-boundary split), so `"alpha"` matches `alpha.txt`
    /// (token `alpha`) and `ALPHABET.md` (token `alphabet`, prefix). True
    /// mid-string substring (`"phab"` → `ALPHABET`) is a known gap; revisit
    /// with an n-gram field if the correctness suite demands it.
    pub fn search(
        &self,
        query: &str,
        max: usize,
        scope_paths: &[PathBuf],
    ) -> Result<Vec<Hit>> {
        let searcher = self.reader.searcher();
        let mut clauses: Vec<(Occur, Box<dyn Query>)> = Vec::new();

        for token in query.split_whitespace() {
            let term = Term::from_field_text(self.fields.filename, &token.to_lowercase());
            clauses.push((
                Occur::Must,
                Box::new(FuzzyTermQuery::new_prefix(term, 0, true)),
            ));
        }

        if !scope_paths.is_empty() {
            let scope_clauses: Vec<(Occur, Box<dyn Query>)> = scope_paths
                .iter()
                .map(|s| {
                    let term =
                        Term::from_field_text(self.fields.path, &s.to_string_lossy());
                    let q: Box<dyn Query> =
                        Box::new(FuzzyTermQuery::new_prefix(term, 0, true));
                    (Occur::Should, q)
                })
                .collect();
            clauses.push((Occur::Must, Box::new(BooleanQuery::new(scope_clauses))));
        }

        // Empty query with no scope = match nothing. The legacy provider
        // returns nothing for a blank query too, so this matches semantics.
        if clauses.is_empty() {
            return Ok(Vec::new());
        }

        let bool_query = BooleanQuery::new(clauses);
        let top = searcher.search(&bool_query, &TopDocs::with_limit(max.max(1)))?;

        let mut hits = Vec::with_capacity(top.len());
        for (score, addr) in top {
            let doc: TantivyDocument = searcher.doc(addr)?;
            hits.push(Hit {
                path: get_text(&doc, self.fields.path).unwrap_or_default(),
                filename: get_text(&doc, self.fields.filename).unwrap_or_default(),
                size_bytes: get_u64(&doc, self.fields.size_bytes).unwrap_or(0),
                modified_unix_ms: get_i64(&doc, self.fields.modified_unix_ms).unwrap_or(0),
                score,
            });
        }
        Ok(hits)
    }
}

fn build_schema() -> Schema {
    let mut sb = Schema::builder();
    sb.add_text_field("path", STRING | STORED);
    sb.add_text_field("filename", TEXT | STORED);
    sb.add_u64_field("size_bytes", STORED | INDEXED);
    sb.add_i64_field("modified_unix_ms", STORED | INDEXED);
    sb.build()
}

fn fields_of(schema: &Schema) -> Fields {
    Fields {
        path: schema.get_field("path").unwrap(),
        filename: schema.get_field("filename").unwrap(),
        size_bytes: schema.get_field("size_bytes").unwrap(),
        modified_unix_ms: schema.get_field("modified_unix_ms").unwrap(),
    }
}

fn get_text(doc: &TantivyDocument, field: Field) -> Option<String> {
    use tantivy::schema::Value;
    doc.get_first(field)
        .and_then(|v| v.as_str())
        .map(|s| s.to_string())
}

fn get_u64(doc: &TantivyDocument, field: Field) -> Option<u64> {
    use tantivy::schema::Value;
    doc.get_first(field).and_then(|v| v.as_u64())
}

fn get_i64(doc: &TantivyDocument, field: Field) -> Option<i64> {
    use tantivy::schema::Value;
    doc.get_first(field).and_then(|v| v.as_i64())
}

/// Returns `%LOCALAPPDATA%\Files\search-index\` with `FILES_SEARCH_INDEX_DIR`
/// override (used by tests and dev runs).
pub fn default_index_dir() -> PathBuf {
    if let Ok(p) = std::env::var("FILES_SEARCH_INDEX_DIR") {
        return PathBuf::from(p);
    }
    let base = std::env::var("LOCALAPPDATA")
        .map(PathBuf::from)
        .unwrap_or_else(|_| std::env::temp_dir());
    base.join("Files").join("search-index")
}
