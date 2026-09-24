<template>
  <div class="h-screen w-screen flex flex-col overflow-hidden bg-[var(--bg-app)] select-none">
    <!-- Window Custom Titlebar -->
    <TopBar />

    <!-- Main Workspace -->
    <div class="flex-1 flex overflow-hidden">
      <!-- Collapsible Sidebar -->
      <Sidebar />

      <!-- Main Content Area -->
      <main class="flex-1 flex flex-col overflow-hidden bg-[var(--bg-app)]">
        <!-- Subheader Toolbar -->
        <div class="h-12 px-6 flex items-center justify-between border-b border-[var(--border-subtle)] bg-[var(--bg-surface)] shrink-0">
          <!-- Left: Filter Title & Count -->
          <div class="flex items-center gap-2">
            <h2 class="text-sm font-bold text-[var(--text-primary)]">
              {{ currentTitle }}
            </h2>
            <span class="text-xs px-2 py-0.5 rounded-full bg-[var(--bg-app)] text-[var(--text-muted)] font-mono">
              {{ libraryStore.filteredSoftware.length }} 个应用
            </span>
          </div>

          <!-- Right: View Controls (Sort, View Mode, Add App) -->
          <div class="flex items-center gap-2">
            <!-- Sort dropdown -->
            <div class="flex items-center gap-1 text-xs text-[var(--text-secondary)]">
              <ArrowDownUp class="w-3.5 h-3.5 text-[var(--text-muted)]" />
              <select
                v-model="libraryStore.sortBy"
                class="bg-[var(--bg-card)] border border-[var(--border-subtle)] rounded-md px-2 py-1 text-xs text-[var(--text-primary)] outline-none cursor-pointer hover:border-[var(--border-strong)] transition-colors"
              >
                <option value="Custom">自定义排序</option>
                <option value="Name">名称字母 (A-Z)</option>
                <option value="LaunchCount">启动频率</option>
                <option value="LastLaunchedAt">最近使用</option>
              </select>
            </div>

            <!-- View Switcher (Grid / List) -->
            <div class="flex items-center p-0.5 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-card)]">
              <button
                @click="libraryStore.viewMode = 'Grid'"
                class="w-7 h-6 rounded flex items-center justify-center transition-colors"
                :class="libraryStore.viewMode === 'Grid' ? 'bg-[var(--accent-primary)] text-white' : 'text-[var(--text-muted)] hover:text-[var(--text-primary)]'"
                title="网格视图"
              >
                <LayoutGrid class="w-3.5 h-3.5" />
              </button>
              <button
                @click="libraryStore.viewMode = 'List'"
                class="w-7 h-6 rounded flex items-center justify-center transition-colors"
                :class="libraryStore.viewMode === 'List' ? 'bg-[var(--accent-primary)] text-white' : 'text-[var(--text-muted)] hover:text-[var(--text-primary)]'"
                title="列表视图"
              >
                <List class="w-3.5 h-3.5" />
              </button>
            </div>

            <!-- Add Application Button -->
            <button
              class="h-7 px-3 rounded-lg bg-[var(--accent-primary)] text-white text-xs font-medium flex items-center gap-1.5 shadow-xs hover:bg-[var(--accent-primary-hover)] transition-all cursor-pointer"
            >
              <Plus class="w-3.5 h-3.5" />
              <span>添加应用</span>
            </button>
          </div>
        </div>

        <!-- Scrollable Cards / Rows Viewport -->
        <div class="flex-1 overflow-y-auto p-6">
          <Transition name="fade" mode="out-in">
            <SoftwareGrid
              v-if="libraryStore.viewMode === 'Grid'"
              :key="'grid-' + libraryStore.selectedNav + '-' + libraryStore.selectedCategoryId"
              :software-list="libraryStore.filteredSoftware"
            />
            <SoftwareList
              v-else
              :key="'list-' + libraryStore.selectedNav + '-' + libraryStore.selectedCategoryId"
              :software-list="libraryStore.filteredSoftware"
            />
          </Transition>
        </div>
      </main>
    </div>

    <!-- Command Palette (Ctrl+K Launcher) -->
    <CommandPalette />
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue";
import TopBar from "@/components/shell/TopBar.vue";
import Sidebar from "@/components/shell/Sidebar.vue";
import SoftwareGrid from "@/components/software/SoftwareGrid.vue";
import SoftwareList from "@/components/software/SoftwareList.vue";
import CommandPalette from "@/components/search/CommandPalette.vue";
import { ArrowDownUp, LayoutGrid, List, Plus } from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";

const libraryStore = useLibraryStore();

const currentTitle = computed(() => {
  if (libraryStore.selectedNav === "favorites") return "我的收藏";
  if (libraryStore.selectedNav === "recent") return "最近启动";
  if (libraryStore.selectedNav === "category") {
    const cat = libraryStore.categories.find(c => c.id === libraryStore.selectedCategoryId);
    return cat ? cat.name : "分类";
  }
  return "全部软件";
});
</script>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 120ms ease, transform 120ms ease;
}

.fade-enter-from {
  opacity: 0;
  transform: translateY(4px);
}

.fade-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
