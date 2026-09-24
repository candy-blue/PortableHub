<template>
  <div
    class="software-card-interactive relative flex flex-col justify-between p-3.5 rounded-xl border border-[var(--border-subtle)] bg-[var(--bg-card)] cursor-pointer group select-none overflow-visible"
    :class="{ 'opacity-70 border-dashed border-[var(--status-missing)]': software.isMissing }"
    @click="handleCardClick"
  >
    <!-- Top Action Row: Running Indicator, Category Badge, Favorite Star, More Menu -->
    <div class="flex items-center justify-between gap-1 mb-2">
      <!-- Status Badge -->
      <div class="flex items-center gap-1.5 min-w-0">
        <span
          v-if="software.isRunning"
          class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[10px] font-medium bg-[var(--status-running-bg)] text-[var(--status-running)]"
        >
          <span class="w-1.5 h-1.5 rounded-full bg-[var(--status-running)] animate-pulse"></span>
          运行中
        </span>

        <span
          v-else-if="software.isMissing"
          class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[10px] font-medium bg-[var(--status-missing-bg)] text-[var(--status-missing)]"
        >
          <AlertCircle class="w-3 h-3" />
          路径缺失
        </span>

        <span
          v-else
          class="text-[10px] px-1.5 py-0.5 rounded-md font-medium bg-[var(--bg-app)] text-[var(--text-secondary)] truncate max-w-[90px]"
        >
          {{ software.categoryName || '默认' }}
        </span>

        <Shield
          v-if="software.runAsAdmin"
          class="w-3 h-3 text-[var(--accent-primary)] shrink-0"
          title="默认以管理员身份运行"
        />
      </div>

      <!-- Favorite & More Buttons -->
      <div class="flex items-center gap-0.5 relative">
        <button
          @click.stop="libraryStore.toggleFavorite(software.id)"
          class="w-6 h-6 rounded-md flex items-center justify-center transition-colors cursor-pointer"
          :class="software.isFavorite ? 'text-amber-400 hover:text-amber-500' : 'text-[var(--text-muted)] hover:text-[var(--text-secondary)] opacity-0 group-hover:opacity-100'"
          title="收藏软件"
        >
          <Star class="w-3.5 h-3.5" :fill="software.isFavorite ? 'currentColor' : 'none'" />
        </button>

        <div class="relative">
          <button
            @click.stop="isMenuOpen = !isMenuOpen"
            class="w-6 h-6 rounded-md flex items-center justify-center text-[var(--text-muted)] hover:text-[var(--text-primary)] hover:bg-[var(--bg-app)] opacity-0 group-hover:opacity-100 transition-all cursor-pointer"
            title="更多操作"
          >
            <MoreVertical class="w-3.5 h-3.5" />
          </button>

          <!-- Dropdown Menu -->
          <div
            v-if="isMenuOpen"
            class="absolute right-0 top-7 w-36 rounded-lg border border-[var(--border-strong)] bg-[var(--bg-surface-elevated)] shadow-xl py-1 z-50 text-xs animate-fade-in"
            @click.stop
          >
            <button
              @click="handleLaunch(false)"
              class="w-full px-3 py-1.5 text-left flex items-center gap-2 hover:bg-[var(--bg-card-hover)] text-[var(--text-primary)] cursor-pointer"
            >
              <Play class="w-3.5 h-3.5 text-[var(--accent-primary)]" />
              <span>启动软件</span>
            </button>

            <button
              @click="handleLaunch(true)"
              class="w-full px-3 py-1.5 text-left flex items-center gap-2 hover:bg-[var(--bg-card-hover)] text-[var(--text-primary)] cursor-pointer"
            >
              <Shield class="w-3.5 h-3.5 text-amber-500" />
              <span>以管理员运行</span>
            </button>

            <button
              @click="handleOpenFolder"
              class="w-full px-3 py-1.5 text-left flex items-center gap-2 hover:bg-[var(--bg-card-hover)] text-[var(--text-primary)] cursor-pointer"
            >
              <FolderOpen class="w-3.5 h-3.5 text-[var(--text-muted)]" />
              <span>打开所在目录</span>
            </button>

            <button
              @click="handleCopyPath"
              class="w-full px-3 py-1.5 text-left flex items-center gap-2 hover:bg-[var(--bg-card-hover)] text-[var(--text-primary)] cursor-pointer"
            >
              <Copy class="w-3.5 h-3.5 text-[var(--text-muted)]" />
              <span>复制路径</span>
            </button>

            <div class="my-1 border-t border-[var(--border-subtle)]"></div>

            <button
              @click="handleEdit"
              class="w-full px-3 py-1.5 text-left flex items-center gap-2 hover:bg-[var(--bg-card-hover)] text-[var(--text-primary)] cursor-pointer"
            >
              <Edit3 class="w-3.5 h-3.5 text-[var(--text-muted)]" />
              <span>编辑应用</span>
            </button>

            <button
              @click="handleDelete"
              class="w-full px-3 py-1.5 text-left flex items-center gap-2 hover:bg-rose-500/10 text-rose-500 cursor-pointer"
            >
              <Trash2 class="w-3.5 h-3.5" />
              <span>从库中删除</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Middle: App Icon + Name + Description -->
    <div class="flex items-start gap-3 my-1">
      <!-- Icon -->
      <div class="w-10 h-10 rounded-xl bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white font-bold text-sm shadow-sm shrink-0 overflow-hidden">
        <img
          v-if="software.iconPath"
          :src="software.iconPath"
          :alt="software.name"
          class="w-full h-full object-cover"
        />
        <span v-else>{{ software.name.slice(0, 1).toUpperCase() }}</span>
      </div>

      <!-- Texts -->
      <div class="flex-1 min-w-0">
        <h3 class="text-xs font-semibold text-[var(--text-primary)] truncate group-hover:text-[var(--accent-primary)] transition-colors">
          {{ software.name }}
        </h3>
        <p class="text-[11px] text-[var(--text-muted)] truncate mt-0.5" :title="software.description || software.exePath">
          {{ software.description || software.exePath }}
        </p>
      </div>
    </div>

    <!-- Bottom Meta: Launch Count & Quick Launch Action -->
    <div class="mt-3 pt-2 border-t border-[var(--border-subtle)] flex items-center justify-between text-[11px] text-[var(--text-muted)]">
      <div class="flex items-center gap-1">
        <span>启动 {{ software.launchCount }} 次</span>
      </div>

      <!-- Quick Launch Hover Button -->
      <button
        @click.stop="handleLaunch(false)"
        class="h-6 px-2.5 rounded-md bg-[var(--accent-primary)] text-white text-[11px] font-medium flex items-center gap-1 shadow-xs hover:bg-[var(--accent-primary-hover)] transition-all opacity-0 group-hover:opacity-100 cursor-pointer"
      >
        <Play class="w-3 h-3 fill-current" />
        <span>启动</span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from "vue";
