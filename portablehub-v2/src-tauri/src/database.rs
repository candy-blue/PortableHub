use std::path::{Path, PathBuf};
use std::sync::Mutex;
use chrono::Utc;
use rusqlite::{params, Connection, Result};
use crate::models::{Category, Software};

pub struct DbState {
    pub conn: Mutex<Connection>,
}

impl DbState {
    pub fn new(db_path: PathBuf) -> Result<Self> {
        if let Some(parent) = db_path.parent() {
            let _ = std::fs::create_dir_all(parent);
        }

        let conn = Connection::open(&db_path)?;
        let state = Self {
            conn: Mutex::new(conn),
        };

        state.migrate()?;
        Ok(state)
    }

    pub fn migrate(&self) -> Result<()> {
        let conn = self.conn.lock().unwrap();

        // 1. Enable WAL mode and foreign keys
        conn.execute_batch("PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;")?;

        // 2. SchemaVersion Table
        conn.execute(
            "CREATE TABLE IF NOT EXISTS SchemaVersion (
                Version INTEGER PRIMARY KEY,
                AppliedAt TEXT NOT NULL
            );",
            [],
        )?;

        let current_version: i64 = conn.query_row(
            "SELECT COALESCE(MAX(Version), 0) FROM SchemaVersion;",
            [],
            |row| row.get(0),
        ).unwrap_or(0);

        if current_version < 1 {
            self.apply_v1(&conn)?;
        }
        if current_version < 2 {
            self.apply_v2(&conn)?;
        }
        if current_version < 3 {
            self.apply_v3(&conn)?;
        }

