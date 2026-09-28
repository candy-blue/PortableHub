<template>
  <header
    class="h-10 w-full flex items-center justify-between pl-3 pr-0 select-none border-b border-[var(--border-subtle)] bg-[var(--bg-surface)] shrink-0 z-30"
  >
    <!-- Left: App Brand & Breadcrumb (Draggable region) -->
    <div
      class="flex items-center gap-2.5 h-full cursor-default"
      data-tauri-drag-region
      @dblclick="toggleMaximize"
    >
      <div class="w-5 h-5 rounded bg-[var(--accent-primary)] flex items-center justify-center text-white font-bold text-[11px] shadow-xs pointer-events-none">
        PH
      </div>
      <span class="font-semibold text-xs tracking-tight text-[var(--text-primary)] pointer-events-none">PortableHub</span>
      <NTag size="small" :bordered="false" round type="info" class="text-[10px] pointer-events-none font-mono">
        v2.0
      </NTag>

      <div class="hidden sm:flex items-center text-xs text-[var(--text-muted)] gap-1 pl-2 border-l border-[var(--border-subtle)] pointer-events-none">
        <span>/</span>
        <span class="text-[var(--text-secondary)] font-medium">{{ currentViewTitle }}</span>
      </div>
    </div>

    <!-- Center: Search Bar Trigger (Draggable space around it) -->
    <div
      class="flex-1 h-full flex items-center justify-center px-4 cursor-default"
      data-tauri-drag-region
      @dblclick="toggleMaximize"
    >
      <button
        @click.stop="libraryStore.isCommandPaletteOpen = true"
        class="w-full max-w-sm h-7 px-3 rounded-md border border-[var(--border-subtle)] bg-[var(--bg-card)] hover:bg-[var(--bg-card-hover)] hover:border-[var(--border-strong)] flex items-center justify-between text-xs text-[var(--text-muted)] cursor-pointer transition-all shadow-xs"
      >
        <div class="flex items-center gap-2 pointer-events-none">
          <Search class="w-3.5 h-3.5" />
          <span>搜索便携软件、分类、拼音...</span>
        </div>
        <kbd class="px-1.5 py-0.2 rounded text-[10px] font-mono bg-[var(--bg-app)] border border-[var(--border-subtle)] text-[var(--text-secondary)] pointer-events-none">Ctrl K</kbd>
      </button>
    </div>

    <!-- Right: Actions & Windows Native Caption Buttons (NOT draggable) -->
    <div class="flex items-center h-full">
      <!-- Theme Switcher -->
      <button
        @click.stop="themeStore.toggleTheme"
        class="w-9 h-full flex items-center justify-center text-[var(--text-secondary)] hover:text-[var(--text-primary)] hover:bg-black/5 dark:hover:bg-white/10 transition-colors cursor-pointer"
        :title="themeStore.isDark ? '切换至浅色模式' : '切换至深色模式'"
      >
        <Sun v-if="themeStore.isDark" class="w-3.5 h-3.5" />
        <Moon v-else class="w-3.5 h-3.5" />
      </button>

      <!-- Window Minimize -->
      <button
        @click.stop="minimizeWindow"
        class="w-11 h-full flex items-center justify-center text-[var(--text-secondary)] hover:text-[var(--text-primary)] hover:bg-black/5 dark:hover:bg-white/10 transition-colors cursor-pointer"
        title="最小化"
      >
        <Minus class="w-3.5 h-3.5" />
      </button>

      <!-- Window Maximize / Restore -->
      <button
        @click.stop="toggleMaximize"
        class="w-11 h-full flex items-center justify-center text-[var(--text-secondary)] hover:text-[var(--text-primary)] hover:bg-black/5 dark:hover:bg-white/10 transition-colors cursor-pointer"
        title="最大化 / 还原"
      >
        <Square class="w-3.5 h-3.5" />
      </button>

      <!-- Window Close -->
      <button
        @click.stop="closeWindow"
        class="w-11 h-full flex items-center justify-center text-[var(--text-secondary)] hover:text-white hover:bg-[#e81123] transition-colors cursor-pointer"
        title="关闭"
      >
        <X class="w-4 h-4" />
      </button>
    </div>
  </header>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { NTag } from "naive-ui";
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
  } catch (e) {
    console.error("Window minimize error:", e);
  }
}

async function toggleMaximize() {
  try {
    const win = getCurrentWindow();
    await win.toggleMaximize();
  } catch (e) {
    console.error("Window toggleMaximize error:", e);
  }
}

async function closeWindow() {
  try {
    const win = getCurrentWindow();
    await win.close();
  } catch (e) {
    console.error("Window close error:", e);
  }
}
</script>