import type { Software } from "@/types";
import {
  Star,
  MoreVertical,
  Play,
  AlertCircle,
  Shield,
  FolderOpen,
  Copy,
  Edit3,
  Trash2,
} from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";

const props = defineProps<{
  software: Software;
}>();

const libraryStore = useLibraryStore();
const isMenuOpen = ref(false);

function handleCardClick() {
  handleLaunch(false);
}

function handleLaunch(forceAdmin: boolean) {
  isMenuOpen.value = false;
  libraryStore.launchSoftware(props.software.id, forceAdmin);
}

function handleOpenFolder() {
  isMenuOpen.value = false;
  libraryStore.openFolder(props.software.exePath);
}

function handleCopyPath() {
  isMenuOpen.value = false;
  navigator.clipboard.writeText(props.software.exePath);
}

function handleEdit() {
  isMenuOpen.value = false;
  libraryStore.openEditModal(props.software);
}

function handleDelete() {
  isMenuOpen.value = false;
  if (confirm(`确定要从库中移除应用 "${props.software.name}" 吗？（不会删除磁盘文件）`)) {
    libraryStore.deleteSoftware(props.software.id);
  }
}

function closeDropdown(_e: MouseEvent) {
  isMenuOpen.value = false;
}

onMounted(() => {
  window.addEventListener("click", closeDropdown);
});

onUnmounted(() => {
  window.removeEventListener("click", closeDropdown);
});
</script>
