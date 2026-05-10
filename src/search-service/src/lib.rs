use std::path::PathBuf;
use std::pin::Pin;
use std::sync::Arc;

use tokio_stream::Stream;
use tonic::{Request, Response, Status};
use tracing::info;

pub mod enumerate;
pub mod index;
pub mod throttle;
pub mod watcher;
pub mod proto {
    tonic::include_proto!("files.search.v1");
}

pub use index::{default_index_dir, SearchIndex};
pub use throttle::{apply_background_priority, Throttle};
pub use watcher::Watcher;

use proto::files_search_server::FilesSearch;
use proto::{HealthRequest, HealthResponse, SearchHit, SearchRequest};

pub struct Service {
    index: Arc<SearchIndex>,
}

impl Service {
    pub fn new(index: Arc<SearchIndex>) -> Self {
        Self { index }
    }
}

type SearchStream = Pin<Box<dyn Stream<Item = Result<SearchHit, Status>> + Send + 'static>>;

#[tonic::async_trait]
impl FilesSearch for Service {
    async fn health(
        &self,
        _: Request<HealthRequest>,
    ) -> Result<Response<HealthResponse>, Status> {
        Ok(Response::new(HealthResponse {
            version: env!("CARGO_PKG_VERSION").to_string(),
            indexed_file_count: self.index.doc_count(),
            indexing: false,
        }))
    }

    type SearchStream = SearchStream;

    async fn search(
        &self,
        req: Request<SearchRequest>,
    ) -> Result<Response<Self::SearchStream>, Status> {
        let req = req.into_inner();
        // 0 = "no caller cap." We still bound the collector to keep
        // Tantivy's TopDocs from allocating a heap sized by usize::MAX
        // (it multiplies internally and overflows). 10k is generous for
        // a UI-driven search; the C# client typically asks for far less.
        let max = match req.max_results {
            0 => 10_000,
            n => n as usize,
        };
        let scope: Vec<PathBuf> = req.scope_paths.iter().map(PathBuf::from).collect();
        let index = Arc::clone(&self.index);
        let query = req.query.clone();

        info!(query = %req.query, max, scope = scope.len(), "search");

        // Run the synchronous Tantivy search on a blocking task so the
        // async runtime stays unblocked. For small corpora this is
        // overkill, but it keeps the wiring honest as corpora grow.
        let hits = tokio::task::spawn_blocking(move || index.search(&query, max, &scope))
            .await
            .map_err(|e| Status::internal(format!("join error: {e}")))?
            .map_err(|e| Status::internal(format!("search error: {e}")))?;

        let stream = async_stream::try_stream! {
            for hit in hits {
                yield SearchHit {
                    path: hit.path,
                    filename: hit.filename,
                    size_bytes: hit.size_bytes,
                    modified_unix_ms: hit.modified_unix_ms,
                    score: hit.score,
                };
            }
        };

        Ok(Response::new(Box::pin(stream)))
    }
}
