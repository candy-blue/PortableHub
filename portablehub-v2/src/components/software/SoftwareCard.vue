<template>
  <div
    class="cursor-pointer group relative flex flex-col h-[156px] rounded-xl bg-[var(--n-color)] border border-transparent hover:border-[var(--n-border-color)] transition-quick transform hover:-translate-y-[1px] p-4 select-none"
    :class="{ 'opacity-60 grayscale': software.isMissing }"
    @click="handleCardClick"
    @contextmenu.prevent="handleContextMenu"
  >
    <!-- Top Row: Icon & Actions -->
    <div class="flex items-start justify-between">
      <div class="w-12 h-12 rounded-xl flex items-center justify-center font-bold text-lg text-white shadow-sm overflow-hidden bg-gradient-to-br from-blue-500 to-indigo-600 shrink-0">
        <img
          v-if="software.iconPath"
          :src="software.iconPath"
          :alt="software.name"
          class="w-full h-full object-cover"
        />
        <span v-else>{{ software.name.slice(0, 1).toUpperCase() }}</span>
      </div>

      <div class="flex items-center gap-1">
        <!-- Always visible if favorited -->
        <button
          v-if="software.isFavorite"
          @click.stop="libraryStore.toggleFavorite(software.id)"
          class="p-1 rounded-full hover:bg-[var(--n-border-color)] transition-colors cursor-pointer"
          title="取消收藏"
        >
          <Star class="w-4 h-4 text-amber-500 fill-amber-500" />
        </button>
        
        <!-- Only visible on hover -->
        <div class="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
          <button
            v-if="!software.isFavorite"
            @click.stop="libraryStore.toggleFavorite(software.id)"
            class="p-1 rounded-full hover:bg-[var(--n-border-color)] transition-colors cursor-pointer"
            title="添加收藏"
          >
            <Star class="w-4 h-4 text-[var(--n-text-color)] opacity-40" />
          </button>

          <NDropdown trigger="click" :options="dropdownOptions" @select="handleMenuSelect">
            <button
              class="p-1 rounded-full hover:bg-[var(--n-border-color)] transition-colors cursor-pointer"
              title="更多操作"
              @click.stop
            >
              <MoreVertical class="w-4 h-4 text-[var(--n-text-color)] opacity-60" />
            </button>
          </NDropdown>
        </div>
      </div>
    </div>

    <!-- Spacer -->
    <div class="flex-1"></div>

    <!-- Bottom Row: Title, Category & Status -->
    <div class="relative min-w-0 pr-4">
      <h3 class="text-sm font-medium text-[var(--n-text-color)] truncate group-hover:text-[var(--n-primary-color)] transition-colors">
        {{ software.name }}
      </h3>
      <div class="flex items-center gap-1.5 mt-0.5">
        <span class="text-xs text-[var(--n-text-color)] opacity-50 truncate">
          {{ software.categoryName || '默认分类' }}
        </span>
        <Shield v-if="software.runAsAdmin" class="w-3 h-3 text-[var(--n-primary-color)] opacity-70 shrink-0" />
      </div>
      
      <!-- Running Dot Absolute -->
      <div
        v-if="software.isRunning"
        class="absolute right-0 bottom-1 w-2 h-2 rounded-full bg-emerald-500 animate-pulse shadow-[0_0_8px_rgba(16,185,129,0.5)]"
        title="运行中"
      ></div>
    </div>

    <!-- Missing Overlay -->
    <div v-if="software.isMissing" class="absolute inset-0 bg-[var(--n-color-modal)]/40 flex items-center justify-center rounded-xl backdrop-blur-[1px] pointer-events-none">
      <div class="bg-black/70 text-white text-[10px] px-2 py-1 rounded-md flex items-center gap-1">
        <AlertCircle class="w-3 h-3" />
        缺失
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
import { NDropdown, useDialog, useMessage, type DropdownOption } from "naive-ui";
import { Star, MoreVertical, Play, AlertCircle, Shield, FolderOpen, Copy, Edit3, Trash2 } from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";

const props = defineProps<{
  software: Software;
}>();

const libraryStore = useLibraryStore();
const dialog = useDialog();
const message = useMessage();

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

function handleCardClick() {
  handleLaunch(false);
}

function handleLaunch(forceAdmin: boolean) {
  showContextMenu.value = false;
  libraryStore.launchSoftware(props.software.id, forceAdmin);
}

function handleContextMenu(e: MouseEvent) {
  showContextMenu.value = false;
  nextTick(() => {
    contextX.value = e.clientX;
    contextY.value = e.clientY;
    showContextMenu.value = true;
  });
}

function handleMenuSelect(key: string | number) {
  showContextMenu.value = false;
  switch (key) {
    case "launch":
      handleLaunch(false);
      break;
    case "launch-admin":
      handleLaunch(true);
      break;
    case "open-folder":
      libraryStore.openFolder(props.software.exePath);
      break;
    case "copy-path":
      navigator.clipboard.writeText(props.software.exePath);
      message.success("程序路径已复制到剪贴板");
      break;
    case "edit":
      libraryStore.openEditModal(props.software);
      break;
    case "delete":
      dialog.warning({
        title: "从库中移除",
        content: `确定要从软件库中移除应用 "${props.software.name}" 吗？（注意：这不会删除您硬盘上的原始文件）`,
        positiveText: "确认移除",
        negativeText: "取消",
        onPositiveClick: () => {
          libraryStore.deleteSoftware(props.software.id);
          message.info(`已移除 "${props.software.name}"`);
        },
      });
      break;
  }
}
</script>