        Ok(())
    }

    fn apply_v1(&self, conn: &Connection) -> Result<()> {
        conn.execute(
            "CREATE TABLE IF NOT EXISTS RootDirectory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Path TEXT NOT NULL,
                IsAvailable INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL
            );",
            [],
        )?;

        conn.execute(
            "CREATE TABLE IF NOT EXISTS Category (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Icon TEXT,
                Color TEXT,
                SortOrder INTEGER NOT NULL DEFAULT 0,
                IsSystem INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );",
            [],
        )?;

        conn.execute(
            "CREATE TABLE IF NOT EXISTS Software (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RootId INTEGER,
                Name TEXT NOT NULL,
                ExePath TEXT NOT NULL,
                RelativePath TEXT,
                Description TEXT,
                Arguments TEXT,
                WorkingDirectory TEXT,
                IconPath TEXT,
                CategoryId INTEGER NOT NULL,
                IsFavorite INTEGER NOT NULL DEFAULT 0,
                LaunchCount INTEGER NOT NULL DEFAULT 0,
                LastLaunchedAt TEXT,
                SortOrder INTEGER NOT NULL DEFAULT 0,
                RunAsAdmin INTEGER NOT NULL DEFAULT 0,
                SingleInstance INTEGER NOT NULL DEFAULT 1,
                Tags TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (CategoryId) REFERENCES Category(Id) ON DELETE RESTRICT,
                FOREIGN KEY (RootId) REFERENCES RootDirectory(Id) ON DELETE SET NULL
            );",
            [],
        )?;

        conn.execute_batch(
            "CREATE INDEX IF NOT EXISTS idx_software_name ON Software(Name);
             CREATE INDEX IF NOT EXISTS idx_software_categoryid ON Software(CategoryId);
             CREATE INDEX IF NOT EXISTS idx_software_sortorder ON Software(SortOrder);
             CREATE INDEX IF NOT EXISTS idx_software_lastlaunchedat ON Software(LastLaunchedAt);
             CREATE INDEX IF NOT EXISTS idx_software_isfavorite ON Software(IsFavorite);"
        )?;

        let category_count: i64 = conn.query_row(
            "SELECT COUNT(*) FROM Category;",
            [],
            |r| r.get(0),
        ).unwrap_or(0);

        if category_count == 0 {
            let now = Utc::now().to_rfc3339();
            let defaults = [
                ("常用软件", "Star", "#3B82F6", 1, 0),
                ("开发工具", "Code", "#10B981", 2, 0),
                ("办公效率", "Briefcase", "#8B5CF6", 3, 0),
                ("系统工具", "Wrench", "#F59E0B", 4, 0),
                ("网络应用", "Globe", "#06B6D4", 5, 0),
                ("多媒体", "Play", "#EC4899", 6, 0),
                ("其他", "Box", "#6B7280", 99, 1),
            ];
            for (name, icon, color, sort_order, is_system) in defaults {
                conn.execute(
                    "INSERT INTO Category (Name, Icon, Color, SortOrder, IsSystem, CreatedAt, UpdatedAt)
                     VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7);",
                    params![name, icon, color, sort_order, is_system, now, now],
                )?;
            }
        }

        let now = Utc::now().to_rfc3339();
        conn.execute(
            "INSERT INTO SchemaVersion (Version, AppliedAt) VALUES (1, ?1);",
            params![now],
        )?;

        Ok(())
    }

    fn apply_v2(&self, conn: &Connection) -> Result<()> {
        let has_other: i64 = conn.query_row(
            "SELECT COUNT(*) FROM Category WHERE Name = '其他';",
            [],
            |r| r.get(0),
        ).unwrap_or(0);

        let now = Utc::now().to_rfc3339();
        if has_other == 0 {
            conn.execute(
                "INSERT INTO Category (Name, Icon, Color, SortOrder, IsSystem, CreatedAt, UpdatedAt)
                 VALUES ('其他', 'Apps24', '#6B7280', 1, 1, ?1, ?2);",
                params![now, now],
            )?;
        }

        conn.execute_batch(
            "DELETE FROM Category 
             WHERE IsSystem = 1 
               AND Name IN ('开发工具', '系统工具', '网络工具', '办公工具', '图形图像', '多媒体', '文件管理', '安全工具')
               AND Id NOT IN (SELECT DISTINCT CategoryId FROM Software WHERE CategoryId IS NOT NULL);"
        )?;

        conn.execute(
            "INSERT INTO SchemaVersion (Version, AppliedAt) VALUES (2, ?1);",
            params![now],
        )?;

        Ok(())
    }

    fn apply_v3(&self, conn: &Connection) -> Result<()> {
        let mut has_column = false;
        let mut stmt = conn.prepare("PRAGMA table_info(Software);")?;
        let col_names = stmt.query_map([], |row| row.get::<_, String>(1))?;
        for col in col_names.flatten() {
            if col.eq_ignore_ascii_case("LinkedSoftwareIds") {
                has_column = true;
                break;
            }
        }

        if !has_column {
            conn.execute("ALTER TABLE Software ADD COLUMN LinkedSoftwareIds TEXT;", [])?;
        }

        let now = Utc::now().to_rfc3339();
        conn.execute(
            "INSERT INTO SchemaVersion (Version, AppliedAt) VALUES (3, ?1);",
            params![now],
        )?;

        Ok(())
    }

    pub fn get_categories(&self) -> Result<Vec<Category>> {
        let conn = self.conn.lock().unwrap();
        let mut stmt = conn.prepare(
            "SELECT Id, Name, Icon, Color, SortOrder, IsSystem, CreatedAt, UpdatedAt
             FROM Category
             ORDER BY SortOrder ASC, Id ASC;"
        )?;

        let iter = stmt.query_map([], |row| {
            Ok(Category {
                id: row.get(0)?,
                name: row.get(1)?,
                icon: row.get(2)?,
                color: row.get(3)?,
                sort_order: row.get(4)?,
                is_system: row.get::<_, i64>(5)? != 0,
                created_at: row.get(6)?,
                updated_at: row.get(7)?,
            })
        })?;

        let mut list = Vec::new();
        for item in iter {
            list.push(item?);
        }
        Ok(list)
    }

    pub fn get_all_software(&self) -> Result<Vec<Software>> {
        let conn = self.conn.lock().unwrap();
        let mut stmt = conn.prepare(
            "SELECT s.Id, s.RootId, s.Name, s.ExePath, s.RelativePath, s.Description,
                    s.Arguments, s.WorkingDirectory, s.IconPath, s.CategoryId, s.IsFavorite,
                    s.LaunchCount, s.LastLaunchedAt, s.SortOrder, s.RunAsAdmin, s.SingleInstance,
                    s.Tags, s.LinkedSoftwareIds, s.CreatedAt, s.UpdatedAt,
                    COALESCE(c.Name, '') as CategoryName
             FROM Software s
             LEFT JOIN Category c ON s.CategoryId = c.Id
             ORDER BY s.SortOrder ASC, s.Id ASC;"
        )?;

        let iter = stmt.query_map([], |row| {
            let exe_path: String = row.get(3)?;
            let is_missing = !Path::new(&exe_path).exists();

            Ok(Software {
                id: row.get(0)?,
                root_id: row.get(1)?,
                name: row.get(2)?,
                exe_path,
                relative_path: row.get(4)?,
                description: row.get(5)?,
                arguments: row.get(6)?,
                working_directory: row.get(7)?,
                icon_path: row.get(8)?,
                category_id: row.get(9)?,
                is_favorite: row.get::<_, i64>(10)? != 0,
                launch_count: row.get(11)?,
                last_launched_at: row.get(12)?,
                sort_order: row.get(13)?,
                run_as_admin: row.get::<_, i64>(14)? != 0,
                single_instance: row.get::<_, i64>(15)? != 0,
                tags: row.get(16)?,
                linked_software_ids: row.get(17)?,
                created_at: row.get(18)?,
                updated_at: row.get(19)?,
                is_missing,
                is_running: false,
                category_name: row.get(20)?,
            })
        })?;

        let mut list = Vec::new();
        for item in iter {
            list.push(item?);
        }
        Ok(list)
    }

    pub fn toggle_favorite(&self, id: i64) -> Result<bool> {
        let conn = self.conn.lock().unwrap();
        let current: i64 = conn.query_row(
            "SELECT IsFavorite FROM Software WHERE Id = ?1;",
            params![id],
            |r| r.get(0),
        )?;

        let next = if current == 0 { 1 } else { 0 };
        let now = Utc::now().to_rfc3339();
        conn.execute(
            "UPDATE Software SET IsFavorite = ?1, UpdatedAt = ?2 WHERE Id = ?3;",
            params![next, now, id],
        )?;

        Ok(next == 1)
    }

    pub fn record_launch(&self, id: i64) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        let now = Utc::now().to_rfc3339();
        conn.execute(
            "UPDATE Software 
             SET LaunchCount = LaunchCount + 1, LastLaunchedAt = ?1, UpdatedAt = ?1 
             WHERE Id = ?2;",
            params![now, id],
        )?;
        Ok(())
    }

    pub fn delete_software(&self, id: i64) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute("DELETE FROM Software WHERE Id = ?1;", params![id])?;
        Ok(())
    }

    pub fn add_software(&self, mut software: Software) -> Result<Software> {
        let conn = self.conn.lock().unwrap();
        let now = Utc::now().to_rfc3339();
        software.created_at = now.clone();
        software.updated_at = now.clone();

        conn.execute(
            "INSERT INTO Software (
                RootId, Name, ExePath, RelativePath, Description, Arguments,
                WorkingDirectory, IconPath, CategoryId, IsFavorite, LaunchCount,
                LastLaunchedAt, SortOrder, RunAsAdmin, SingleInstance, Tags,
                LinkedSoftwareIds, CreatedAt, UpdatedAt
            ) VALUES (
                ?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12, ?13, ?14, ?15, ?16, ?17, ?18, ?19
            );",
            params![
                software.root_id,
                software.name,
                software.exe_path,
                software.relative_path,
                software.description,
                software.arguments,
                software.working_directory,
                software.icon_path,
                software.category_id,
                if software.is_favorite { 1 } else { 0 },
                software.launch_count,
                software.last_launched_at,
                software.sort_order,
                if software.run_as_admin { 1 } else { 0 },
                if software.single_instance { 1 } else { 0 },
                software.tags,
                software.linked_software_ids,
                software.created_at,
                software.updated_at,
            ],
        )?;

        let id = conn.last_insert_rowid();
        software.id = id;
        software.is_missing = !Path::new(&software.exe_path).exists();
        Ok(software)
    }

    pub fn update_software(&self, mut software: Software) -> Result<Software> {
        let conn = self.conn.lock().unwrap();
        let now = Utc::now().to_rfc3339();
        software.updated_at = now.clone();

        conn.execute(
            "UPDATE Software SET
                Name = ?1, ExePath = ?2, Description = ?3, Arguments = ?4,
                WorkingDirectory = ?5, IconPath = ?6, CategoryId = ?7,
                IsFavorite = ?8, RunAsAdmin = ?9, SingleInstance = ?10,
                Tags = ?11, UpdatedAt = ?12
             WHERE Id = ?13;",
            params![
                software.name,
                software.exe_path,
                software.description,
                software.arguments,
                software.working_directory,
                software.icon_path,
                software.category_id,
                if software.is_favorite { 1 } else { 0 },
                if software.run_as_admin { 1 } else { 0 },
                if software.single_instance { 1 } else { 0 },
                software.tags,
                software.updated_at,
                software.id,
            ],
        )?;

        software.is_missing = !Path::new(&software.exe_path).exists();
        Ok(software)
    }

    pub fn add_category(&self, mut category: Category) -> Result<Category> {
        let conn = self.conn.lock().unwrap();
        let now = Utc::now().to_rfc3339();
        category.created_at = now.clone();
        category.updated_at = now.clone();

        conn.execute(
            "INSERT INTO Category (Name, Icon, Color, SortOrder, IsSystem, CreatedAt, UpdatedAt)
             VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7);",
            params![
                category.name,
                category.icon,
                category.color,
                category.sort_order,
                if category.is_system { 1 } else { 0 },
                category.created_at,
                category.updated_at,
            ],
        )?;

        let id = conn.last_insert_rowid();
        category.id = id;
        Ok(category)
    }

    pub fn delete_category(&self, id: i64) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        let is_system: i64 = conn.query_row(
            "SELECT IsSystem FROM Category WHERE Id = ?1;",
            params![id],
            |r| r.get(0),
        ).unwrap_or(0);

        if is_system != 0 {
            return Err(rusqlite::Error::QueryReturnedNoRows);
        }

        let fallback_cat_id: i64 = conn.query_row(
            "SELECT Id FROM Category WHERE Id != ?1 ORDER BY SortOrder ASC, Id ASC LIMIT 1;",
            params![id],
            |r| r.get(0),
        ).unwrap_or(1);

        conn.execute(
            "UPDATE Software SET CategoryId = ?1 WHERE CategoryId = ?2;",
            params![fallback_cat_id, id],
        )?;

        conn.execute("DELETE FROM Category WHERE Id = ?1;", params![id])?;
        Ok(())
    }

    pub fn batch_add_software(&self, candidates: Vec<crate::models::SoftwareScanCandidate>) -> Result<usize> {
        let conn = self.conn.lock().unwrap();
        let now = Utc::now().to_rfc3339();
        let mut count = 0;

        for c in candidates {
            let exists: i64 = conn.query_row(
                "SELECT COUNT(*) FROM Software WHERE ExePath = ?1;",
                params![c.exe_path],
                |r| r.get(0),
            ).unwrap_or(0);

            if exists == 0 {
                conn.execute(
                    "INSERT INTO Software (
                        Name, ExePath, CategoryId, IsFavorite, LaunchCount,
                        RunAsAdmin, SingleInstance, CreatedAt, UpdatedAt
                    ) VALUES (?1, ?2, ?3, 0, 0, 0, 1, ?4, ?4);",
                    params![c.name, c.exe_path, c.suggested_category_id, now],
                )?;
                count += 1;
            }
        }

        Ok(count)
    }
}
