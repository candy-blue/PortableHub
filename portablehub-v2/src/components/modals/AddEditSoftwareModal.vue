<template>
  <div
    v-if="libraryStore.isAddEditModalOpen"
    class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs animate-fade-in"
    @click.self="closeModal"
  >
    <div
      class="w-full max-w-lg rounded-xl border border-[var(--border-strong)] bg-[var(--bg-surface)] shadow-2xl overflow-hidden flex flex-col text-[var(--text-primary)]"
      @keydown.esc="closeModal"
    >
      <!-- Modal Header -->
      <div class="h-12 px-5 flex items-center justify-between border-b border-[var(--border-subtle)] bg-[var(--bg-surface-elevated)]">
        <div class="flex items-center gap-2">
          <div class="w-6 h-6 rounded-md bg-[var(--accent-primary-subtle)] text-[var(--accent-primary)] flex items-center justify-center">
            <AppWindow class="w-4 h-4" />
          </div>
          <h3 class="font-semibold text-sm">
            {{ isEditing ? '编辑便携应用' : '添加便携应用' }}
          </h3>
        </div>
        <button
          @click="closeModal"
          class="w-7 h-7 rounded-md flex items-center justify-center text-[var(--text-muted)] hover:text-[var(--text-primary)] hover:bg-[var(--bg-card-hover)] transition-colors"
        >
          <X class="w-4 h-4" />
        </button>
      </div>

      <!-- Modal Body (Form) -->
      <div class="p-5 space-y-4 max-h-[75vh] overflow-y-auto text-xs">
        <!-- Name & Category -->
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block font-medium text-[var(--text-secondary)] mb-1">
              应用名称 <span class="text-rose-500">*</span>
            </label>
            <input
              v-model="form.name"
              type="text"
              placeholder="例如：Visual Studio Code"
              class="w-full h-8 px-2.5 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-card)] text-[var(--text-primary)] focus:border-[var(--accent-primary)] outline-none transition-colors"
            />
          </div>

          <div>
            <label class="block font-medium text-[var(--text-secondary)] mb-1">
              所属分类 <span class="text-rose-500">*</span>
            </label>
            <select
              v-model="form.categoryId"
              class="w-full h-8 px-2 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-card)] text-[var(--text-primary)] focus:border-[var(--accent-primary)] outline-none transition-colors cursor-pointer"
            >
              <option v-for="cat in libraryStore.categories" :key="cat.id" :value="cat.id">
                {{ cat.name }}
              </option>
            </select>
          </div>
        </div>

        <!-- Exe Path -->
        <div>
          <label class="block font-medium text-[var(--text-secondary)] mb-1">
            可执行文件路径 (.exe) <span class="text-rose-500">*</span>
          </label>
          <div class="flex items-center gap-2">
            <input
              v-model="form.exePath"
              type="text"
              placeholder="D:\PortableApps\VSCode\Code.exe"
              class="flex-1 h-8 px-2.5 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-card)] text-[var(--text-primary)] font-mono text-[11px] focus:border-[var(--accent-primary)] outline-none transition-colors"
            />
          </div>
          <p class="text-[10px] text-[var(--text-muted)] mt-1">
            建议使用相对路径或将软件放置于 PortableHub 同级或同盘符目录。
          </p>
        </div>

        <!-- Description -->
        <div>
          <label class="block font-medium text-[var(--text-secondary)] mb-1">
            软件描述
          </label>
          <input
            v-model="form.description"
            type="text"
            placeholder="简要功能说明..."
            class="w-full h-8 px-2.5 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-card)] text-[var(--text-primary)] focus:border-[var(--accent-primary)] outline-none transition-colors"
          />
        </div>

        <!-- Arguments & Working Directory -->
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block font-medium text-[var(--text-secondary)] mb-1">
              启动参数 (可选)
            </label>
            <input
              v-model="form.arguments"
              type="text"
              placeholder="例如：--portable"
              class="w-full h-8 px-2.5 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-card)] text-[var(--text-primary)] font-mono text-[11px] focus:border-[var(--accent-primary)] outline-none transition-colors"
            />
          </div>

          <div>
            <label class="block font-medium text-[var(--text-secondary)] mb-1">
              工作目录 (可选)
            </label>
            <input
              v-model="form.workingDirectory"
              type="text"
              placeholder="留空默认程序所在目录"
              class="w-full h-8 px-2.5 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-card)] text-[var(--text-primary)] font-mono text-[11px] focus:border-[var(--accent-primary)] outline-none transition-colors"
            />
          </div>
        </div>

        <!-- Tags -->
        <div>
          <label class="block font-medium text-[var(--text-secondary)] mb-1">
            搜索标签 (用逗号分隔)
          </label>
          <input
            v-model="form.tags"
            type="text"
            placeholder="Editor, 开发, IDE"
            class="w-full h-8 px-2.5 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-card)] text-[var(--text-primary)] focus:border-[var(--accent-primary)] outline-none transition-colors"
          />
        </div>

        <!-- Checkboxes -->
        <div class="pt-2 border-t border-[var(--border-subtle)] flex items-center gap-6">
          <label class="flex items-center gap-2 cursor-pointer select-none">
            <input
              v-model="form.runAsAdmin"
              type="checkbox"
              class="w-4 h-4 rounded text-[var(--accent-primary)] border-[var(--border-strong)] focus:ring-0 cursor-pointer"
            />
            <span class="font-medium">以管理员身份运行</span>
          </label>

          <label class="flex items-center gap-2 cursor-pointer select-none">
            <input
              v-model="form.singleInstance"
              type="checkbox"
              class="w-4 h-4 rounded text-[var(--accent-primary)] border-[var(--border-strong)] focus:ring-0 cursor-pointer"
            />
            <span class="font-medium">限制单实例运行</span>
          </label>

          <label class="flex items-center gap-2 cursor-pointer select-none">
            <input
              v-model="form.isFavorite"
              type="checkbox"
              class="w-4 h-4 rounded text-[var(--accent-primary)] border-[var(--border-strong)] focus:ring-0 cursor-pointer"
            />
            <span class="font-medium">加入收藏</span>
          </label>
        </div>
      </div>

      <!-- Modal Footer -->
      <div class="h-12 px-5 flex items-center justify-end gap-2 border-t border-[var(--border-subtle)] bg-[var(--bg-surface-elevated)]">
        <button
          @click="closeModal"
          class="h-8 px-3.5 rounded-lg border border-[var(--border-subtle)] hover:bg-[var(--bg-card-hover)] text-xs font-medium transition-colors"
        >
          取消
        </button>
        <button
          @click="handleSubmit"
          :disabled="!form.name || !form.exePath"
          class="h-8 px-4 rounded-lg bg-[var(--accent-primary)] hover:bg-[var(--accent-primary-hover)] disabled:opacity-50 disabled:cursor-not-allowed text-white text-xs font-medium transition-colors shadow-xs"
        >
          {{ isEditing ? '保存修改' : '立即添加' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue";
import { X, AppWindow } from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";
import type { Software } from "@/types";

const libraryStore = useLibraryStore();

const isEditing = computed(() => !!libraryStore.editingSoftware);

const form = ref({
  name: "",
  exePath: "",
  categoryId: 1,
  description: "",
  arguments: "",
  workingDirectory: "",
  tags: "",
  runAsAdmin: false,
  singleInstance: true,
  isFavorite: false,
});

watch(
  () => libraryStore.isAddEditModalOpen,
  (open) => {
    if (open) {
      if (libraryStore.editingSoftware) {
        const item = libraryStore.editingSoftware;
        form.value = {
          name: item.name,
          exePath: item.exePath,
          categoryId: item.categoryId || (libraryStore.categories[0]?.id || 1),
          description: item.description || "",
          arguments: item.arguments || "",
          workingDirectory: item.workingDirectory || "",
          tags: item.tags || "",
          runAsAdmin: item.runAsAdmin,
          singleInstance: item.singleInstance,
          isFavorite: item.isFavorite,
        };
      } else {
        form.value = {
          name: "",
          exePath: "",
          categoryId: libraryStore.selectedCategoryId || (libraryStore.categories[0]?.id || 1),
          description: "",
          arguments: "",
          workingDirectory: "",
          tags: "",
          runAsAdmin: false,
          singleInstance: true,
          isFavorite: false,
        };
      }
    }
  }
);

function closeModal() {
  libraryStore.isAddEditModalOpen = false;
  libraryStore.editingSoftware = null;
}

async function handleSubmit() {
  if (!form.value.name.trim() || !form.value.exePath.trim()) return;

  if (isEditing.value && libraryStore.editingSoftware) {
    const updated: Software = {
      ...libraryStore.editingSoftware,
      name: form.value.name.trim(),
      exePath: form.value.exePath.trim(),
      categoryId: form.value.categoryId,
      categoryName: libraryStore.categories.find(c => c.id === form.value.categoryId)?.name || "",
      description: form.value.description.trim() || null,
      arguments: form.value.arguments.trim() || null,
      workingDirectory: form.value.workingDirectory.trim() || null,
      tags: form.value.tags.trim() || null,
      runAsAdmin: form.value.runAsAdmin,
      singleInstance: form.value.singleInstance,
      isFavorite: form.value.isFavorite,
    };
    await libraryStore.updateSoftware(updated);
  } else {
    await libraryStore.addSoftware({
      name: form.value.name.trim(),
      exePath: form.value.exePath.trim(),
      categoryId: form.value.categoryId,
      description: form.value.description.trim() || null,
      arguments: form.value.arguments.trim() || null,
      workingDirectory: form.value.workingDirectory.trim() || null,
      tags: form.value.tags.trim() || null,
      runAsAdmin: form.value.runAsAdmin,
      singleInstance: form.value.singleInstance,
      isFavorite: form.value.isFavorite,
    });
  }

  closeModal();
}
</script>
