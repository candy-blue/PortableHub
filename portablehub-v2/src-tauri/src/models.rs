use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Category {
    pub id: i64,
    pub name: string_or_empty::StringOrEmpty,
    pub icon: Option<String>,
    pub color: Option<String>,
    pub sort_order: i64,
    pub is_system: bool,
    pub created_at: String,
    pub updated_at: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Software {
    pub id: i64,
    pub root_id: Option<i64>,
    pub name: String,
    pub exe_path: String,
    pub relative_path: Option<String>,
    pub description: Option<String>,
    pub arguments: Option<String>,
    pub working_directory: Option<String>,
    pub icon_path: Option<String>,
    pub category_id: i64,
    pub is_favorite: bool,
    pub launch_count: i64,
    pub last_launched_at: Option<String>,
    pub sort_order: i64,
    pub run_as_admin: bool,
    pub single_instance: bool,
    pub tags: Option<String>,
    pub linked_software_ids: Option<String>,
    pub created_at: String,
    pub updated_at: String,

    // Runtime state fields
    #[serde(default)]
    pub is_missing: bool,
    #[serde(default)]
    pub is_running: bool,
    #[serde(default)]
    pub category_name: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SoftwareScanCandidate {
    pub name: String,
    pub exe_path: String,
    pub relative_path: Option<String>,
    pub root_id: Option<i64>,
    pub icon_path: Option<String>,
    pub suggested_category_id: i64,
    pub description: Option<String>,
    pub selected: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct LaunchResult {
    pub success: bool,
    pub message: Option<String>,
    pub process_id: Option<u32>,
}

mod string_or_empty {
    use serde::{self, Deserialize, Deserializer, Serializer};

    pub type StringOrEmpty = String;

    pub fn _serialize<S>(val: &str, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: Serializer,
    {
        serializer.serialize_str(val)
    }

    pub fn _deserialize<'de, D>(deserializer: D) -> Result<String, D::Error>
    where
        D: Deserializer<'de>,
    {
        Option::<String>::deserialize(deserializer).map(|opt| opt.unwrap_or_default())
    }
}
