//! Process throttling.
//!
//! Two complementary mechanisms keep the service from being a bad citizen:
//!
//! 1. `apply_background_priority()` — once at startup, calls
//!    `SetPriorityClass(PROCESS_MODE_BACKGROUND_BEGIN)`. The Windows
//!    scheduler then puts CPU and I/O behind any normal-priority process.
//!    Same trick OneDrive uses for sync.
//!
//! 2. `Throttle` — a background poller (every 2s) that watches three
//!    signals and exposes `should_pause()`:
//!      - **On battery**: `GetSystemPowerStatus` `ACLineStatus == 0`.
//!      - **Fullscreen / presentation**: `SHQueryUserNotificationState`
//!        returns `QUNS_RUNNING_D3D_FULL_SCREEN` or `QUNS_PRESENTATION_MODE`.
//!      - **High CPU load**: derived from `GetSystemTimes` over the
//!        polling interval; threshold 70%.
//!
//! The watcher consults `should_pause()` before committing. Apply work
//! (in-memory adds/deletes) keeps happening so we don't drop events;
//! only commit + reader refresh is skipped, deferring fsync and
//! query-visibility until the system is idle again.

use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;
use std::thread;
use std::time::Duration;

use tracing::{info, warn};

const POLL_INTERVAL: Duration = Duration::from_secs(2);
const HIGH_LOAD_THRESHOLD: f64 = 0.70;

#[cfg(windows)]
pub fn apply_background_priority() {
    use windows::Win32::System::Threading::{
        GetCurrentProcess, SetPriorityClass, PROCESS_MODE_BACKGROUND_BEGIN,
    };
    unsafe {
        match SetPriorityClass(GetCurrentProcess(), PROCESS_MODE_BACKGROUND_BEGIN) {
            Ok(_) => info!("background priority enabled"),
            Err(err) => {
                warn!(%err, "SetPriorityClass(PROCESS_MODE_BACKGROUND_BEGIN) failed")
            }
        }
    }
}

#[cfg(not(windows))]
pub fn apply_background_priority() {}

pub struct Throttle {
    paused: Arc<AtomicBool>,
    stop: Arc<AtomicBool>,
    poller: Option<thread::JoinHandle<()>>,
}

impl Throttle {
    pub fn start() -> Self {
        let paused = Arc::new(AtomicBool::new(false));
        let stop = Arc::new(AtomicBool::new(false));
        let p = Arc::clone(&paused);
        let s = Arc::clone(&stop);
        let poller = thread::spawn(move || poll_loop(p, s));
        Self {
            paused,
            stop,
            poller: Some(poller),
        }
    }

    pub fn should_pause(&self) -> bool {
        self.paused.load(Ordering::Acquire)
    }
}

impl Drop for Throttle {
    fn drop(&mut self) {
        self.stop.store(true, Ordering::Release);
        if let Some(h) = self.poller.take() {
            let _ = h.join();
        }
    }
}

fn poll_loop(paused: Arc<AtomicBool>, stop: Arc<AtomicBool>) {
    let mut last_cpu = sample_cpu();
    while !stop.load(Ordering::Acquire) {
        thread::sleep(POLL_INTERVAL);
        let now_cpu = sample_cpu();
        let load = compute_load(&last_cpu, &now_cpu);
        last_cpu = now_cpu;

        let on_battery = is_on_battery();
        let fullscreen = is_fullscreen();
        let high_load = load > HIGH_LOAD_THRESHOLD;
        let new = on_battery || fullscreen || high_load;
        let prev = paused.swap(new, Ordering::AcqRel);
        if prev != new {
            info!(
                paused = new,
                on_battery,
                fullscreen,
                high_load,
                load,
                "throttle state changed"
            );
        }
    }
}

#[cfg(windows)]
fn is_on_battery() -> bool {
    use windows::Win32::System::Power::{GetSystemPowerStatus, SYSTEM_POWER_STATUS};
    let mut status = SYSTEM_POWER_STATUS::default();
    unsafe {
        if GetSystemPowerStatus(&mut status).is_err() {
            return false;
        }
    }
    // 0 = offline (battery), 1 = online, 255 = unknown. Conservative:
    // only flag battery on a definite "offline".
    status.ACLineStatus == 0
}

#[cfg(not(windows))]
fn is_on_battery() -> bool {
    false
}

#[cfg(windows)]
fn is_fullscreen() -> bool {
    use windows::Win32::UI::Shell::{
        SHQueryUserNotificationState, QUNS_BUSY, QUNS_PRESENTATION_MODE,
        QUNS_RUNNING_D3D_FULL_SCREEN,
    };
    let _ = QUNS_BUSY; // silence unused; matches! below references the others.
    let state = match unsafe { SHQueryUserNotificationState() } {
        Ok(s) => s,
        Err(_) => return false,
    };
    matches!(
        state,
        QUNS_RUNNING_D3D_FULL_SCREEN | QUNS_PRESENTATION_MODE
    )
}

#[cfg(not(windows))]
fn is_fullscreen() -> bool {
    false
}

struct CpuSample {
    #[cfg_attr(not(windows), allow(dead_code))]
    idle: u64,
    #[cfg_attr(not(windows), allow(dead_code))]
    kernel: u64,
    #[cfg_attr(not(windows), allow(dead_code))]
    user: u64,
}

#[cfg(windows)]
fn sample_cpu() -> CpuSample {
    use windows::Win32::Foundation::FILETIME;
    use windows::Win32::System::Threading::GetSystemTimes;
    let mut idle = FILETIME::default();
    let mut kernel = FILETIME::default();
    let mut user = FILETIME::default();
    unsafe {
        let _ = GetSystemTimes(Some(&mut idle), Some(&mut kernel), Some(&mut user));
    }
    CpuSample {
        idle: ft_to_u64(&idle),
        kernel: ft_to_u64(&kernel),
        user: ft_to_u64(&user),
    }
}

#[cfg(not(windows))]
fn sample_cpu() -> CpuSample {
    CpuSample { idle: 0, kernel: 0, user: 0 }
}

#[cfg(windows)]
fn ft_to_u64(ft: &windows::Win32::Foundation::FILETIME) -> u64 {
    ((ft.dwHighDateTime as u64) << 32) | (ft.dwLowDateTime as u64)
}

fn compute_load(prev: &CpuSample, now: &CpuSample) -> f64 {
    // GetSystemTimes documents that lpKernelTime *includes* idle time.
    // total = kernel + user, busy = total - idle.
    let idle_d = now.idle.saturating_sub(prev.idle) as f64;
    let kernel_d = now.kernel.saturating_sub(prev.kernel) as f64;
    let user_d = now.user.saturating_sub(prev.user) as f64;
    let total = kernel_d + user_d;
    if total <= 0.0 {
        return 0.0;
    }
    ((total - idle_d) / total).clamp(0.0, 1.0)
}
