use std::path::PathBuf;
use std::sync::mpsc;

use files_search_service::enumerate;

#[test]
fn enumerator_finds_all_files_recursively() {
    let dir = tempdir();
    let sub_a = dir.join("a");
    let sub_b = dir.join("b").join("nested");
    std::fs::create_dir_all(&sub_a).unwrap();
    std::fs::create_dir_all(&sub_b).unwrap();
    std::fs::write(dir.join("top.txt"), b"x").unwrap();
    std::fs::write(sub_a.join("a1.txt"), b"x").unwrap();
    std::fs::write(sub_a.join("a2.txt"), b"x").unwrap();
    std::fs::write(sub_b.join("deep.txt"), b"x").unwrap();

    let (tx, rx) = mpsc::channel();
    enumerate::enumerate(&dir, tx);
    let mut names: Vec<String> = rx
        .into_iter()
        .map(|e| e.path.file_name().unwrap().to_string_lossy().into_owned())
        .collect();
    names.sort();
    assert_eq!(
        names,
        vec!["a1.txt", "a2.txt", "deep.txt", "top.txt"]
    );
}

#[test]
fn enumerator_reports_size_and_modified() {
    let dir = tempdir();
    std::fs::write(dir.join("hello.txt"), b"hello world").unwrap();

    let (tx, rx) = mpsc::channel();
    enumerate::enumerate(&dir, tx);
    let entries: Vec<_> = rx.into_iter().collect();
    assert_eq!(entries.len(), 1);
    assert_eq!(entries[0].size_bytes, b"hello world".len() as u64);
    // Sanity check: modified time is in the last 60 seconds and after epoch.
    let now_ms = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_millis() as i64;
    let delta = (now_ms - entries[0].modified_unix_ms).abs();
    assert!(
        delta < 60_000,
        "modified_unix_ms drift {delta}ms is implausibly large"
    );
}

fn tempdir() -> PathBuf {
    use std::sync::atomic::{AtomicU64, Ordering};
    static COUNTER: AtomicU64 = AtomicU64::new(0);
    let n = COUNTER.fetch_add(1, Ordering::Relaxed);
    let nanos = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let dir = std::env::temp_dir().join(format!("files-search-enum-{nanos}-{n}"));
    std::fs::create_dir_all(&dir).unwrap();
    dir
}
