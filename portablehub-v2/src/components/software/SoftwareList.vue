<template>
  <div class="h-full">
    <!-- Empty State -->
    <div v-if="softwareList.length === 0" class="h-full flex items-center justify-center">
      <NEmpty
        class="h-72 flex flex-col items-center justify-center p-8 rounded-xl border border-dashed border-[var(--n-border-color)] bg-[var(--n-color-modal)] opacity-80"
        :description="libraryStore.searchQuery ? '未找到匹配软件' : '暂无便携软件'"
        size="large"
      >
        <template #extra>
          <div class="flex items-center gap-3 mt-2">
            <NButton type="primary" size="small" @click="libraryStore.openAddModal">
              <template #icon>
                <Plus class="w-3.5 h-3.5" />
              </template>
              <span>添加应用</span>
            </NButton>

            <NButton secondary size="small" @click="libraryStore.isScannerModalOpen = true">
              <template #icon>
                <FolderSearch class="w-3.5 h-3.5" />
              </template>
              <span>扫描导入</span>
            </NButton>
          </div>
        </template>
      </NEmpty>
    </div>

    <!-- Rows List -->
    <div v-else class="space-y-1.5">
      <div
        v-for="(item, index) in softwareList"
        :key="item.id"
        class="stagger-item group flex items-center justify-between p-2 rounded-lg border border-transparent hover:border-[var(--n-border-color)] bg-[var(--n-color)] hover:bg-[var(--n-color-modal)] transition-all cursor-pointer select-none shadow-sm"
        :style="{ '--stagger-index': Math.min(index, 12) }"
        @click="libraryStore.launchSoftware(item.id)"
        @contextmenu.prevent="handleContextMenu($event, item)"
      >
        <!-- Left: Icon + Title + Category + Path -->
        <div class="flex items-center gap-3 min-w-0 flex-1">
          <div class="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white font-bold text-xs shrink-0 overflow-hidden shadow-xs">
            <img v-if="item.iconPath" :src="item.iconPath" :alt="item.name" class="w-full h-full object-cover" />
            <span v-else>{{ item.name.slice(0, 1).toUpperCase() }}</span>
          </div>

          <div class="min-w-0 flex-1">
            <div class="flex items-center gap-2">
              <span class="text-xs font-semibold text-[var(--n-text-color)] group-hover:text-[var(--n-primary-color)] truncate transition-colors">
                {{ item.name }}
              </span>
              <span v-if="item.isRunning" class="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
              <NTag v-if="item.isMissing" size="tiny" type="warning" :bordered="false" round>
                缺失
              </NTag>
            </div>
            <div class="text-[11px] text-[var(--n-text-color)] opacity-60 truncate flex items-center gap-2 mt-0.5">
              <NTag size="tiny" :bordered="false" class="text-[10px]">
                {{ item.categoryName || '默认' }}
              </NTag>
              <span class="truncate font-mono">{{ item.exePath }}</span>
            </div>
          </div>
        </div>

        <!-- Right: Meta & Actions -->
        <div class="flex items-center gap-1.5 shrink-0" @click.stop>
          <span class="text-[11px] text-[var(--n-text-color)] opacity-60 hidden sm:inline mr-2">启动 {{ item.launchCount }} 次</span>

          <NButton
            quaternary
            circle
            size="tiny"
            @click.stop="libraryStore.toggleFavorite(item.id)"
            :title="item.isFavorite ? '取消收藏' : '添加收藏'"
          >
            <template #icon>
              <Star
                class="w-3.5 h-3.5 transition-colors"
                :class="item.isFavorite ? 'text-amber-500 fill-amber-500' : 'text-[var(--n-text-color)] opacity-60'"
              />
            </template>
          </NButton>

          <NButton
            quaternary
            circle
            size="tiny"
            class="opacity-0 group-hover:opacity-100 transition-opacity"
            @click.stop="libraryStore.openFolder(item.exePath)"
            title="打开所在目录"
          >
            <template #icon>
              <FolderOpen class="w-3.5 h-3.5 text-[var(--n-text-color)] opacity-60" />
            </template>
          </NButton>

          <NButton
            quaternary
            circle
            size="tiny"
            class="opacity-0 group-hover:opacity-100 transition-opacity"
            @click.stop="libraryStore.openEditModal(item)"
            title="编辑应用"
          >
            <template #icon>
              <Edit3 class="w-3.5 h-3.5 text-[var(--n-text-color)] opacity-60" />
            </template>
          </NButton>

          <NButton
            type="primary"
            size="tiny"
            class="opacity-0 group-hover:opacity-100 transition-opacity"
            @click.stop="libraryStore.launchSoftware(item.id)"
          >
            <template #icon>
              <Play class="w-3 h-3 fill-current" />
            </template>
            <span>启动</span>
          </NButton>
        </div>
      </div>
    </div>

    <!-- Right-click Context Menu -->
    <NDropdown
      placement="bottom-start"
      trigger="manual"
      :x="contextX"
      :y="contextY"
      :options="dropdownOptions"
      :show="showContextMenu"
      :on-clickoutside="() => showContextMenu = false"
      @select="handleMenuSelect"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, h, nextTick } from "vue";
