use std::path::{Path, PathBuf};
use sha2::{Sha256, Digest};

pub fn get_icon_cache_path(cache_dir: &Path, exe_path: &str) -> PathBuf {
    let mut hasher = Sha256::new();
    hasher.update(exe_path.as_bytes());
    let result = hasher.finalize();
    let hash_hex = format!("{:x}", result);
    cache_dir.join(format!("{}.png", hash_hex))
}

pub fn get_cached_icon(cache_dir: &Path, exe_path: &str) -> Option<String> {
    let target = get_icon_cache_path(cache_dir, exe_path);
    if target.exists() {
        Some(target.to_string_lossy().to_string())
    } else {
        None
    }
}
