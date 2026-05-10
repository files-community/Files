fn main() -> Result<(), Box<dyn std::error::Error>> {
    // Vendored protoc — keeps contributors from needing a system install.
    // SAFETY: build scripts run single-threaded before any other code.
    unsafe { std::env::set_var("PROTOC", protoc_bin_vendored::protoc_bin_path()?) };
    // Client is built so integration tests (and any in-process Rust
    // consumer) can talk to the service. The C# client uses its own
    // generated stubs, not these.
    tonic_build::configure()
        .compile_protos(&["proto/files_search.proto"], &["proto"])?;
    Ok(())
}