import type { Software } from "@/types";
import {
  NEmpty,
  NTag,
  NButton,
  NDropdown,
  useDialog,
  useMessage,
  type DropdownOption,
} from "naive-ui";
import {
  Star,
  Play,
  FolderOpen,
  Plus,
  FolderSearch,
  Edit3,
  Shield,
  Copy,
  Trash2,
} from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";

defineProps<{
  softwareList: Software[];
}>();

const libraryStore = useLibraryStore();
const dialog = useDialog();
const message = useMessage();

const activeSoftware = ref<Software | null>(null);
const showContextMenu = ref(false);
const contextX = ref(0);
const contextY = ref(0);

const dropdownOptions: DropdownOption[] = [
  {
    label: "启动应用",
    key: "launch",
    icon: () => h(Play, { class: "w-4 h-4 text-blue-500" }),
  },
  {
    label: "以管理员身份运行",
    key: "launch-admin",
    icon: () => h(Shield, { class: "w-4 h-4 text-amber-500" }),
  },
  {
    type: "divider",
    key: "d1",
  },
  {
    label: "打开所在目录",
    key: "open-folder",
    icon: () => h(FolderOpen, { class: "w-4 h-4" }),
  },
  {
    label: "复制程序路径",
    key: "copy-path",
    icon: () => h(Copy, { class: "w-4 h-4" }),
  },
  {
    type: "divider",
    key: "d2",
  },
  {
    label: "编辑应用信息",
    key: "edit",
    icon: () => h(Edit3, { class: "w-4 h-4" }),
  },
  {
    label: "从库中移除",
    key: "delete",
    icon: () => h(Trash2, { class: "w-4 h-4 text-rose-500" }),
  },
];

function handleContextMenu(e: MouseEvent, item: Software) {
  activeSoftware.value = item;
  showContextMenu.value = false;
  nextTick(() => {
    contextX.value = e.clientX;
    contextY.value = e.clientY;
    showContextMenu.value = true;
  });
}

function handleMenuSelect(key: string | number) {
  showContextMenu.value = false;
  if (!activeSoftware.value) return;

  const item = activeSoftware.value;
  switch (key) {
    case "launch":
      libraryStore.launchSoftware(item.id, false);
      break;
    case "launch-admin":
      libraryStore.launchSoftware(item.id, true);
      break;
    case "open-folder":
      libraryStore.openFolder(item.exePath);
      break;
    case "copy-path":
      navigator.clipboard.writeText(item.exePath);
      message.success("程序路径已复制到剪贴板");
      break;
    case "edit":
      libraryStore.openEditModal(item);
      break;
    case "delete":
      dialog.warning({
        title: "从库中移除",
        content: `确定要从软件库中移除应用 "${item.name}" 吗？（注意：这不会删除您硬盘上的原始文件）`,
        positiveText: "确认移除",
        negativeText: "取消",
        onPositiveClick: () => {
          libraryStore.deleteSoftware(item.id);
          message.info(`已移除 "${item.name}"`);
        },
      });
      break;
  }
}
</script>
