use std::path::PathBuf;
use std::sync::Arc;
use std::time::Duration;

use files_search_service::proto::files_search_client::FilesSearchClient;
use files_search_service::proto::files_search_server::FilesSearchServer;
use files_search_service::proto::{HealthRequest, SearchRequest};
use files_search_service::{SearchIndex, Service};
use tokio::net::TcpListener;
use tokio::sync::oneshot;
use tokio_stream::wrappers::TcpListenerStream;
use tokio_stream::StreamExt;
use tonic::transport::{Endpoint, Server};

struct ServiceHandle {
    url: String,
    shutdown: Option<oneshot::Sender<()>>,
    task: Option<tokio::task::JoinHandle<()>>,
}

impl ServiceHandle {
    async fn stop(mut self) {
        if let Some(tx) = self.shutdown.take() {
            let _ = tx.send(());
        }
        if let Some(task) = self.task.take() {
            let _ = task.await;
        }
    }
}

async fn spawn_service(root: PathBuf, index_dir: PathBuf) -> ServiceHandle {
    let listener = TcpListener::bind("127.0.0.1:0").await.unwrap();
    let addr = listener.local_addr().unwrap();
    let index = Arc::new(
        tokio::task::spawn_blocking(move || SearchIndex::open_or_build(&index_dir, &root))
            .await
            .unwrap()
            .unwrap(),
    );
    let (tx, rx) = oneshot::channel();
    let task = tokio::spawn(async move {
        Server::builder()
            .add_service(FilesSearchServer::new(Service::new(index)))
            .serve_with_incoming_shutdown(TcpListenerStream::new(listener), async {
                let _ = rx.await;
            })
            .await
            .unwrap();
    });
    tokio::time::sleep(Duration::from_millis(50)).await;
    ServiceHandle {
        url: format!("http://{addr}"),
        shutdown: Some(tx),
        task: Some(task),
    }
}

async fn connect(url: String) -> FilesSearchClient<tonic::transport::Channel> {
    let channel = Endpoint::from_shared(url).unwrap().connect().await.unwrap();
    FilesSearchClient::new(channel)
}

#[tokio::test]
async fn health_reports_indexed_count() {
    let (root, index_dir) = tempdirs();
    std::fs::write(root.join("alpha.txt"), b"a").unwrap();
    std::fs::write(root.join("beta.txt"), b"b").unwrap();

    let svc = spawn_service(root, index_dir).await;
    let mut client = connect(svc.url.clone()).await;
    let resp = client.health(HealthRequest {}).await.unwrap().into_inner();
    assert_eq!(resp.indexed_file_count, 2);
    assert!(!resp.indexing);
    assert!(!resp.version.is_empty());
    svc.stop().await;
}

#[tokio::test]
async fn search_returns_substring_matches() {
    let (root, index_dir) = tempdirs();
    std::fs::write(root.join("alpha.txt"), b"a").unwrap();
    std::fs::write(root.join("beta.txt"), b"b").unwrap();
    std::fs::write(root.join("ALPHABET.md"), b"c").unwrap();

    let svc = spawn_service(root, index_dir).await;
    let mut client = connect(svc.url.clone()).await;
    let mut stream = client
        .search(SearchRequest {
            query: "alpha".into(),
            max_results: 0,
            scope_paths: vec![],
        })
        .await
        .unwrap()
        .into_inner();

    let mut names = Vec::new();
    while let Some(hit) = stream.next().await {
        names.push(hit.unwrap().filename);
    }
    names.sort();
    assert_eq!(names, vec!["ALPHABET.md", "alpha.txt"]);
    svc.stop().await;
}

#[tokio::test]
async fn search_honors_max_results() {
    let (root, index_dir) = tempdirs();
    for i in 0..10 {
        std::fs::write(root.join(format!("hit_{i}.txt")), b"x").unwrap();
    }

    let svc = spawn_service(root, index_dir).await;
    let mut client = connect(svc.url.clone()).await;
    let mut stream = client
        .search(SearchRequest {
            query: "hit".into(),
            max_results: 3,
            scope_paths: vec![],
        })
        .await
        .unwrap()
        .into_inner();

    let mut count = 0;
    while let Some(hit) = stream.next().await {
        hit.unwrap();
        count += 1;
    }
    assert_eq!(count, 3);
    svc.stop().await;
}

#[tokio::test]
async fn search_scope_filters_paths() {
    let (root, index_dir) = tempdirs();
    let inside = root.join("inside");
    let outside = root.join("outside");
    std::fs::create_dir(&inside).unwrap();
    std::fs::create_dir(&outside).unwrap();
    std::fs::write(inside.join("match.txt"), b"x").unwrap();
    std::fs::write(outside.join("match.txt"), b"x").unwrap();

    let svc = spawn_service(root, index_dir).await;
    let mut client = connect(svc.url.clone()).await;
    let mut stream = client
        .search(SearchRequest {
            query: "match".into(),
            max_results: 0,
            scope_paths: vec![inside.to_string_lossy().into_owned()],
        })
        .await
        .unwrap()
        .into_inner();

    let mut paths = Vec::new();
    while let Some(hit) = stream.next().await {
        paths.push(hit.unwrap().path);
    }
    assert_eq!(paths.len(), 1);
    assert!(paths[0].contains("inside"));
    svc.stop().await;
}

#[tokio::test]
async fn index_persists_across_restarts() {
    let (root, index_dir) = tempdirs();
    std::fs::write(root.join("persistent.txt"), b"x").unwrap();

    // First start: builds index from root.
    {
        let svc = spawn_service(root.clone(), index_dir.clone()).await;
        let mut client = connect(svc.url.clone()).await;
        let resp = client.health(HealthRequest {}).await.unwrap().into_inner();
        assert_eq!(resp.indexed_file_count, 1);
        svc.stop().await; // Releases the Tantivy writer lock.
    }

    // Second start: deletes the source root, opens existing index.
    // Expectation: docs survive because the index was committed to disk.
    std::fs::remove_dir_all(&root).unwrap();
    let empty_root = root.clone();
    std::fs::create_dir_all(&empty_root).unwrap();

    let svc = spawn_service(empty_root, index_dir).await;
    let mut client = connect(svc.url.clone()).await;
    let resp = client.health(HealthRequest {}).await.unwrap().into_inner();
    assert_eq!(resp.indexed_file_count, 1);

    let mut stream = client
        .search(SearchRequest {
            query: "persistent".into(),
            max_results: 0,
            scope_paths: vec![],
        })
        .await
        .unwrap()
        .into_inner();
    let mut found = false;
    while let Some(hit) = stream.next().await {
        if hit.unwrap().filename == "persistent.txt" {
            found = true;
        }
    }
    assert!(found, "persisted doc should survive a restart");
    svc.stop().await;
}

fn tempdirs() -> (PathBuf, PathBuf) {
    use std::sync::atomic::{AtomicU64, Ordering};
    static COUNTER: AtomicU64 = AtomicU64::new(0);
    let n = COUNTER.fetch_add(1, Ordering::Relaxed);
    let nanos = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let base = std::env::temp_dir().join(format!("files-search-test-{nanos}-{n}"));
    let root = base.join("root");
    let index_dir = base.join("index");
    std::fs::create_dir_all(&root).unwrap();
    std::fs::create_dir_all(&index_dir).unwrap();
    (root, index_dir)
}
