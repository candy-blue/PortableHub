use std::path::Path;
use std::process::Command;
use crate::models::{LaunchResult, Software};

#[cfg(windows)]
use windows_sys::Win32::UI::Shell::ShellExecuteW;
#[cfg(windows)]
use windows_sys::Win32::UI::WindowsAndMessaging::SW_SHOWNORMAL;

pub fn launch_software(software: &Software, force_admin: bool) -> LaunchResult {
    let exe_path = Path::new(&software.exe_path);
    if !exe_path.exists() {
        return LaunchResult {
            success: false,
            message: Some(format!("文件不存在: {}", software.exe_path)),
            process_id: None,
        };
    }

    let working_dir = if let Some(ref dir) = software.working_directory {
        if !dir.is_empty() && Path::new(dir).exists() {
            Path::new(dir).to_path_buf()
        } else {
            exe_path.parent().unwrap_or(Path::new(".")).to_path_buf()
        }
    } else {
        exe_path.parent().unwrap_or(Path::new(".")).to_path_buf()
    };

    let run_as_admin = force_admin || software.run_as_admin;

    #[cfg(windows)]
    if run_as_admin {
        return launch_with_shell_execute(&software.exe_path, software.arguments.as_deref(), &working_dir, true);
    }

    // Normal launch
    let mut cmd = Command::new(&software.exe_path);
    cmd.current_dir(&working_dir);

    if let Some(ref args) = software.arguments {
        if !args.is_empty() {
            for arg in args.split_whitespace() {
                cmd.arg(arg);
            }
        }
    }

    match cmd.spawn() {
        Ok(child) => LaunchResult {
            success: true,
            message: None,
            process_id: Some(child.id()),
        },
        Err(_e) => {
            // Fallback to ShellExecuteW if standard spawn fails (e.g. .lnk or specialized format)
            #[cfg(windows)]
            {
                launch_with_shell_execute(&software.exe_path, software.arguments.as_deref(), &working_dir, false)
            }
            #[cfg(not(windows))]
            {
                LaunchResult {
                    success: false,
                    message: Some(format!("启动失败: {}", e)),
                    process_id: None,
                }
            }
        }
    }
}

#[cfg(windows)]
fn launch_with_shell_execute(
    file_path: &str,
    args: Option<&str>,
    working_dir: &Path,
    as_admin: bool,
) -> LaunchResult {
    use std::os::windows::ffi::OsStrExt;
    use std::ffi::OsStr;

    fn to_wide(s: &str) -> Vec<u16> {
        OsStr::new(s).encode_wide().chain(std::iter::once(0)).collect()
    }

    let verb = if as_admin { "runas" } else { "open" };
    let wide_verb = to_wide(verb);
    let wide_file = to_wide(file_path);
    let wide_args = args.map(to_wide);
    let wide_dir = to_wide(working_dir.to_str().unwrap_or(""));

    let args_ptr = wide_args.as_ref().map(|v| v.as_ptr()).unwrap_or(std::ptr::null());

    unsafe {
        let instance = ShellExecuteW(
            std::ptr::null_mut(),
            wide_verb.as_ptr(),
            wide_file.as_ptr(),
            args_ptr,
            wide_dir.as_ptr(),
            SW_SHOWNORMAL,
        );

        // Values <= 32 indicate error in ShellExecute
        let code = instance as usize;
        if code > 32 {
            LaunchResult {
                success: true,
                message: None,
                process_id: None,
            }
        } else {
            LaunchResult {
                success: false,
                message: Some(format!("ShellExecuteW 启动返回错误码: {}", code)),
                process_id: None,
            }
        }
    }
}
