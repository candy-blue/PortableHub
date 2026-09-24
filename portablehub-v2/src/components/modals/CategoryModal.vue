<template>
  <div
    v-if="libraryStore.isCategoryModalOpen"
    class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs animate-fade-in"
    @click.self="closeModal"
  >
    <div
      class="w-full max-w-sm rounded-xl border border-[var(--border-strong)] bg-[var(--bg-surface)] shadow-2xl overflow-hidden flex flex-col text-[var(--text-primary)]"
      @keydown.esc="closeModal"
    >
      <div class="h-12 px-5 flex items-center justify-between border-b border-[var(--border-subtle)] bg-[var(--bg-surface-elevated)]">
        <div class="flex items-center gap-2">
          <div class="w-6 h-6 rounded-md bg-[var(--accent-primary-subtle)] text-[var(--accent-primary)] flex items-center justify-center">
            <FolderPlus class="w-4 h-4" />
          </div>
          <h3 class="font-semibold text-sm">新建软件分类</h3>
        </div>
        <button
          @click="closeModal"
          class="w-7 h-7 rounded-md flex items-center justify-center text-[var(--text-muted)] hover:text-[var(--text-primary)] hover:bg-[var(--bg-card-hover)] transition-colors"
        >
          <X class="w-4 h-4" />
        </button>
      </div>

      <div class="p-5 space-y-4 text-xs">
        <div>
          <label class="block font-medium text-[var(--text-secondary)] mb-1">
            分类名称 <span class="text-rose-500">*</span>
          </label>
          <input
            v-model="name"
            type="text"
            placeholder="例如：游戏娱乐、设计工具..."
            class="w-full h-8 px-2.5 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-card)] text-[var(--text-primary)] focus:border-[var(--accent-primary)] outline-none"
            @keydown.enter="handleSubmit"
          />
        </div>

        <div>
          <label class="block font-medium text-[var(--text-secondary)] mb-2">
            标识色彩
          </label>
          <div class="flex items-center gap-2">
            <button
              v-for="c in colorPresets"
              :key="c"
              @click="selectedColor = c"
              class="w-6 h-6 rounded-full transition-transform cursor-pointer"
              :class="{ 'ring-2 ring-offset-2 ring-[var(--accent-primary)] scale-110': selectedColor === c }"
              :style="{ backgroundColor: c }"
            ></button>
          </div>
        </div>
      </div>

      <div class="h-12 px-5 flex items-center justify-end gap-2 border-t border-[var(--border-subtle)] bg-[var(--bg-surface-elevated)]">
        <button
          @click="closeModal"
          class="h-8 px-3.5 rounded-lg border border-[var(--border-subtle)] hover:bg-[var(--bg-card-hover)] text-xs font-medium transition-colors"
        >
          取消
        </button>
        <button
          @click="handleSubmit"
          :disabled="!name.trim()"
          class="h-8 px-4 rounded-lg bg-[var(--accent-primary)] hover:bg-[var(--accent-primary-hover)] disabled:opacity-50 text-white text-xs font-medium transition-colors shadow-xs cursor-pointer"
        >
          创建分类
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { X, FolderPlus } from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";

const libraryStore = useLibraryStore();

const name = ref("");
const selectedColor = ref("#3b82f6");

const colorPresets = [
  "#3b82f6", // Blue
  "#10b981", // Green
  "#8b5cf6", // Purple
  "#f59e0b", // Amber
  "#ef4444", // Red
  "#06b6d4", // Cyan
  "#ec4899", // Pink
  "#64748b", // Slate
];

function closeModal() {
  libraryStore.isCategoryModalOpen = false;
  name.value = "";
  selectedColor.value = "#3b82f6";
}

async function handleSubmit() {
  if (!name.value.trim()) return;
  await libraryStore.addCategory({
    name: name.value.trim(),
    color: selectedColor.value,
    icon: "Folder",
  });
  closeModal();
}
</script>
