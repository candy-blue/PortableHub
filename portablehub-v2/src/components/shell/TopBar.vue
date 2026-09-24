<template>
  <header
    data-tauri-drag-region
    class="h-12 w-full flex items-center justify-between px-3 select-none border-b border-[var(--border-subtle)] bg-[var(--bg-surface)] z-30 transition-colors"
  >
    <!-- Left: App Brand & Breadcrumb -->
    <div class="flex items-center gap-3" data-tauri-drag-region>
      <div class="flex items-center gap-2" data-tauri-drag-region>
        <div class="w-6 h-6 rounded-md bg-[var(--accent-primary)] flex items-center justify-center text-white shadow-sm font-bold text-xs tracking-tighter">
          PH
        </div>
        <span class="font-semibold text-sm tracking-tight text-[var(--text-primary)]">PortableHub</span>
        <span class="text-xs px-1.5 py-0.5 rounded font-mono bg-[var(--accent-primary-subtle)] text-[var(--accent-primary)]">2.0</span>
      </div>

      <!-- Current Location Tag -->
      <div class="hidden sm:flex items-center text-xs text-[var(--text-muted)] gap-1 pl-2 border-l border-[var(--border-subtle)]" data-tauri-drag-region>
        <span>/</span>
        <span class="text-[var(--text-secondary)] font-medium">{{ currentViewTitle }}</span>
      </div>
    </div>

    <!-- Center: Search Bar Trigger (Quick Launcher preview) -->
    <div
      @click="libraryStore.isCommandPaletteOpen = true"
      class="flex-1 max-w-sm mx-4 h-8 px-3 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-card)] hover:bg-[var(--bg-card-hover)] hover:border-[var(--border-strong)] flex items-center justify-between text-xs text-[var(--text-muted)] cursor-pointer transition-all shadow-xs"
    >
      <div class="flex items-center gap-2">
        <Search class="w-3.5 h-3.5" />
        <span>搜索便携软件、分类、拼音...</span>
      </div>
      <kbd class="px-1.5 py-0.5 rounded text-[10px] font-mono bg-[var(--bg-app)] border border-[var(--border-subtle)] text-[var(--text-secondary)]">Ctrl K</kbd>
    </div>

    <!-- Right: Actions & Window Controls -->
    <div class="flex items-center gap-1">
      <!-- Theme Switcher -->
      <button
        @click="themeStore.toggleTheme"
        class="w-8 h-8 rounded-md flex items-center justify-center text-[var(--text-secondary)] hover:text-[var(--text-primary)] hover:bg-[var(--bg-card-hover)] transition-colors"
        :title="themeStore.isDark ? '切换至浅色模式' : '切换至深色模式'"
      >
        <Sun v-if="themeStore.isDark" class="w-4 h-4" />
        <Moon v-else class="w-4 h-4" />
      </button>

      <!-- Window Minimize -->
      <button
        @click="minimizeWindow"
        class="w-8 h-8 rounded-md flex items-center justify-center text-[var(--text-secondary)] hover:text-[var(--text-primary)] hover:bg-[var(--bg-card-hover)] transition-colors"
        title="最小化"
      >
        <Minus class="w-3.5 h-3.5" />
      </button>

      <!-- Window Maximize / Restore -->
      <button
        @click="toggleMaximize"
        class="w-8 h-8 rounded-md flex items-center justify-center text-[var(--text-secondary)] hover:text-[var(--text-primary)] hover:bg-[var(--bg-card-hover)] transition-colors"
        title="最大化 / 还原"
      >
        <Square class="w-3.5 h-3.5" />
      </button>

      <!-- Window Close -->
      <button
        @click="closeWindow"
        class="w-8 h-8 rounded-md flex items-center justify-center text-[var(--text-secondary)] hover:text-white hover:bg-[var(--status-danger)] transition-colors"
        title="关闭"
      >
        <X class="w-4 h-4" />
      </button>
    </div>
  </header>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { Search, Sun, Moon, Minus, Square, X } from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";
import { useThemeStore } from "@/stores/theme";
import { getCurrentWindow } from "@tauri-apps/api/window";

const libraryStore = useLibraryStore();
const themeStore = useThemeStore();

const currentViewTitle = computed(() => {
  if (libraryStore.selectedNav === "favorites") return "我的收藏";
  if (libraryStore.selectedNav === "recent") return "最近启动";
  if (libraryStore.selectedNav === "category") {
    const cat = libraryStore.categories.find(c => c.id === libraryStore.selectedCategoryId);
    return cat ? cat.name : "分类";
  }
  return "全部软件";
});

async function minimizeWindow() {
  try {
    const win = getCurrentWindow();
    await win.minimize();
  } catch {
    // In browser preview
  }
}

async function toggleMaximize() {
  try {
    const win = getCurrentWindow();
    await win.toggleMaximize();
  } catch {
    // In browser preview
  }
}

async function closeWindow() {
  try {
    const win = getCurrentWindow();
    await win.close();
  } catch {
    // In browser preview
  }
}
</script>
