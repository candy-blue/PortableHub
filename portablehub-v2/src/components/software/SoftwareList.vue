<template>
  <div class="space-y-1.5">
    <div
      v-for="(item, index) in softwareList"
      :key="item.id"
      class="stagger-item group flex items-center justify-between p-2.5 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-card)] hover:bg-[var(--bg-card-hover)] hover:border-[var(--border-strong)] transition-all cursor-pointer"
      :style="{ '--stagger-index': Math.min(index, 12) }"
      @click="libraryStore.launchSoftware(item.id)"
    >
      <!-- Left: Icon + Title + Category + Path -->
      <div class="flex items-center gap-3 min-w-0 flex-1">
        <div class="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white font-bold text-xs shrink-0 overflow-hidden">
          <img v-if="item.iconPath" :src="item.iconPath" :alt="item.name" class="w-full h-full object-cover" />
          <span v-else>{{ item.name.slice(0, 1).toUpperCase() }}</span>
        </div>

        <div class="min-w-0 flex-1">
          <div class="flex items-center gap-2">
            <span class="text-xs font-semibold text-[var(--text-primary)] group-hover:text-[var(--accent-primary)] truncate">
              {{ item.name }}
            </span>
            <span v-if="item.isRunning" class="w-2 h-2 rounded-full bg-[var(--status-running)]"></span>
            <span v-if="item.isMissing" class="text-[10px] text-amber-500 font-mono">缺失</span>
          </div>
          <div class="text-[11px] text-[var(--text-muted)] truncate flex items-center gap-2">
            <span>{{ item.categoryName }}</span>
            <span>·</span>
            <span class="truncate font-mono">{{ item.exePath }}</span>
          </div>
        </div>
      </div>

      <!-- Right: Meta & Actions -->
      <div class="flex items-center gap-3 shrink-0">
        <span class="text-[11px] text-[var(--text-muted)] hidden sm:inline">启动 {{ item.launchCount }} 次</span>

        <button
          @click.stop="libraryStore.toggleFavorite(item.id)"
          class="w-7 h-7 rounded-md flex items-center justify-center transition-colors"
          :class="item.isFavorite ? 'text-amber-400' : 'text-[var(--text-muted)] hover:text-[var(--text-secondary)]'"
        >
          <Star class="w-3.5 h-3.5" :fill="item.isFavorite ? 'currentColor' : 'none'" />
        </button>

        <button
          @click.stop="libraryStore.launchSoftware(item.id)"
          class="h-6 px-2.5 rounded-md bg-[var(--accent-primary)] text-white text-[11px] font-medium flex items-center gap-1 shadow-xs hover:bg-[var(--accent-primary-hover)] transition-all opacity-0 group-hover:opacity-100"
        >
          <Play class="w-3 h-3 fill-current" />
          <span>启动</span>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { Software } from "@/types";
import { Star, Play } from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";

defineProps<{
  softwareList: Software[];
}>();

const libraryStore = useLibraryStore();
</script>
