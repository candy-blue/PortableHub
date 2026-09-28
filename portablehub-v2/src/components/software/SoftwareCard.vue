<template>
  <NCard
    hoverable
    size="small"
    class="cursor-pointer group select-none transition-all duration-150 border-[var(--border-subtle)]"
    :class="{ 'opacity-60 border-dashed border-amber-500': software.isMissing }"
    @click="handleCardClick"
    @contextmenu.prevent="handleContextMenu"
  >
    <!-- Top Action Row: Running Indicator, Category Badge, Favorite Star, More Menu -->
    <div class="flex items-center justify-between gap-1 mb-2.5">
      <!-- Status Badges -->
      <div class="flex items-center gap-1.5 min-w-0">
        <span
          v-if="software.isRunning"
          class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[10px] font-medium bg-emerald-500/10 text-emerald-600 dark:text-emerald-400"
        >
          <span class="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse"></span>
          运行中
        </span>

        <NTag
          v-else-if="software.isMissing"
          size="tiny"
          type="warning"
          :bordered="false"
          round
        >
          <template #icon>
            <AlertCircle class="w-3 h-3" />
          </template>
          路径缺失
        </NTag>

        <NTag
          v-else
          size="tiny"
          :bordered="false"
          round
          type="default"
          class="text-[10px] truncate max-w-[90px]"
        >
          {{ software.categoryName || '默认' }}
        </NTag>

        <Shield
          v-if="software.runAsAdmin"
          class="w-3 h-3 text-[var(--accent-primary)] shrink-0"
          title="默认以管理员身份运行"
        />
      </div>

      <!-- Favorite & Dropdown Menu -->
      <div class="flex items-center gap-0.5" @click.stop>
        <NButton
          quaternary
          circle
          size="tiny"
          @click.stop="libraryStore.toggleFavorite(software.id)"
          :title="software.isFavorite ? '取消收藏' : '添加收藏'"
        >
          <template #icon>
            <Star
              class="w-3.5 h-3.5 transition-colors"
              :class="software.isFavorite ? 'text-amber-500 fill-amber-500' : 'text-[var(--text-muted)]'"
            />
          </template>
        </NButton>

        <NDropdown
          trigger="click"
          :options="dropdownOptions"
          @select="handleMenuSelect"
        >
          <NButton
            quaternary
            circle
            size="tiny"
            class="opacity-0 group-hover:opacity-100 transition-opacity"
            title="更多操作"
          >
            <template #icon>
              <MoreVertical class="w-3.5 h-3.5" />
            </template>
          </NButton>
        </NDropdown>
      </div>
    </div>

    <!-- Middle: App Icon + Name + Description -->
    <div class="flex items-start gap-3 my-1">
      <!-- App Icon -->
      <div class="w-10 h-10 rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white font-bold text-sm shadow-xs shrink-0 overflow-hidden">
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
      <span>启动 {{ software.launchCount }} 次</span>

      <!-- Quick Launch Hover Button -->
      <NButton
        type="primary"
        size="tiny"
        class="opacity-0 group-hover:opacity-100 transition-opacity"
        @click.stop="handleLaunch(false)"
      >
        <template #icon>
          <Play class="w-3 h-3 fill-current" />
        </template>
        <span>启动</span>
      </NButton>
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
  </NCard>
</template>

<script setup lang="ts">
import { ref, h, nextTick } from "vue";
import type { Software } from "@/types";
import {
  NCard,
  NTag,
  NButton,
  NDropdown,
  useDialog,
  useMessage,
  type DropdownOption,
} from "naive-ui";
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
