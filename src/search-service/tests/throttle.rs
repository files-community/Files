use std::time::Duration;

use files_search_service::Throttle;

/// `apply_background_priority` is best-tested by observing the running
/// process's priority class with an external tool (Process Explorer);
/// here we just confirm that startup + drop don't panic and that
/// `should_pause()` produces a well-defined boolean. The behavior tests
/// (verifying we actually pause on battery / fullscreen / load) live in
/// `tests/Files.Search.Resource/` per CLAUDE.md.
#[test]
fn throttle_starts_and_stops_cleanly() {
    let t = Throttle::start();
    let _ = t.should_pause();
    // Give the poller at least one tick to populate state.
    std::thread::sleep(Duration::from_millis(100));
    let _ = t.should_pause();
    drop(t);
}
