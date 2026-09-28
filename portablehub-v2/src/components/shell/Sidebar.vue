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
        class="w-full h-8 px-2.5 rounded-md flex items-center justify-between transition-colors text-xs font-medium cursor-pointer"
        :class="libraryStore.selectedNav === 'all'
          ? 'bg-[var(--accent-primary)] text-white shadow-xs'
          : 'text-[var(--text-secondary)] hover:bg-[var(--bg-card)] hover:text-[var(--text-primary)]'"
        :title="isCollapsed ? '全部软件' : ''"
      >
        <div class="flex items-center gap-2.5 min-w-0">
          <Grid class="w-4 h-4 shrink-0" />
          <span v-if="!isCollapsed" class="truncate">全部软件</span>
        </div>
        <NBadge
          v-if="!isCollapsed"
          :value="libraryStore.softwareList.length"
          :max="999"
          :type="libraryStore.selectedNav === 'all' ? 'info' : 'default'"
        />
      </button>

      <button
        @click="libraryStore.selectNav('favorites')"
        class="w-full h-8 px-2.5 rounded-md flex items-center justify-between transition-colors text-xs font-medium cursor-pointer"
        :class="libraryStore.selectedNav === 'favorites'
          ? 'bg-[var(--accent-primary)] text-white shadow-xs'
          : 'text-[var(--text-secondary)] hover:bg-[var(--bg-card)] hover:text-[var(--text-primary)]'"
        :title="isCollapsed ? '我的收藏' : ''"
      >
        <div class="flex items-center gap-2.5 min-w-0">
          <Star class="w-4 h-4 shrink-0" />
          <span v-if="!isCollapsed" class="truncate">我的收藏</span>
        </div>
        <NBadge
          v-if="!isCollapsed"
          :value="libraryStore.favoritesCount"
          :max="999"
          :type="libraryStore.selectedNav === 'favorites' ? 'info' : 'default'"
        />
      </button>

      <button
        @click="libraryStore.selectNav('recent')"
        class="w-full h-8 px-2.5 rounded-md flex items-center justify-between transition-colors text-xs font-medium cursor-pointer"
        :class="libraryStore.selectedNav === 'recent'
          ? 'bg-[var(--accent-primary)] text-white shadow-xs'
          : 'text-[var(--text-secondary)] hover:bg-[var(--bg-card)] hover:text-[var(--text-primary)]'"
        :title="isCollapsed ? '最近启动' : ''"
      >
        <div class="flex items-center gap-2.5 min-w-0">
          <Clock class="w-4 h-4 shrink-0" />
          <span v-if="!isCollapsed" class="truncate">最近启动</span>
        </div>
        <NBadge
          v-if="!isCollapsed"
          :value="libraryStore.recentCount"
          :max="999"
          :type="libraryStore.selectedNav === 'recent' ? 'info' : 'default'"
        />
      </button>

      <!-- Divider -->
      <div class="my-2 border-t border-[var(--border-subtle)]"></div>

      <!-- Categories Header with Add Category (+) Button -->
      <div v-if="!isCollapsed" class="px-2.5 py-1 flex items-center justify-between text-[11px] font-semibold text-[var(--text-muted)]">
        <span>软件分类</span>
        <NButton
          quaternary
          circle
          size="tiny"
          @click.stop="libraryStore.isCategoryModalOpen = true"
          title="新建分类"
        >
          <template #icon>
            <Plus class="w-3.5 h-3.5" />
          </template>
        </NButton>
      </div>

      <!-- Categories List -->
      <div class="space-y-0.5">
        <button
          v-for="cat in libraryStore.categories"
          :key="cat.id"
          @click="libraryStore.selectCategory(cat.id)"
          class="w-full h-8 px-2.5 rounded-md flex items-center justify-between transition-colors text-xs cursor-pointer"
          :class="libraryStore.selectedNav === 'category' && libraryStore.selectedCategoryId === cat.id
            ? 'bg-[var(--accent-primary)] text-white font-medium shadow-xs'
            : 'text-[var(--text-secondary)] hover:bg-[var(--bg-card)] hover:text-[var(--text-primary)]'"
          :title="isCollapsed ? cat.name : ''"
        >
          <div class="flex items-center gap-2.5 min-w-0">
            <span
              class="w-2.5 h-2.5 rounded-full shrink-0"
              :style="{ backgroundColor: cat.color || '#0078D4' }"
            ></span>
            <span v-if="!isCollapsed" class="truncate">{{ cat.name }}</span>
          </div>
          <NBadge
            v-if="!isCollapsed"
            :value="libraryStore.categoryCounts.get(cat.id) || 0"
            :max="999"
            :type="libraryStore.selectedNav === 'category' && libraryStore.selectedCategoryId === cat.id ? 'info' : 'default'"
          />
        </button>
      </div>
    </div>

    <!-- Bottom Actions Section -->
    <div class="p-2 border-t border-[var(--border-subtle)] space-y-1 shrink-0">
      <!-- Quick Scan Trigger -->
      <button
        @click="libraryStore.isScannerModalOpen = true"
        class="w-full h-8 px-2.5 rounded-md flex items-center gap-2.5 text-xs text-[var(--text-secondary)] hover:bg-[var(--bg-card)] hover:text-[var(--text-primary)] transition-colors cursor-pointer"
        :title="isCollapsed ? '扫描目录' : ''"
      >
        <FolderSearch class="w-4 h-4 shrink-0 text-[var(--accent-primary)]" />
        <span v-if="!isCollapsed" class="truncate flex-1 text-left">扫描导入</span>
      </button>

      <!-- Settings -->
      <button
        @click="libraryStore.isSettingsModalOpen = true"
        class="w-full h-8 px-2.5 rounded-md flex items-center gap-2.5 text-xs text-[var(--text-secondary)] hover:bg-[var(--bg-card)] hover:text-[var(--text-primary)] transition-colors cursor-pointer"
        :title="isCollapsed ? '设置' : ''"
      >
        <Settings class="w-4 h-4 shrink-0" />
        <span v-if="!isCollapsed" class="truncate flex-1 text-left">偏好设置</span>
      </button>

      <!-- Collapse / Expand Toggle Button -->
      <button
        @click="isCollapsed = !isCollapsed"
        class="w-full h-8 px-2.5 rounded-md flex items-center gap-2.5 text-xs text-[var(--text-muted)] hover:bg-[var(--bg-card)] hover:text-[var(--text-primary)] transition-colors cursor-pointer"
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
import { NBadge, NButton } from "naive-ui";
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
