<template>
  <aside
    class="sidebar-transition h-full flex flex-col justify-between border-r border-[var(--border-subtle)] bg-[var(--bg-sidebar)] select-none shrink-0"
    :class="isCollapsed ? 'w-16' : 'w-52'"
  >
    <!-- Top Nav Section -->
    <div class="p-2 space-y-1 overflow-y-auto overflow-x-hidden flex-1">
      <!-- Section: Main Nav -->
      <button
        @click="libraryStore.selectNav('all')"
        class="w-full h-8 px-2.5 rounded-lg flex items-center gap-2.5 transition-colors text-xs font-medium cursor-pointer"
        :class="libraryStore.selectedNav === 'all'
          ? 'bg-[var(--accent-primary)] text-white shadow-xs'
          : 'text-[var(--text-secondary)] hover:bg-[var(--bg-card)] hover:text-[var(--text-primary)]'"
        :title="isCollapsed ? '全部软件' : ''"
      >
        <Grid class="w-4 h-4 shrink-0" />
        <span v-if="!isCollapsed" class="truncate flex-1 text-left">全部软件</span>
        <span
          v-if="!isCollapsed"
          class="text-[10px] px-1.5 py-0.2 rounded-full font-mono"
          :class="libraryStore.selectedNav === 'all' ? 'bg-white/20 text-white' : 'bg-[var(--bg-card)] text-[var(--text-muted)]'"
        >
          {{ libraryStore.softwareList.length }}
        </span>
      </button>

      <button
        @click="libraryStore.selectNav('favorites')"
        class="w-full h-8 px-2.5 rounded-lg flex items-center gap-2.5 transition-colors text-xs font-medium cursor-pointer"
        :class="libraryStore.selectedNav === 'favorites'
          ? 'bg-[var(--accent-primary)] text-white shadow-xs'
          : 'text-[var(--text-secondary)] hover:bg-[var(--bg-card)] hover:text-[var(--text-primary)]'"
        :title="isCollapsed ? '我的收藏' : ''"
      >
        <Star class="w-4 h-4 shrink-0" />
        <span v-if="!isCollapsed" class="truncate flex-1 text-left">我的收藏</span>
        <span
          v-if="!isCollapsed"
          class="text-[10px] px-1.5 py-0.2 rounded-full font-mono"
          :class="libraryStore.selectedNav === 'favorites' ? 'bg-white/20 text-white' : 'bg-[var(--bg-card)] text-[var(--text-muted)]'"
        >
          {{ libraryStore.favoritesCount }}
        </span>
      </button>

      <button
        @click="libraryStore.selectNav('recent')"
        class="w-full h-8 px-2.5 rounded-lg flex items-center gap-2.5 transition-colors text-xs font-medium cursor-pointer"
        :class="libraryStore.selectedNav === 'recent'
          ? 'bg-[var(--accent-primary)] text-white shadow-xs'
          : 'text-[var(--text-secondary)] hover:bg-[var(--bg-card)] hover:text-[var(--text-primary)]'"
        :title="isCollapsed ? '最近启动' : ''"
      >
        <Clock class="w-4 h-4 shrink-0" />
        <span v-if="!isCollapsed" class="truncate flex-1 text-left">最近启动</span>
        <span
          v-if="!isCollapsed"
          class="text-[10px] px-1.5 py-0.2 rounded-full font-mono"
          :class="libraryStore.selectedNav === 'recent' ? 'bg-white/20 text-white' : 'bg-[var(--bg-card)] text-[var(--text-muted)]'"
        >
          {{ libraryStore.recentCount }}
        </span>
      </button>

      <!-- Divider -->
      <div class="my-2 border-t border-[var(--border-subtle)]"></div>

      <!-- Categories Header with Add Category (+) Button -->
      <div v-if="!isCollapsed" class="px-2.5 py-1 flex items-center justify-between text-[11px] font-semibold text-[var(--text-muted)]">
        <span>软件分类</span>
        <button
          @click.stop="libraryStore.isCategoryModalOpen = true"
          class="w-5 h-5 rounded flex items-center justify-center text-[var(--text-muted)] hover:text-[var(--accent-primary)] hover:bg-[var(--bg-card)] transition-colors cursor-pointer"
          title="新建分类"
        >
          <Plus class="w-3.5 h-3.5" />
        </button>
      </div>

      <!-- Categories List -->
      <div class="space-y-0.5">
        <button
          v-for="cat in libraryStore.categories"
          :key="cat.id"
          @click="libraryStore.selectCategory(cat.id)"
          class="w-full h-8 px-2.5 rounded-lg flex items-center gap-2.5 transition-colors text-xs cursor-pointer"
          :class="libraryStore.selectedNav === 'category' && libraryStore.selectedCategoryId === cat.id
            ? 'bg-[var(--accent-primary)] text-white font-medium shadow-xs'
            : 'text-[var(--text-secondary)] hover:bg-[var(--bg-card)] hover:text-[var(--text-primary)]'"
          :title="isCollapsed ? cat.name : ''"
        >
          <span
            class="w-2.5 h-2.5 rounded-full shrink-0"
            :style="{ backgroundColor: cat.color || '#3b82f6' }"
          ></span>
          <span v-if="!isCollapsed" class="truncate flex-1 text-left">{{ cat.name }}</span>
          <span
            v-if="!isCollapsed"
            class="text-[10px] px-1.5 py-0.2 rounded-full font-mono"
            :class="libraryStore.selectedNav === 'category' && libraryStore.selectedCategoryId === cat.id ? 'bg-white/20 text-white' : 'text-[var(--text-muted)]'"
          >
            {{ libraryStore.categoryCounts.get(cat.id) || 0 }}
          </span>
        </button>
      </div>
    </div>

    <!-- Bottom Actions Section -->
    <div class="p-2 border-t border-[var(--border-subtle)] space-y-1 shrink-0">
      <!-- Quick Scan Trigger -->
      <button
        @click="libraryStore.isScannerModalOpen = true"
        class="w-full h-8 px-2.5 rounded-lg flex items-center gap-2.5 text-xs text-[var(--text-secondary)] hover:bg-[var(--bg-card)] hover:text-[var(--text-primary)] transition-colors cursor-pointer"
        :title="isCollapsed ? '扫描目录' : ''"
      >
        <FolderSearch class="w-4 h-4 shrink-0 text-[var(--accent-primary)]" />
        <span v-if="!isCollapsed" class="truncate flex-1 text-left">扫描导入</span>
      </button>

      <!-- Settings -->
      <button
        @click="libraryStore.isSettingsModalOpen = true"
        class="w-full h-8 px-2.5 rounded-lg flex items-center gap-2.5 text-xs text-[var(--text-secondary)] hover:bg-[var(--bg-card)] hover:text-[var(--text-primary)] transition-colors cursor-pointer"
        :title="isCollapsed ? '偏好设置' : ''"
      >
        <Settings class="w-4 h-4 shrink-0" />
        <span v-if="!isCollapsed" class="truncate flex-1 text-left">偏好设置</span>
      </button>

      <!-- Collapse / Expand Toggle Button -->
      <button
        @click="isCollapsed = !isCollapsed"
        class="w-full h-8 px-2.5 rounded-lg flex items-center gap-2.5 text-xs text-[var(--text-muted)] hover:bg-[var(--bg-card)] hover:text-[var(--text-primary)] transition-colors cursor-pointer"
        :title="isCollapsed ? '展开侧栏' : '折叠侧栏'"
      >
        <PanelLeftClose v-if="!isCollapsed" class="w-4 h-4 shrink-0" />
        <PanelLeftOpen v-else class="w-4 h-4 shrink-0" />
        <span v-if="!isCollapsed" class="truncate flex-1 text-left">收起侧栏</span>
      </button>
    </div>
  </aside>
</template>

<script setup lang="ts">
import { ref } from "vue";
import {
  Grid,
  Star,
  Clock,
  FolderSearch,
  Settings,
  PanelLeftClose,
  PanelLeftOpen,
  Plus,
} from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";

const libraryStore = useLibraryStore();
const isCollapsed = ref(false);
</script>
