// PortableHub 2.0 TypeScript Definitions (matching PortableHub.Core Models)

export interface Category {
  id: number;
  name: string;
  icon?: string | null;
  color?: string | null;
  sortOrder: number;
  isSystem: boolean;
  createdAt?: string;
  updatedAt?: string;
  softwareCount?: number;
}

export interface Software {
  id: number;
  rootId?: number | null;
  name: string;
  exePath: string;
  relativePath?: string | null;
  description?: string | null;
  arguments?: string | null;
  workingDirectory?: string | null;
  iconPath?: string | null;
  categoryId: number;
  isFavorite: boolean;
  launchCount: number;
  lastLaunchedAt?: string | null;
  sortOrder: number;
  runAsAdmin: boolean;
  singleInstance: boolean;
  tags?: string | null;
  linkedSoftwareIds?: string | null;
  createdAt: string;
  updatedAt: string;

  // Runtime computed fields
  isMissing?: boolean;
  isRunning?: boolean;
  categoryName?: string;
}

export interface RootDirectory {
  id: number;
  name: string;
  path: string;
  isAvailable: boolean;
  createdAt?: string;
}

export interface AppSettings {
  startWithWindows: boolean;
  startMinimizedToTray: boolean;
  minimizeToTray: boolean;
  closeToTray: boolean;
  globalHotkey: string;
  theme: "System" | "Light" | "Dark";
  cardSize: "Small" | "Medium" | "Large";
  language: string;
  backupFrequency: "Off" | "Daily" | "Weekly" | "Monthly";
  maxBackupCount: number;
  lastBackupAt?: string | null;
  windowWidth: number;
  windowHeight: number;
  windowLeft?: number | null;
  windowTop?: number | null;
  isMaximized: boolean;
  sortBy: "Custom" | "Name" | "LaunchCount" | "LastLaunchedAt";
  viewMode: "Grid" | "List";
  launchClickMode: "DoubleClick" | "SingleClick";
  autoRelocateMissing: boolean;
  hotkeyOpenStyle: "Grid" | "Search";
}

export interface SoftwareScanCandidate {
  name: string;
  exePath: string;
  relativePath?: string | null;
  rootId?: number | null;
  iconPath?: string | null;
  suggestedCategoryId: number;
  description?: string | null;
  selected: boolean;
}

export interface LaunchResult {
  success: boolean;
  message?: string | null;
  processId?: number | null;
}
