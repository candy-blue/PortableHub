<template>
  <div>
    <!-- Empty State -->
    <div
      v-if="softwareList.length === 0"
      class="h-80 flex flex-col items-center justify-center text-center p-8 rounded-2xl border border-dashed border-[var(--border-subtle)] bg-[var(--bg-card)]/50"
    >
      <div class="w-14 h-14 rounded-full bg-[var(--bg-surface)] flex items-center justify-center text-[var(--accent-primary)] mb-3 shadow-sm border border-[var(--border-subtle)]">
        <FolderOpen class="w-7 h-7" />
      </div>
      <h4 class="text-sm font-semibold text-[var(--text-primary)]">
        {{ libraryStore.searchQuery ? '未找到匹配软件' : '暂无便携软件' }}
      </h4>
      <p class="text-xs text-[var(--text-muted)] mt-1.5 max-w-sm leading-relaxed">
        {{ libraryStore.searchQuery ? '尝试清空搜索关键字或切换分类。' : '您可以手动登记便携应用，或一键扫描本地目录自动识别导入。' }}
      </p>

      <div class="flex items-center gap-3 mt-5">
        <button
          @click="libraryStore.openAddModal"
          class="h-8 px-4 rounded-lg bg-[var(--accent-primary)] hover:bg-[var(--accent-primary-hover)] text-white text-xs font-medium flex items-center gap-1.5 shadow-xs transition-colors cursor-pointer"
        >
          <Plus class="w-3.5 h-3.5" />
          <span>添加应用</span>
        </button>

        <button
          @click="libraryStore.isScannerModalOpen = true"
          class="h-8 px-4 rounded-lg border border-[var(--border-strong)] bg-[var(--bg-surface)] hover:bg-[var(--bg-card-hover)] text-[var(--text-primary)] text-xs font-medium flex items-center gap-1.5 shadow-xs transition-colors cursor-pointer"
        >
          <FolderSearch class="w-3.5 h-3.5 text-[var(--accent-primary)]" />
          <span>扫描目录</span>
        </button>
      </div>
    </div>

    <!-- Rows List -->
    <div v-else class="space-y-1.5">
      <div
        v-for="(item, index) in softwareList"
        :key="item.id"
        class="stagger-item group flex items-center justify-between p-2.5 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-card)] hover:bg-[var(--bg-card-hover)] hover:border-[var(--border-strong)] transition-all cursor-pointer"
        :style="{ '--stagger-index': Math.min(index, 12) }"
        @click="libraryStore.launchSoftware(item.id)"
      >
        <!-- Left: Icon + Title + Category + Path -->
        <div class="flex items-center gap-3 min-w-0 flex-1">
          <div class="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white font-bold text-xs shrink-0 overflow-hidden shadow-xs">
            <img v-if="item.iconPath" :src="item.iconPath" :alt="item.name" class="w-full h-full object-cover" />
            <span v-else>{{ item.name.slice(0, 1).toUpperCase() }}</span>
          </div>

          <div class="min-w-0 flex-1">
            <div class="flex items-center gap-2">
              <span class="text-xs font-semibold text-[var(--text-primary)] group-hover:text-[var(--accent-primary)] truncate">
                {{ item.name }}
              </span>
              <span v-if="item.isRunning" class="w-2 h-2 rounded-full bg-[var(--status-running)] animate-pulse"></span>
              <span v-if="item.isMissing" class="text-[10px] text-amber-500 font-mono">缺失</span>
            </div>
            <div class="text-[11px] text-[var(--text-muted)] truncate flex items-center gap-2 mt-0.5">
              <span>{{ item.categoryName || '默认' }}</span>
              <span>·</span>
              <span class="truncate font-mono">{{ item.exePath }}</span>
            </div>
          </div>
        </div>

        <!-- Right: Meta & Actions -->
        <div class="flex items-center gap-2 shrink-0">
          <span class="text-[11px] text-[var(--text-muted)] hidden sm:inline mr-2">启动 {{ item.launchCount }} 次</span>

          <button
            @click.stop="libraryStore.toggleFavorite(item.id)"
            class="w-7 h-7 rounded-md flex items-center justify-center transition-colors cursor-pointer"
            :class="item.isFavorite ? 'text-amber-400' : 'text-[var(--text-muted)] hover:text-[var(--text-secondary)]'"
            title="收藏"
          >
            <Star class="w-3.5 h-3.5" :fill="item.isFavorite ? 'currentColor' : 'none'" />
          </button>

          <button
            @click.stop="libraryStore.openFolder(item.exePath)"
            class="w-7 h-7 rounded-md flex items-center justify-center text-[var(--text-muted)] hover:text-[var(--text-primary)] hover:bg-[var(--bg-app)] opacity-0 group-hover:opacity-100 transition-all cursor-pointer"
            title="打开所在目录"
          >
            <FolderOpen class="w-3.5 h-3.5" />
          </button>

          <button
            @click.stop="libraryStore.openEditModal(item)"
            class="w-7 h-7 rounded-md flex items-center justify-center text-[var(--text-muted)] hover:text-[var(--text-primary)] hover:bg-[var(--bg-app)] opacity-0 group-hover:opacity-100 transition-all cursor-pointer"
            title="编辑应用"
          >
            <Edit3 class="w-3.5 h-3.5" />
          </button>

          <button
            @click.stop="libraryStore.launchSoftware(item.id)"
            class="h-6 px-2.5 rounded-md bg-[var(--accent-primary)] text-white text-[11px] font-medium flex items-center gap-1 shadow-xs hover:bg-[var(--accent-primary-hover)] transition-all opacity-0 group-hover:opacity-100 cursor-pointer"
          >
            <Play class="w-3 h-3 fill-current" />
            <span>启动</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { Software } from "@/types";
import { Star, Play, FolderOpen, Plus, FolderSearch, Edit3 } from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";

defineProps<{
  softwareList: Software[];
}>();

const libraryStore = useLibraryStore();
</script>
