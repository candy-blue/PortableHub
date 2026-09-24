import { defineStore } from "pinia";
import { ref, computed } from "vue";
import type { Category, Software, SoftwareScanCandidate, LaunchResult } from "@/types";
import { invoke } from "@tauri-apps/api/core";
import PinyinMatch from "pinyin-match";

export const useLibraryStore = defineStore("library", () => {
  const categories = ref<Category[]>([
    { id: 1, name: "开发工具", icon: "Code", color: "#3b82f6", sortOrder: 1, isSystem: false },
    { id: 2, name: "系统工具", icon: "Wrench", color: "#10b981", sortOrder: 2, isSystem: false },
    { id: 3, name: "办公效率", icon: "Briefcase", color: "#8b5cf6", sortOrder: 3, isSystem: false },
    { id: 4, name: "网络工具", icon: "Globe", color: "#06b6d4", sortOrder: 4, isSystem: false },
    { id: 5, name: "多媒体", icon: "Play", color: "#ec4899", sortOrder: 5, isSystem: false },
    { id: 6, name: "其他", icon: "Box", color: "#6b7280", sortOrder: 99, isSystem: true },
  ]);

  const softwareList = ref<Software[]>([
    {
      id: 1,
      name: "Visual Studio Code",
      exePath: "D:\\PortableApps\\VSCode\\Code.exe",
      description: "现代轻量级跨平台代码编辑器",
      categoryId: 1,
      categoryName: "开发工具",
      isFavorite: true,
      launchCount: 42,
      lastLaunchedAt: new Date(Date.now() - 3600000).toISOString(),
      sortOrder: 1,
      runAsAdmin: false,
      singleInstance: true,
      tags: "IDE, Editor, 开发, 微软",
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isRunning: true,
    },
    {
      id: 2,
      name: "Everything 极速搜索",
      exePath: "D:\\PortableApps\\Everything\\Everything.exe",
      description: "Windows 秒级本地文件搜索引擎",
      categoryId: 2,
      categoryName: "系统工具",
      isFavorite: true,
      launchCount: 128,
      lastLaunchedAt: new Date(Date.now() - 7200000).toISOString(),
      sortOrder: 2,
      runAsAdmin: false,
      singleInstance: true,
      tags: "Search, 搜索, 文件",
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
    {
      id: 3,
      name: "7-Zip 便携版",
      exePath: "D:\\PortableApps\\7-Zip\\7zFM.exe",
      description: "高压缩比压缩解压工具",
      categoryId: 2,
      categoryName: "系统工具",
      isFavorite: false,
      launchCount: 19,
      lastLaunchedAt: new Date(Date.now() - 86400000).toISOString(),
      sortOrder: 3,
      runAsAdmin: false,
      singleInstance: true,
      tags: "Zip, 压缩, 解压",
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
    {
      id: 4,
      name: "Postman API 调试",
      exePath: "D:\\PortableApps\\Postman\\Postman.exe",
      description: "专业 API 协同与测试平台",
      categoryId: 1,
      categoryName: "开发工具",
      isFavorite: false,
      launchCount: 15,
      lastLaunchedAt: new Date(Date.now() - 172800000).toISOString(),
      sortOrder: 4,
      runAsAdmin: false,
      singleInstance: true,
      tags: "API, HTTP, 网络, 调试",
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
    {
      id: 5,
      name: "GitKraken",
      exePath: "D:\\PortableApps\\GitKraken\\GitKraken.exe",
      description: "可视化 Git 版本管理客户端",
      categoryId: 1,
      categoryName: "开发工具",
      isFavorite: true,
      launchCount: 36,
      lastLaunchedAt: new Date(Date.now() - 5400000).toISOString(),
      sortOrder: 5,
      runAsAdmin: false,
      singleInstance: true,
      tags: "Git, 版本控制, 仓库",
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
    {
      id: 6,
      name: "Snipaste 截图利器",
      exePath: "D:\\PortableApps\\Snipaste\\Snipaste.exe",
      description: "强大贴图与屏幕截图工具",
      categoryId: 3,
      categoryName: "办公效率",
      isFavorite: true,
      launchCount: 99,
      lastLaunchedAt: new Date(Date.now() - 1800000).toISOString(),
      sortOrder: 6,
      runAsAdmin: true,
      singleInstance: true,
      tags: "截图, 贴图, 办公",
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isRunning: true,
    },
    {
      id: 7,
      name: "PotPlayer 影音播放器",
      exePath: "D:\\PortableApps\\PotPlayer\\PotPlayer64.exe",
      description: "全格式高清硬件加速播放器",
      categoryId: 5,
      categoryName: "多媒体",
      isFavorite: false,
      launchCount: 22,
      lastLaunchedAt: new Date(Date.now() - 36000000).toISOString(),
      sortOrder: 7,
      runAsAdmin: false,
      singleInstance: true,
      tags: "视频, 播放器, 媒体",
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
    {
      id: 8,
      name: "Notepad3 轻量记事本",
      exePath: "D:\\PortableApps\\Notepad3\\Notepad3.exe",
      description: "快速小巧的纯文本及代码查看器",
      categoryId: 3,
      categoryName: "办公效率",
      isFavorite: false,
      launchCount: 8,
      lastLaunchedAt: null,
      sortOrder: 8,
      runAsAdmin: false,
      singleInstance: true,
      tags: "文本, 记事本, Editor",
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isMissing: true,
    }
  ]);

  const selectedNav = ref<"all" | "recent" | "favorites" | "category">("all");
  const selectedCategoryId = ref<number | null>(null);
  const searchQuery = ref("");
  const viewMode = ref<"Grid" | "List">("Grid");
  const cardSize = ref<"Small" | "Medium" | "Large">("Medium");
  const sortBy = ref<"Custom" | "Name" | "LaunchCount" | "LastLaunchedAt">("Custom");
  const isCommandPaletteOpen = ref(false);
  const isAddEditModalOpen = ref(false);
  const editingSoftware = ref<Software | null>(null);
  const isScannerModalOpen = ref(false);
  const isCategoryModalOpen = ref(false);
  const isSettingsModalOpen = ref(false);
  const isNative = ref(false);

  // Synchronize with Rust SQLite backend
  async function loadFromBackend() {
    try {
      const cats = await invoke<Category[]>("get_categories");
      if (cats && cats.length > 0) {
        categories.value = cats;
        isNative.value = true;
      }
      const software = await invoke<Software[]>("get_all_software");
      if (Array.isArray(software) && isNative.value) {
        softwareList.value = software;
      }
    } catch {
      // Running in standalone web preview without Tauri backend
    }
  }

  // Category counts
  const categoryCounts = computed(() => {
    const map = new Map<number, number>();
    for (const item of softwareList.value) {
      map.set(item.categoryId, (map.get(item.categoryId) || 0) + 1);
    }
    return map;
  });

  const favoritesCount = computed(() => softwareList.value.filter(s => s.isFavorite).length);
  const recentCount = computed(() => softwareList.value.filter(s => !!s.lastLaunchedAt).length);

  // Filtered and scored software items
  const filteredSoftware = computed(() => {
    let items = [...softwareList.value];

    if (selectedNav.value === "favorites") {
      items = items.filter(s => s.isFavorite);
    } else if (selectedNav.value === "recent") {
      items = items.filter(s => !!s.lastLaunchedAt);
    } else if (selectedNav.value === "category" && selectedCategoryId.value) {
      items = items.filter(s => s.categoryId === selectedCategoryId.value);
    }

    const q = searchQuery.value.trim().toLowerCase();
    if (q) {
      items = items.filter(s => {
        const nameMatch = s.name.toLowerCase().includes(q) || (PinyinMatch.match(s.name, q) !== false);
        const tagMatch = (s.tags || "").toLowerCase().includes(q);
        const descMatch = (s.description || "").toLowerCase().includes(q);
        return nameMatch || tagMatch || descMatch;
      });
    }

    if (selectedNav.value === "recent" && sortBy.value === "Custom") {
      items.sort((a, b) => new Date(b.lastLaunchedAt || 0).getTime() - new Date(a.lastLaunchedAt || 0).getTime());
    } else if (sortBy.value === "Name") {
      items.sort((a, b) => a.name.localeCompare(b.name, "zh-CN"));
    } else if (sortBy.value === "LaunchCount") {
      items.sort((a, b) => b.launchCount - a.launchCount);
    } else if (sortBy.value === "LastLaunchedAt") {
      items.sort((a, b) => new Date(b.lastLaunchedAt || 0).getTime() - new Date(a.lastLaunchedAt || 0).getTime());
    } else {
      items.sort((a, b) => a.sortOrder - b.sortOrder);
    }

    return items;
  });

  // Actions
  async function toggleFavorite(id: number) {
    const item = softwareList.value.find(s => s.id === id);
    if (item) {
      item.isFavorite = !item.isFavorite;
      try {
        await invoke<boolean>("toggle_favorite", { id });
      } catch {
        // Fallback for local preview
      }
    }
  }

  async function launchSoftware(id: number, forceAdmin = false) {
    const item = softwareList.value.find(s => s.id === id);
    if (item) {
      item.launchCount++;
      item.lastLaunchedAt = new Date().toISOString();
      item.isRunning = true;
      try {
        const res = await invoke<LaunchResult>("launch_software", { software: item, forceAdmin });
        if (!res.success && res.message) {
          console.warn("启动提示:", res.message);
        }
      } catch (err) {
        console.warn("启动调用失败:", err);
      }
    }
  }

  async function deleteSoftware(id: number) {
    const index = softwareList.value.findIndex(s => s.id === id);
    if (index !== -1) {
      softwareList.value.splice(index, 1);
      try {
        await invoke("delete_software", { id });
      } catch {
        // Local preview fallback
      }
    }
  }

  async function scanDirectory(dirPath: string, defaultCategoryId: number): Promise<SoftwareScanCandidate[]> {
    try {
      return await invoke<SoftwareScanCandidate[]>("scan_directory", { dirPath, defaultCategoryId });
    } catch {
      return [];
    }
  }

  async function addSoftware(payload: Partial<Software>): Promise<Software | null> {
    const newSoftware: Software = {
      id: 0,
      name: payload.name || "未命名应用",
      exePath: payload.exePath || "",
      description: payload.description || "",
      arguments: payload.arguments || "",
      workingDirectory: payload.workingDirectory || "",
      iconPath: payload.iconPath || "",
      categoryId: payload.categoryId || (categories.value[0]?.id || 1),
      categoryName: categories.value.find(c => c.id === payload.categoryId)?.name || "默认",
      isFavorite: payload.isFavorite || false,
      launchCount: 0,
      lastLaunchedAt: null,
      sortOrder: softwareList.value.length + 1,
      runAsAdmin: payload.runAsAdmin || false,
      singleInstance: payload.singleInstance !== false,
      tags: payload.tags || "",
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isMissing: false,
      isRunning: false,
    };

    try {
      const created = await invoke<Software>("add_software", { software: newSoftware });
      softwareList.value.push(created);
      return created;
    } catch (e) {
      console.error("Failed to add software:", e);
      newSoftware.id = Date.now();
      softwareList.value.push(newSoftware);
      return newSoftware;
    }
  }

  async function updateSoftware(software: Software): Promise<boolean> {
    try {
      const updated = await invoke<Software>("update_software", { software });
      const idx = softwareList.value.findIndex(s => s.id === software.id);
      if (idx !== -1) {
        softwareList.value[idx] = updated;
      }
      return true;
    } catch (e) {
      console.error("Failed to update software:", e);
      const idx = softwareList.value.findIndex(s => s.id === software.id);
      if (idx !== -1) {
        softwareList.value[idx] = { ...software, updatedAt: new Date().toISOString() };
      }
      return true;
    }
  }

  async function addCategory(payload: Partial<Category>): Promise<Category | null> {
    const newCat: Category = {
      id: 0,
      name: payload.name || "新分类",
      icon: payload.icon || "Folder",
      color: payload.color || "#3b82f6",
      sortOrder: categories.value.length + 1,
      isSystem: false,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };

    try {
      const created = await invoke<Category>("add_category", { category: newCat });
      categories.value.push(created);
      return created;
    } catch (e) {
      console.error("Failed to add category:", e);
      newCat.id = Date.now();
      categories.value.push(newCat);
      return newCat;
    }
  }

  async function batchAddSoftware(candidates: SoftwareScanCandidate[]): Promise<number> {
    try {
      const count = await invoke<number>("batch_add_software", { candidates });
      await loadFromBackend();
      return count;
    } catch (e) {
      console.error("Failed to batch add software:", e);
      return 0;
    }
  }

  async function openFolder(path: string) {
    try {
      await invoke("open_folder", { path });
    } catch (err) {
      console.warn("Failed to open folder:", err);
    }
  }

  function openAddModal() {
    editingSoftware.value = null;
    isAddEditModalOpen.value = true;
  }

  function openEditModal(software: Software) {
    editingSoftware.value = { ...software };
    isAddEditModalOpen.value = true;
  }

  function selectNav(nav: "all" | "recent" | "favorites") {
    selectedNav.value = nav;
    selectedCategoryId.value = null;
  }

  function selectCategory(categoryId: number) {
    selectedNav.value = "category";
    selectedCategoryId.value = categoryId;
  }

  return {
    categories,
    softwareList,
    selectedNav,
    selectedCategoryId,
    searchQuery,
    viewMode,
    cardSize,
    sortBy,
    isCommandPaletteOpen,
    isAddEditModalOpen,
    editingSoftware,
    isScannerModalOpen,
    isCategoryModalOpen,
    isSettingsModalOpen,
    isNative,
    categoryCounts,
    favoritesCount,
    recentCount,
    filteredSoftware,
    loadFromBackend,
    toggleFavorite,
    launchSoftware,
    deleteSoftware,
    addSoftware,
    updateSoftware,
    addCategory,
    scanDirectory,
    batchAddSoftware,
    openFolder,
    openAddModal,
    openEditModal,
    selectNav,
    selectCategory,
  };
});
