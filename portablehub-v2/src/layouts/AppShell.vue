<template>
  <NLayout
    position="absolute"
    class="h-screen w-screen overflow-hidden select-none"
    @dragover.prevent
    @dragenter.prevent
    @drop.prevent="handleFileDrop"
  >
    <!-- Window Custom Titlebar -->
    <NLayoutHeader bordered class="h-10 shrink-0">
      <TopBar />
    </NLayoutHeader>

    <NLayout has-sider position="absolute" style="top: 40px; bottom: 0;">
      <!-- Collapsible Sidebar -->
      <NLayoutSider
        bordered
        collapse-mode="width"
        :collapsed-width="64"
        :width="208"
        :collapsed="isCollapsed"
        show-trigger="bar"
        @update:collapsed="isCollapsed = $event"
      >
        <Sidebar :collapsed="isCollapsed" />
      </NLayoutSider>

      <!-- Main Content Area -->
      <NLayoutContent class="flex flex-col h-full bg-transparent">
        <!-- Subheader Toolbar -->
        <div class="h-12 px-5 flex items-center justify-between border-b border-[var(--n-border-color)] shrink-0 transition-colors">
          <!-- Left: Filter Title & Count -->
          <div class="flex items-center gap-2.5">
            <h2 class="text-xs font-bold text-[var(--n-text-color)] transition-colors">
              {{ currentTitle }}
            </h2>
            <NTag round :bordered="false" size="small" type="default" class="text-[11px] font-mono">
              {{ libraryStore.filteredSoftware.length }} 个应用
            </NTag>
          </div>

          <!-- Right: View Controls (Sort, View Mode, Add App) -->
          <div class="flex items-center gap-3">
            <!-- Sort dropdown via Naive UI NSelect -->
            <div class="flex items-center gap-1.5">
              <span class="text-xs text-[var(--n-text-color)] opacity-70 transition-colors">排序:</span>
              <NSelect
                v-model:value="libraryStore.sortBy"
                size="small"
                :options="sortOptions"
                style="width: 130px"
              />
            </div>

            <!-- View Switcher (Grid / List) via NButtonGroup -->
            <NButtonGroup size="small">
              <NButton
                :type="libraryStore.viewMode === 'Grid' ? 'primary' : 'default'"
                @click="libraryStore.viewMode = 'Grid'"
                title="网格视图"
              >
                <template #icon>
                  <LayoutGrid class="w-3.5 h-3.5" />
                </template>
              </NButton>
              <NButton
                :type="libraryStore.viewMode === 'List' ? 'primary' : 'default'"
                @click="libraryStore.viewMode = 'List'"
                title="列表视图"
              >
                <template #icon>
                  <List class="w-3.5 h-3.5" />
                </template>
              </NButton>
            </NButtonGroup>

            <!-- Add Application Primary Button via NButton -->
            <NButton
              type="primary"
              size="small"
              @click="libraryStore.openAddModal"
            >
              <template #icon>
                <Plus class="w-3.5 h-3.5" />
              </template>
              <span>添加应用</span>
            </NButton>
          </div>
        </div>

        <!-- Scrollable Cards / Rows Viewport -->
        <div class="flex-1 overflow-y-auto p-5">
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
      </NLayoutContent>
    </NLayout>

    <!-- Modals & Quick Launch Palette -->
    <CommandPalette />
    <AddEditSoftwareModal />
    <ScannerModal />
    <CategoryModal />
    <SettingsModal />
  </NLayout>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { NTag, NSelect, NButtonGroup, NButton, NLayout, NLayoutHeader, NLayoutSider, NLayoutContent } from "naive-ui";
import TopBar from "@/components/shell/TopBar.vue";
import Sidebar from "@/components/shell/Sidebar.vue";
import SoftwareGrid from "@/components/software/SoftwareGrid.vue";
import SoftwareList from "@/components/software/SoftwareList.vue";
import CommandPalette from "@/components/search/CommandPalette.vue";
import AddEditSoftwareModal from "@/components/modals/AddEditSoftwareModal.vue";
import ScannerModal from "@/components/modals/ScannerModal.vue";
import CategoryModal from "@/components/modals/CategoryModal.vue";
import SettingsModal from "@/components/modals/SettingsModal.vue";
import { LayoutGrid, List, Plus } from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";

const libraryStore = useLibraryStore();
const isCollapsed = ref(false);

const sortOptions = [
  { label: "默认排序", value: "Custom" },
  { label: "名称 (A-Z)", value: "Name" },
  { label: "启动频率", value: "LaunchCount" },
  { label: "最近启动", value: "LastLaunchedAt" },
];

const currentTitle = computed(() => {
  if (libraryStore.selectedNav === "favorites") return "我的收藏";
  if (libraryStore.selectedNav === "recent") return "最近启动";
  if (libraryStore.selectedNav === "category") {
    const cat = libraryStore.categories.find(c => c.id === libraryStore.selectedCategoryId);
    return cat ? cat.name : "分类";
  }
  return "全部软件";
});

function handleFileDrop(e: DragEvent) {
  const files = e.dataTransfer?.files;
  if (!files || files.length === 0) return;

  const file = files[0];
  const filePath = (file as any).path || file.name;
  if (filePath.toLowerCase().endsWith(".exe")) {
    const baseName = file.name.replace(/\.exe$/i, "");
    libraryStore.editingSoftware = {
      id: 0,
      name: baseName,
      exePath: filePath,
      description: "",
      arguments: "",
      workingDirectory: "",
      iconPath: "",
      categoryId: libraryStore.selectedCategoryId || (libraryStore.categories[0]?.id || 1),
      categoryName: "",
      isFavorite: false,
      launchCount: 0,
      lastLaunchedAt: null,
      sortOrder: libraryStore.softwareList.length + 1,
      runAsAdmin: false,
      singleInstance: true,
      tags: "",
      createdAt: "",
      updatedAt: "",
      isMissing: false,
      isRunning: false,
    };
    libraryStore.isAddEditModalOpen = true;
  }
}
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
