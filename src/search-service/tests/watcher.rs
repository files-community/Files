use std::path::PathBuf;
use std::sync::Arc;
use std::time::{Duration, Instant};

use files_search_service::{SearchIndex, Watcher};

/// Polls `cond` every 25ms for up to `timeout`, returning true if it
/// ever returned true. Filesystem watchers are inherently async so we
/// can't assert synchronously after writing a file.
fn wait_until<F: FnMut() -> bool>(timeout: Duration, mut cond: F) -> bool {
    let start = Instant::now();
    while start.elapsed() < timeout {
        if cond() {
            return true;
        }
        std::thread::sleep(Duration::from_millis(25));
    }
    false
}

fn search_count(index: &SearchIndex, query: &str) -> usize {
    index
        .search(query, 100, &[])
        .map(|hits| hits.len())
        .unwrap_or(0)
}

#[test]
fn watcher_indexes_new_files() {
    let (root, index_dir) = tempdirs();
    let index = Arc::new(SearchIndex::open_or_build(&index_dir, &root).unwrap());
    let watcher = Watcher::start(root.clone(), Arc::clone(&index), None).unwrap();

    std::fs::write(root.join("brandnew.txt"), b"x").unwrap();
    let saw = wait_until(Duration::from_secs(5), || {
        search_count(&index, "brandnew") > 0
    });
    watcher.stop();
    assert!(saw, "watcher should index new files within 5s");
}

#[test]
fn watcher_removes_deleted_files() {
    let (root, index_dir) = tempdirs();
    let target = root.join("doomed.txt");
    std::fs::write(&target, b"x").unwrap();

    let index = Arc::new(SearchIndex::open_or_build(&index_dir, &root).unwrap());
    assert_eq!(search_count(&index, "doomed"), 1);

    let watcher = Watcher::start(root.clone(), Arc::clone(&index), None).unwrap();
    std::fs::remove_file(&target).unwrap();

    let gone = wait_until(Duration::from_secs(5), || {
        search_count(&index, "doomed") == 0
    });
    watcher.stop();
    assert!(gone, "watcher should remove deleted files within 5s");
}

#[test]
fn watcher_picks_up_files_in_subdirs() {
    let (root, index_dir) = tempdirs();
    let sub = root.join("nested").join("deep");
    std::fs::create_dir_all(&sub).unwrap();

    let index = Arc::new(SearchIndex::open_or_build(&index_dir, &root).unwrap());
    let watcher = Watcher::start(root.clone(), Arc::clone(&index), None).unwrap();

    std::fs::write(sub.join("buried.txt"), b"x").unwrap();
    let saw = wait_until(Duration::from_secs(5), || {
        search_count(&index, "buried") > 0
    });
    watcher.stop();
    assert!(saw, "watcher should follow subdirectories");
}

#[test]
fn watcher_handles_burst_with_single_commit_window() {
    let (root, index_dir) = tempdirs();
    let index = Arc::new(SearchIndex::open_or_build(&index_dir, &root).unwrap());
    let watcher = Watcher::start(root.clone(), Arc::clone(&index), None).unwrap();

    // Simulate a `git checkout`-style burst: 50 files at once.
    for i in 0..50 {
        std::fs::write(root.join(format!("burst_{i:02}.txt")), b"x").unwrap();
    }

    let saw_all = wait_until(Duration::from_secs(10), || {
        search_count(&index, "burst") == 50
    });
    watcher.stop();
    assert!(saw_all, "all 50 burst files should be indexed");
}

fn tempdirs() -> (PathBuf, PathBuf) {
    use std::sync::atomic::{AtomicU64, Ordering};
    static COUNTER: AtomicU64 = AtomicU64::new(0);
    let n = COUNTER.fetch_add(1, Ordering::Relaxed);
    let nanos = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let base = std::env::temp_dir().join(format!("files-search-watch-{nanos}-{n}"));
    let root = base.join("root");
    let index_dir = base.join("index");
    std::fs::create_dir_all(&root).unwrap();
    std::fs::create_dir_all(&index_dir).unwrap();
    (root, index_dir)
}
