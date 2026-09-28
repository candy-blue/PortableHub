<template>
  <div class="flex flex-col h-full bg-transparent">
    <!-- Top Nav Section -->
    <NScrollbar class="flex-1">
      <div class="py-2">
        <NMenu
          :collapsed="collapsed"
          :collapsed-width="64"
          :collapsed-icon-size="20"
          :options="menuOptions"
          v-model:value="activeKey"
        />
      </div>
    </NScrollbar>

    <!-- Bottom Actions Section -->
    <div class="p-2 border-t border-[var(--n-border-color)] space-y-1 shrink-0 flex flex-col items-center">
      <NButton
        quaternary
        class="w-full justify-start"
        :class="{ 'px-0 justify-center': collapsed }"
        @click="libraryStore.isScannerModalOpen = true"
        :title="collapsed ? '扫描目录' : ''"
      >
        <template #icon>
          <FolderSearch class="w-4 h-4 text-[var(--n-primary-color)]" />
        </template>
        <span v-if="!collapsed" class="text-xs">扫描导入</span>
      </NButton>

      <NButton
        quaternary
        class="w-full justify-start"
        :class="{ 'px-0 justify-center': collapsed }"
        @click="libraryStore.isSettingsModalOpen = true"
        :title="collapsed ? '偏好设置' : ''"
      >
        <template #icon>
          <Settings class="w-4 h-4" />
        </template>
        <span v-if="!collapsed" class="text-xs">偏好设置</span>
      </NButton>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, h } from "vue";
import { NBadge, NMenu, NScrollbar, NButton, type MenuOption } from "naive-ui";
import {
  Grid,
  Star,
  Clock,
  FolderSearch,
  Settings,
  Plus,
} from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";

const props = defineProps<{
  collapsed: boolean;
}>();

const libraryStore = useLibraryStore();

function renderIcon(icon: any, color?: string) {
  return () => h(icon, { class: "w-4 h-4", style: color ? { color } : {} });
}

function renderLabelWithBadge(label: string, count: number, showBadge: boolean) {
  return () => h("div", { class: "flex items-center justify-between w-full" }, [
    h("span", { class: "text-xs font-medium truncate" }, label),
    (showBadge && !props.collapsed) ? h(NBadge, { value: count, max: 999, type: 'info', showZero: true }) : null
  ]);
}

function renderCategoryDot(color: string | null | undefined) {
  return () => h("div", { class: "flex items-center justify-center w-full h-full" }, [
    h("div", {
      class: "w-2.5 h-2.5 rounded-full",
      style: { backgroundColor: color || '#0078D4' }
    })
  ]);
}

const menuOptions = computed<MenuOption[]>(() => {
  const options: MenuOption[] = [
    {
      label: renderLabelWithBadge("全部软件", libraryStore.softwareList.length, true),
      key: "nav-all",
      icon: renderIcon(Grid),
    },
    {
      label: renderLabelWithBadge("我的收藏", libraryStore.favoritesCount, true),
      key: "nav-favorites",
      icon: renderIcon(Star),
    },
    {
      label: renderLabelWithBadge("最近启动", libraryStore.recentCount, true),
      key: "nav-recent",
      icon: renderIcon(Clock),
    },
    {
      type: "divider",
      key: "d1"
    }
  ];

  if (!props.collapsed) {
    options.push({
      key: 'category-header',
      type: 'render',
      render: () => h("div", { class: "px-4 py-2 mt-2 flex items-center justify-between text-[11px] font-semibold text-[var(--n-text-color)] opacity-60" }, [
        h("span", "软件分类"),
        h(NButton, {
          quaternary: true,
          circle: true,
          size: "tiny",
          onClick: (e: MouseEvent) => {
            e.stopPropagation();
            libraryStore.isCategoryModalOpen = true;
          }
        }, { icon: () => h(Plus, { class: "w-3.5 h-3.5" }) })
      ])
    });
  }

  libraryStore.categories.forEach(cat => {
    options.push({
      label: renderLabelWithBadge(cat.name, libraryStore.categoryCounts.get(cat.id) || 0, true),
      key: `cat-${cat.id}`,
      icon: renderCategoryDot(cat.color)
    });
  });

  return options;
});

const activeKey = computed({
  get: () => {
    if (libraryStore.selectedNav === 'category') {
      return `cat-${libraryStore.selectedCategoryId}`;
    }
    return `nav-${libraryStore.selectedNav}`;
  },
  set: (val: string) => {
    if (val.startsWith('nav-')) {
      libraryStore.selectNav(val.replace('nav-', '') as any);
    } else if (val.startsWith('cat-')) {
      libraryStore.selectCategory(parseInt(val.replace('cat-', '')));
    }
  }
});
</script>

<style scoped>
:deep(.n-menu-item-content) {
  padding-right: 12px !important;
}
</style>
