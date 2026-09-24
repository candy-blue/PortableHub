use std::collections::HashSet;
use std::path::Path;
use walkdir::WalkDir;
use crate::models::SoftwareScanCandidate;

const EXCLUDED_EXE_NAMES: &[&str] = &[
    "uninstall", "unins000", "unins001", "setup", "installer", "install",
    "update", "updater", "crashpad_handler", "helper", "service", "driver",
    "elevate", "vc_redist", "dxwebsetup", "vcredist_x64", "vcredist_x86",
    "python", "pythonw", "node", "npm", "npx", "cmd", "powershell",
    "conhost", "bash", "ssh", "7za", "7zr", "regsvr32", "rundll32",
    "unitycrashhandler64", "unitycrashhandler32", "ffmpeg", "ffprobe",
];

const EXCLUDED_FOLDERS: &[&str] = &[
    "runtimes", "plugins", "resources", "node_modules", ".git",
    "obj", "packages", "locales", "swiftshader", "x86", "x64", "arm64",
];

pub fn is_excluded_exe(path: &Path) -> bool {
    let file_stem = match path.file_stem().and_then(|s| s.to_str()) {
        Some(s) => s.to_lowercase(),
        None => return true,
    };

    let excluded_set: HashSet<&str> = EXCLUDED_EXE_NAMES.iter().copied().collect();
    if excluded_set.contains(file_stem.as_str()) {
        return true;
    }

    if file_stem.starts_with("unins")
        || file_stem.starts_with("setup")
        || file_stem.starts_with("install")
        || file_stem.ends_with("_helper")
        || file_stem.contains("crashpad")
        || file_stem.contains("crash_handler")
    {
        return true;
    }

    for component in path.components() {
        if let Some(comp_str) = component.as_os_str().to_str() {
            let lower = comp_str.to_lowercase();
            if EXCLUDED_FOLDERS.contains(&lower.as_str()) {
                return true;
            }
        }
    }

    false
}

pub fn scan_directory(dir_path: &str, default_category_id: i64) -> Vec<SoftwareScanCandidate> {
    let mut candidates = Vec::new();
    let root = Path::new(dir_path);
    if !root.exists() || !root.is_dir() {
        return candidates;
    }

    for entry in WalkDir::new(root).into_iter().filter_map(|e| e.ok()) {
        let path = entry.path();
        if path.is_file() {
            if let Some(ext) = path.extension().and_then(|e| e.to_str()) {
                if ext.eq_ignore_ascii_case("exe") {
                    if !is_excluded_exe(path) {
                        let name = path.file_stem()
                            .and_then(|s| s.to_str())
                            .unwrap_or("Unknown")
                            .to_string();

                        candidates.push(SoftwareScanCandidate {
                            name,
                            exe_path: path.to_string_lossy().to_string(),
                            relative_path: path.strip_prefix(root).ok().map(|p| p.to_string_lossy().to_string()),
                            root_id: None,
                            icon_path: None,
                            suggested_category_id: default_category_id,
                            description: None,
                            selected: true,
                        });
                    }
                }
            }
        }
    }

    candidates
}
