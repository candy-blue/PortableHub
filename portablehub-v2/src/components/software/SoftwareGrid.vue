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

    <!-- Grid Cards -->
    <div
      v-else
      class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5 gap-3"
    >
      <div
        v-for="(item, index) in softwareList"
        :key="item.id"
        class="stagger-item"
        :style="{ '--stagger-index': Math.min(index, 12) }"
      >
        <SoftwareCard :software="item" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { Software } from "@/types";
import { FolderOpen, Plus, FolderSearch } from "@lucide/vue";
import SoftwareCard from "./SoftwareCard.vue";
import { useLibraryStore } from "@/stores/library";

defineProps<{
  softwareList: Software[];
}>();

const libraryStore = useLibraryStore();
</script>
