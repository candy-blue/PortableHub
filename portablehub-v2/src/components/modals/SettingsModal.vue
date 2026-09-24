<template>
  <div
    v-if="libraryStore.isSettingsModalOpen"
    class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs animate-fade-in"
    @click.self="closeModal"
  >
    <div
      class="w-full max-w-md rounded-xl border border-[var(--border-strong)] bg-[var(--bg-surface)] shadow-2xl overflow-hidden flex flex-col text-[var(--text-primary)]"
      @keydown.esc="closeModal"
    >
      <div class="h-12 px-5 flex items-center justify-between border-b border-[var(--border-subtle)] bg-[var(--bg-surface-elevated)]">
        <div class="flex items-center gap-2">
          <div class="w-6 h-6 rounded-md bg-[var(--accent-primary-subtle)] text-[var(--accent-primary)] flex items-center justify-center">
            <Settings class="w-4 h-4" />
          </div>
          <h3 class="font-semibold text-sm">偏好设置</h3>
        </div>
        <button
          @click="closeModal"
          class="w-7 h-7 rounded-md flex items-center justify-center text-[var(--text-muted)] hover:text-[var(--text-primary)] hover:bg-[var(--bg-card-hover)] transition-colors"
        >
          <X class="w-4 h-4" />
        </button>
      </div>

      <div class="p-5 space-y-5 text-xs">
        <!-- Theme Setting -->
        <div>
          <label class="block font-medium text-[var(--text-secondary)] mb-2">
            界面外观主题
          </label>
          <div class="grid grid-cols-3 gap-2">
            <button
              v-for="item in themeOptions"
              :key="item.value"
              @click="themeStore.applyTheme(item.value)"
              class="h-8 rounded-lg border flex items-center justify-center gap-1.5 transition-colors cursor-pointer"
              :class="themeStore.mode === item.value
                ? 'border-[var(--accent-primary)] bg-[var(--accent-primary-subtle)] text-[var(--accent-primary)] font-semibold'
                : 'border-[var(--border-subtle)] bg-[var(--bg-card)] text-[var(--text-secondary)] hover:border-[var(--border-strong)]'"
            >
              <component :is="item.icon" class="w-3.5 h-3.5" />
              <span>{{ item.label }}</span>
            </button>
          </div>
        </div>

        <!-- View Preferences -->
        <div>
          <label class="block font-medium text-[var(--text-secondary)] mb-2">
            默认布局模式
          </label>
          <div class="grid grid-cols-2 gap-2">
            <button
              @click="libraryStore.viewMode = 'Grid'"
              class="h-8 rounded-lg border flex items-center justify-center gap-2 cursor-pointer transition-colors"
              :class="libraryStore.viewMode === 'Grid'
                ? 'border-[var(--accent-primary)] bg-[var(--accent-primary-subtle)] text-[var(--accent-primary)] font-semibold'
                : 'border-[var(--border-subtle)] bg-[var(--bg-card)] text-[var(--text-secondary)]'"
            >
              <LayoutGrid class="w-3.5 h-3.5" />
              <span>网格卡片布局</span>
            </button>
            <button
              @click="libraryStore.viewMode = 'List'"
              class="h-8 rounded-lg border flex items-center justify-center gap-2 cursor-pointer transition-colors"
              :class="libraryStore.viewMode === 'List'
                ? 'border-[var(--accent-primary)] bg-[var(--accent-primary-subtle)] text-[var(--accent-primary)] font-semibold'
                : 'border-[var(--border-subtle)] bg-[var(--bg-card)] text-[var(--text-secondary)]'"
            >
              <List class="w-3.5 h-3.5" />
              <span>紧凑行列表</span>
            </button>
          </div>
        </div>

        <!-- Portable Mode Info -->
        <div class="p-3.5 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-app)] space-y-1.5">
          <div class="flex items-center gap-2 font-medium text-[var(--text-primary)]">
            <ShieldCheck class="w-4 h-4 text-emerald-500" />
            <span>便携模式已生效</span>
          </div>
          <p class="text-[11px] text-[var(--text-muted)] leading-relaxed">
            数据目录存储于同级 <code class="px-1 py-0.5 rounded bg-[var(--bg-card)] font-mono text-[10px]">data/PortableHub.db</code>，不向 Windows 注册表写入垃圾项。
          </p>
        </div>

        <!-- About Info -->
        <div class="pt-3 border-t border-[var(--border-subtle)] text-[11px] text-[var(--text-muted)] flex items-center justify-between">
          <span>PortableHub 2.0 (Tauri 2 + Rust + Vue 3)</span>
          <span class="font-mono">v2.0.0</span>
        </div>
      </div>

      <div class="h-12 px-5 flex items-center justify-end border-t border-[var(--border-subtle)] bg-[var(--bg-surface-elevated)]">
        <button
          @click="closeModal"
          class="h-8 px-4 rounded-lg bg-[var(--accent-primary)] hover:bg-[var(--accent-primary-hover)] text-white text-xs font-medium transition-colors cursor-pointer"
        >
          确定
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { X, Settings, Sun, Moon, Laptop, LayoutGrid, List, ShieldCheck } from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";
import { useThemeStore, type ThemeMode } from "@/stores/theme";

const libraryStore = useLibraryStore();
const themeStore = useThemeStore();

const themeOptions: { label: string; value: ThemeMode; icon: any }[] = [
  { label: "跟随系统", value: "System", icon: Laptop },
  { label: "浅色模式", value: "Light", icon: Sun },
  { label: "深色模式", value: "Dark", icon: Moon },
];

function closeModal() {
  libraryStore.isSettingsModalOpen = false;
}
</script>
