//! Filesystem watcher.
//!
//! Wraps the `notify` crate (which uses `ReadDirectoryChangesW` with
//! overlapped I/O on Windows) and applies events to a `SearchIndex`.
//! Commits are debounced — bursts of file events (extracting an archive,
//! `git checkout`) collapse into a single Tantivy commit so we don't
//! pay segment + fsync overhead per file.

use std::path::PathBuf;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;
use std::thread;
use std::time::Duration;

use anyhow::Result;
use notify::event::{EventKind, ModifyKind, RenameMode};
use notify::{RecommendedWatcher, RecursiveMode, Watcher as _};
use parking_lot::Mutex;
use tracing::{debug, warn};

use crate::index::SearchIndex;
use crate::throttle::Throttle;

/// Time we wait for a quiet window before committing a batch of edits.
/// 250ms is short enough that single-file changes feel instant in the UI
/// and long enough to coalesce a `git checkout` of hundreds of files.
const COMMIT_DEBOUNCE: Duration = Duration::from_millis(250);

pub struct Watcher {
    _watcher: RecommendedWatcher,
    stop: Arc<AtomicBool>,
    committer: Option<thread::JoinHandle<()>>,
}

impl Watcher {
    pub fn start(
        root: PathBuf,
        index: Arc<SearchIndex>,
        throttle: Option<Arc<Throttle>>,
    ) -> Result<Self> {
        let dirty = Arc::new(AtomicBool::new(false));
        let stop = Arc::new(AtomicBool::new(false));
        let last_event = Arc::new(Mutex::new(std::time::Instant::now()));

        let dirty_for_handler = Arc::clone(&dirty);
        let last_event_for_handler = Arc::clone(&last_event);
        let index_for_handler = Arc::clone(&index);

        let mut watcher = notify::recommended_watcher(move |res: notify::Result<notify::Event>| {
            match res {
                Ok(event) => apply_event(&index_for_handler, &event),
                Err(err) => warn!(%err, "watcher error"),
            }
            dirty_for_handler.store(true, Ordering::Release);
            *last_event_for_handler.lock() = std::time::Instant::now();
        })?;
        watcher.watch(&root, RecursiveMode::Recursive)?;

        let committer = {
            let stop = Arc::clone(&stop);
            let dirty = Arc::clone(&dirty);
            let last_event = Arc::clone(&last_event);
            let index = Arc::clone(&index);
            thread::spawn(move || committer_loop(index, stop, dirty, last_event, throttle))
        };

        Ok(Self {
            _watcher: watcher,
            stop,
            committer: Some(committer),
        })
    }

    /// Stops the committer thread and forces a final commit so any
    /// pending events are durable. The notify watcher itself is dropped
    /// here too, which cancels the underlying ReadDirectoryChangesW.
    pub fn stop(mut self) {
        self.stop.store(true, Ordering::Release);
        if let Some(handle) = self.committer.take() {
            let _ = handle.join();
        }
    }
}

impl Drop for Watcher {
    fn drop(&mut self) {
        self.stop.store(true, Ordering::Release);
        if let Some(handle) = self.committer.take() {
            let _ = handle.join();
        }
    }
}

fn apply_event(index: &SearchIndex, event: &notify::Event) {
    match event.kind {
        EventKind::Create(_) | EventKind::Modify(ModifyKind::Data(_))
        | EventKind::Modify(ModifyKind::Metadata(_))
        | EventKind::Modify(ModifyKind::Any) => {
            for path in &event.paths {
                if let Err(err) = index.upsert(path) {
                    warn!(path = %path.display(), %err, "upsert failed");
                }
            }
        }
        EventKind::Remove(_) => {
            for path in &event.paths {
                if let Err(err) = index.delete(path) {
                    warn!(path = %path.display(), %err, "delete failed");
                }
            }
        }
        EventKind::Modify(ModifyKind::Name(rename)) => {
            // notify normalizes renames into either a single Both event
            // (paths = [old, new]) or two events (From / To). Handle
            // both shapes by deleting any path that no longer exists
            // and upserting any path that does.
            match rename {
                RenameMode::Both if event.paths.len() == 2 => {
                    let _ = index.delete(&event.paths[0]);
                    if let Err(err) = index.upsert(&event.paths[1]) {
                        warn!(path = %event.paths[1].display(), %err, "rename upsert failed");
                    }
                }
                _ => {
                    for path in &event.paths {
                        if path.exists() {
                            let _ = index.upsert(path);
                        } else {
                            let _ = index.delete(path);
                        }
                    }
                }
            }
        }
        // Access events and other modify variants don't change index
        // contents — ignore so we don't churn commits.
        _ => {}
    }
    debug!(?event, "applied");
}

fn committer_loop(
    index: Arc<SearchIndex>,
    stop: Arc<AtomicBool>,
    dirty: Arc<AtomicBool>,
    last_event: Arc<Mutex<std::time::Instant>>,
    throttle: Option<Arc<Throttle>>,
) {
    while !stop.load(Ordering::Acquire) {
        thread::sleep(Duration::from_millis(50));
        if !dirty.load(Ordering::Acquire) {
            continue;
        }
        // Defer commit (and the reader reload that makes new docs
        // visible) while the system is busy. Apply work already happened
        // in the notify callback, so events aren't lost — they just
        // accumulate in the writer's in-memory buffer until we catch up.
        if throttle.as_ref().is_some_and(|t| t.should_pause()) {
            continue;
        }
        let elapsed = last_event.lock().elapsed();
        if elapsed < COMMIT_DEBOUNCE {
            continue;
        }
        dirty.store(false, Ordering::Release);
        if let Err(err) = index.commit() {
            warn!(%err, "watcher commit failed");
            dirty.store(true, Ordering::Release);
        }
    }

    // Final commit on shutdown — never lose a pending event, even if we
    // were paused when shutdown was requested.
    if dirty.load(Ordering::Acquire) {
        if let Err(err) = index.commit() {
            warn!(%err, "final watcher commit failed");
        }
    }
}
