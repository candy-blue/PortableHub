<template>
  <div class="h-full">
    <!-- Empty State -->
    <div v-if="softwareList.length === 0" class="h-full flex items-center justify-center">
      <NEmpty
        class="flex flex-col items-center justify-center p-8 rounded-xl bg-transparent"
        :description="libraryStore.searchQuery ? '未找到匹配软件' : '还没添加任何便携软件'"
        size="huge"
      >
        <template #extra>
          <div class="flex items-center gap-3 mt-4">
            <NButton type="primary" @click="libraryStore.openAddModal">
              <template #icon>
                <Plus class="w-4 h-4" />
              </template>
              <span>手动添加</span>
            </NButton>

            <NButton secondary @click="libraryStore.isScannerModalOpen = true">
              <template #icon>
                <FolderSearch class="w-4 h-4" />
              </template>
              <span>扫描目录</span>
            </NButton>
          </div>
        </template>
      </NEmpty>
    </div>

    <!-- Rows List -->
    <div v-else class="space-y-1">
      <div
        v-for="(item, index) in softwareList"
        :key="item.id"
        class="stagger-item group flex items-center justify-between p-2 rounded-lg border border-transparent hover:bg-[var(--n-hover-color)] transition-quick cursor-pointer select-none"
        :style="{ '--stagger-index': Math.min(index, 15) }"
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
              <span class="text-[13px] font-medium text-[var(--n-text-color)] group-hover:text-[var(--n-primary-color)] truncate transition-colors">
                {{ item.name }}
              </span>
              <span v-if="item.isRunning" class="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse"></span>
              <span v-if="item.isMissing" class="text-[10px] text-amber-500">缺失</span>
            </div>
            <div class="text-[11px] text-[var(--n-text-color)] opacity-60 truncate flex items-center gap-1 mt-0.5">
              <span>{{ item.categoryName || '默认分类' }}</span>
              <span>·</span>
              <span class="truncate font-mono" :title="'启动次数: ' + item.launchCount">{{ item.exePath }}</span>
            </div>
          </div>
        </div>

        <!-- Right: Actions -->
        <div class="flex items-center gap-1 shrink-0">
          <button
            v-if="item.isFavorite"
            @click.stop="libraryStore.toggleFavorite(item.id)"
            class="p-1.5 rounded-full hover:bg-[var(--n-border-color)] transition-colors cursor-pointer mr-1"
            title="取消收藏"
          >
            <Star class="w-4 h-4 text-amber-500 fill-amber-500" />
          </button>

          <div class="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
            <button
              v-if="!item.isFavorite"
              @click.stop="libraryStore.toggleFavorite(item.id)"
              class="p-1.5 rounded-full hover:bg-[var(--n-border-color)] transition-colors cursor-pointer"
              title="添加收藏"
            >
              <Star class="w-4 h-4 text-[var(--n-text-color)] opacity-40" />
            </button>
            <button
              @click.stop="libraryStore.openFolder(item.exePath)"
              class="p-1.5 rounded-full hover:bg-[var(--n-border-color)] transition-colors cursor-pointer"
              title="打开目录"
            >
              <FolderOpen class="w-4 h-4 text-[var(--n-text-color)] opacity-60" />
            </button>
            <button
              @click.stop="libraryStore.openEditModal(item)"
              class="p-1.5 rounded-full hover:bg-[var(--n-border-color)] transition-colors cursor-pointer"
              title="编辑应用"
            >
              <Edit3 class="w-4 h-4 text-[var(--n-text-color)] opacity-60" />
            </button>
            <button
              @click.stop="libraryStore.launchSoftware(item.id)"
              class="p-1.5 rounded-full hover:bg-black/5 dark:hover:bg-white/10 transition-colors cursor-pointer"
              title="启动应用"
            >
              <Play class="w-4 h-4 text-[var(--n-primary-color)] fill-[var(--n-primary-color)]" />
            </button>
          </div>
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
