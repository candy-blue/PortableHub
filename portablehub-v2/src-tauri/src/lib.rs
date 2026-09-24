pub mod models;
pub mod database;
pub mod launcher;
pub mod scanner;
pub mod icon;

use std::path::PathBuf;
use tauri::{Manager, State};
use database::DbState;
use models::{Category, LaunchResult, Software, SoftwareScanCandidate};

#[tauri::command]
fn get_categories(db: State<'_, DbState>) -> Result<Vec<Category>, String> {
    db.get_categories().map_err(|e| e.to_string())
}

#[tauri::command]
fn get_all_software(db: State<'_, DbState>) -> Result<Vec<Software>, String> {
    db.get_all_software().map_err(|e| e.to_string())
}

#[tauri::command]
fn toggle_favorite(db: State<'_, DbState>, id: i64) -> Result<bool, String> {
    db.toggle_favorite(id).map_err(|e| e.to_string())
}

#[tauri::command]
fn delete_software(db: State<'_, DbState>, id: i64) -> Result<(), String> {
    db.delete_software(id).map_err(|e| e.to_string())
}

#[tauri::command]
fn launch_software(db: State<'_, DbState>, software: Software, force_admin: bool) -> Result<LaunchResult, String> {
    let result = launcher::launch_software(&software, force_admin);
    if result.success {
        let _ = db.record_launch(software.id);
    }
    Ok(result)
}

#[tauri::command]
fn scan_directory(dir_path: String, default_category_id: i64) -> Vec<SoftwareScanCandidate> {
    scanner::scan_directory(&dir_path, default_category_id)
}

fn resolve_db_path(app: &tauri::AppHandle) -> PathBuf {
    // 1. Check local portable directory next to executable
    if let Ok(exe_dir) = std::env::current_exe().map(|p| p.parent().unwrap_or(&p).to_path_buf()) {
        let portable_db = exe_dir.join("data").join("PortableHub.db");
        if portable_db.exists() {
            return portable_db;
        }
    }

    // 2. Default to AppData
    app.path()
        .app_data_dir()
        .unwrap_or_else(|_| PathBuf::from("."))
        .join("PortableHub.db")
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .setup(|app| {
            let db_path = resolve_db_path(app.handle());
            let db_state = DbState::new(db_path)
                .expect("Failed to initialize SQLite database");
            app.manage(db_state);
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            get_categories,
            get_all_software,
            toggle_favorite,
            delete_software,
            launch_software,
            scan_directory,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
