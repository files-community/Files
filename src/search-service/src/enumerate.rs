//! Parallel filesystem enumeration.
//!
//! On Windows: `FindFirstFileExW` with `FindExInfoBasic` (skips the 8.3
//! short-name lookup that Win32 normally does) and `FIND_FIRST_EX_LARGE_FETCH`
//! (larger internal buffer per syscall). Subdirectory recursion is fanned
//! out via `rayon::scope` so multiple threads walk in parallel. Callers
//! receive entries through an `mpsc::Sender` so producer and consumer (the
//! Tantivy writer) can run concurrently.
//!
//! On non-Windows: falls back to `walkdir` so the crate still builds and
//! tests on Linux/macOS for development convenience. The Files product is
//! Windows-only and the bench gates are measured on Windows.

use std::path::{Path, PathBuf};
use std::sync::mpsc::Sender;

#[derive(Debug)]
pub struct Entry {
    pub path: PathBuf,
    pub size_bytes: u64,
    pub modified_unix_ms: i64,
}

pub fn enumerate(root: &Path, send: Sender<Entry>) {
    #[cfg(windows)]
    {
        rayon::scope(|s| {
            win::recurse(s, root.to_path_buf(), send);
        });
    }
    #[cfg(not(windows))]
    {
        fallback::walk(root, send);
    }
}

#[cfg(windows)]
mod win {
    use super::Entry;
    use std::os::windows::ffi::{OsStrExt, OsStringExt};
    use std::path::PathBuf;
    use std::sync::mpsc::Sender;

    use windows::core::PCWSTR;
    use windows::Win32::Foundation::HANDLE;
    use windows::Win32::Storage::FileSystem::{
        FindClose, FindExInfoBasic, FindExSearchNameMatch, FindFirstFileExW, FindNextFileW,
        FILE_ATTRIBUTE_DIRECTORY, FILE_ATTRIBUTE_REPARSE_POINT, FIND_FIRST_EX_LARGE_FETCH,
        WIN32_FIND_DATAW,
    };

    pub(super) fn recurse<'a>(
        scope: &rayon::Scope<'a>,
        dir: PathBuf,
        send: Sender<Entry>,
    ) {
        let pattern = wide_path(&dir.join("*"));

        let mut data: WIN32_FIND_DATAW = unsafe { std::mem::zeroed() };
        let handle = unsafe {
            FindFirstFileExW(
                PCWSTR(pattern.as_ptr()),
                FindExInfoBasic,
                &mut data as *mut _ as *mut _,
                FindExSearchNameMatch,
                None,
                FIND_FIRST_EX_LARGE_FETCH,
            )
        };

        let handle: HANDLE = match handle {
            Ok(h) if !h.is_invalid() => h,
            _ => return,
        };

        loop {
            handle_entry(scope, &dir, &data, &send);
            let next = unsafe { FindNextFileW(handle, &mut data) };
            if next.is_err() {
                break;
            }
        }

        let _ = unsafe { FindClose(handle) };
    }

    fn handle_entry<'a>(
        scope: &rayon::Scope<'a>,
        dir: &PathBuf,
        data: &WIN32_FIND_DATAW,
        send: &Sender<Entry>,
    ) {
        let name = wide_to_osstring(&data.cFileName);
        let bytes = name.as_encoded_bytes();
        if bytes == b"." || bytes == b".." {
            return;
        }
        let path = dir.join(&name);
        let attrs = data.dwFileAttributes;
        let is_dir = (attrs & FILE_ATTRIBUTE_DIRECTORY.0) != 0;
        let is_reparse = (attrs & FILE_ATTRIBUTE_REPARSE_POINT.0) != 0;

        // Skip reparse points (junctions, symlinks) to match the previous
        // `WalkDir::follow_links(false)` behavior. Without this, a symlink
        // loop can spin the enumerator forever.
        if is_reparse {
            return;
        }

        if is_dir {
            let send2 = send.clone();
            scope.spawn(move |s| recurse(s, path, send2));
            return;
        }

        let size_bytes = ((data.nFileSizeHigh as u64) << 32) | (data.nFileSizeLow as u64);
        let modified_unix_ms = filetime_to_unix_ms(
            data.ftLastWriteTime.dwHighDateTime,
            data.ftLastWriteTime.dwLowDateTime,
        );
        let _ = send.send(Entry {
            path,
            size_bytes,
            modified_unix_ms,
        });
    }

    fn wide_path(p: &std::path::Path) -> Vec<u16> {
        let mut v: Vec<u16> = p.as_os_str().encode_wide().collect();
        v.push(0);
        v
    }

    fn wide_to_osstring(buf: &[u16]) -> std::ffi::OsString {
        let len = buf.iter().position(|&c| c == 0).unwrap_or(buf.len());
        std::ffi::OsString::from_wide(&buf[..len])
    }

    /// FILETIME counts 100-nanosecond intervals since 1601-01-01 UTC.
    /// Unix epoch is 11644473600 seconds later; convert to milliseconds.
    fn filetime_to_unix_ms(high: u32, low: u32) -> i64 {
        const EPOCH_DIFFERENCE_MS: i64 = 11_644_473_600_000;
        let ticks = ((high as u64) << 32) | (low as u64);
        let ms = (ticks / 10_000) as i64;
        ms - EPOCH_DIFFERENCE_MS
    }
}

#[cfg(not(windows))]
mod fallback {
    use super::Entry;
    use std::path::Path;
    use std::sync::mpsc::Sender;
    use std::time::UNIX_EPOCH;

    pub(super) fn walk(root: &Path, send: Sender<Entry>) {
        for entry in walkdir::WalkDir::new(root).follow_links(false) {
            let Ok(entry) = entry else { continue };
            if !entry.file_type().is_file() {
                continue;
            }
            let (size_bytes, modified_unix_ms) = match entry.metadata() {
                Ok(m) => {
                    let size = m.len();
                    let modified = m
                        .modified()
                        .ok()
                        .and_then(|t| t.duration_since(UNIX_EPOCH).ok())
                        .map(|d| d.as_millis() as i64)
                        .unwrap_or(0);
                    (size, modified)
                }
                Err(_) => (0, 0),
            };
            let _ = send.send(Entry {
                path: entry.into_path(),
                size_bytes,
                modified_unix_ms,
            });
        }
    }
}
