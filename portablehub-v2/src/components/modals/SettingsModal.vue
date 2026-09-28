<template>
  <NModal
    v-model:show="libraryStore.isSettingsModalOpen"
    preset="card"
    title="偏好设置"
    style="width: 440px; max-width: 95vw;"
    :bordered="false"
    size="medium"
  >
    <div class="space-y-4 text-xs">
      <!-- Theme Setting -->
      <div>
        <label class="block font-medium text-[var(--text-secondary)] mb-2">
          界面外观主题
        </label>
        <NRadioGroup
          :value="themeStore.mode"
          name="theme-mode"
          size="small"
          class="w-full flex"
          @update:value="handleThemeChange"
        >
          <NRadioButton value="System" class="flex-1 text-center">
            跟随系统
          </NRadioButton>
          <NRadioButton value="Light" class="flex-1 text-center">
            浅色模式
          </NRadioButton>
          <NRadioButton value="Dark" class="flex-1 text-center">
            深色模式
          </NRadioButton>
        </NRadioGroup>
      </div>

      <!-- View Preference -->
      <div>
        <label class="block font-medium text-[var(--text-secondary)] mb-2">
          默认视图布局
        </label>
        <NRadioGroup
          v-model:value="libraryStore.viewMode"
          name="view-mode"
          size="small"
          class="w-full flex"
        >
          <NRadioButton value="Grid" class="flex-1 text-center">
            网格卡片布局
          </NRadioButton>
          <NRadioButton value="List" class="flex-1 text-center">
            紧凑行列表
          </NRadioButton>
        </NRadioGroup>
      </div>

      <!-- Portable Mode Info -->
      <div class="p-3.5 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-app)] space-y-1.5">
        <div class="flex items-center gap-2 font-medium text-[var(--text-primary)]">
          <ShieldCheck class="w-4 h-4 text-emerald-500" />
          <span>完全便携模式已生效</span>
        </div>
        <p class="text-[11px] text-[var(--text-muted)] leading-relaxed">
          SQLite 数据库及图标缓存全量存储于同级 <code class="px-1 py-0.5 rounded bg-[var(--bg-card)] font-mono text-[10px]">data/PortableHub.db</code>，不向 Windows 注册表写入任何残留。
        </p>
      </div>

      <!-- Version & Architecture -->
      <div class="pt-2 border-t border-[var(--border-subtle)] text-[11px] text-[var(--text-muted)] flex items-center justify-between">
        <span>PortableHub 2.0 (Tauri 2 + Naive UI + Rust)</span>
        <span class="font-mono">v2.0.0</span>
      </div>
    </div>

    <template #footer>
      <NSpace justify="end">
        <NButton type="primary" size="small" @click="closeModal">
          完成
        </NButton>
      </NSpace>
    </template>
  </NModal>
</template>

<script setup lang="ts">
import { NModal, NRadioGroup, NRadioButton, NButton, NSpace } from "naive-ui";
import { ShieldCheck } from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";
import { useThemeStore, type ThemeMode } from "@/stores/theme";

const libraryStore = useLibraryStore();
const themeStore = useThemeStore();

function handleThemeChange(val: string) {
  themeStore.applyTheme(val as ThemeMode);
}

function closeModal() {
  libraryStore.isSettingsModalOpen = false;
}
</script>
