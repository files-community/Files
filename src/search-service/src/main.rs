use std::path::PathBuf;
use std::sync::Arc;

use tonic::transport::Server;
use tracing::info;

use files_search_service::proto::files_search_server::FilesSearchServer;
use files_search_service::{
    apply_background_priority, default_index_dir, SearchIndex, Service, Throttle, Watcher,
};

fn resolve_root() -> PathBuf {
    let raw = std::env::var("FILES_SEARCH_ROOT")
        .or_else(|_| std::env::var("USERPROFILE"))
        .map(PathBuf::from)
        .unwrap_or_else(|_| PathBuf::from("."));
    normalize_root(raw)
}

/// Normalize the indexing root so stored paths are byte-identical to
/// what a Windows caller (e.g. `Path.GetFullPath` from C#) will pass in
/// `scope_paths`. Without this, mixed forward/backward slashes silently
/// break prefix scoping.
///
/// Strategy:
///   1. `fs::canonicalize` to resolve `..`, symlinks, and case.
///   2. Strip the `\\?\` UNC prefix Windows adds, since C# callers
///      don't include it.
///
/// Falls back to the input on canonicalize failure (path doesn't exist
/// yet, permissions, etc.).
fn normalize_root(p: PathBuf) -> PathBuf {
    let canonical = match std::fs::canonicalize(&p) {
        Ok(c) => c,
        Err(_) => return p,
    };
    #[cfg(windows)]
    {
        let s = canonical.to_string_lossy();
        if let Some(stripped) = s.strip_prefix(r"\\?\") {
            return PathBuf::from(stripped);
        }
    }
    canonical
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    tracing_subscriber::fmt()
        .with_env_filter(
            tracing_subscriber::EnvFilter::try_from_default_env()
                .unwrap_or_else(|_| "info".into()),
        )
        .init();

    apply_background_priority();

    let root = resolve_root();
    let index_dir = default_index_dir();
    info!(root = %root.display(), index_dir = %index_dir.display(), "starting");

    let root_for_index = root.clone();
    let index = Arc::new(
        tokio::task::spawn_blocking(move || {
            SearchIndex::open_or_build(&index_dir, &root_for_index)
        })
        .await??,
    );

    let throttle = Arc::new(Throttle::start());
    let watcher = Watcher::start(root.clone(), Arc::clone(&index), Some(Arc::clone(&throttle)))?;
    info!(root = %root.display(), "watcher started");

    // TCP for v0; swap to named pipe (\\.\pipe\files-search) once the
    // service does enough to be worth integration-testing from C#.
    let addr = "127.0.0.1:50080".parse()?;
    info!(%addr, "files-search-service listening");

    Server::builder()
        .add_service(FilesSearchServer::new(Service::new(index)))
        .serve_with_shutdown(addr, async {
            let _ = tokio::signal::ctrl_c().await;
            info!("shutting down");
        })
        .await?;

    watcher.stop();
    Ok(())
}
