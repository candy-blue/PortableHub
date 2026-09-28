<template>
  <NModal
    v-model:show="libraryStore.isCategoryModalOpen"
    preset="card"
    title="新建软件分类"
    style="width: 380px; max-width: 95vw;"
    :bordered="false"
    size="medium"
  >
    <div class="space-y-4">
      <div>
        <label class="block text-xs font-medium text-[var(--n-text-color)] opacity-80 mb-1.5">
          分类名称 <span class="text-rose-500">*</span>
        </label>
        <NInput
          v-model:value="name"
          placeholder="例如：设计工具、游戏娱乐..."
          clearable
          @keydown.enter="handleSubmit"
        />
      </div>

      <div>
        <label class="block text-xs font-medium text-[var(--n-text-color)] opacity-80 mb-1.5">
          标识色彩
        </label>
        <NColorPicker
          v-model:value="selectedColor"
          :swatches="colorPresets"
          :show-alpha="false"
        />
      </div>
    </div>

    <template #footer>
      <NSpace justify="end" :size="12">
        <NButton @click="closeModal">取消</NButton>
        <NButton
          type="primary"
          :disabled="!name.trim()"
          @click="handleSubmit"
        >
          创建分类
        </NButton>
      </NSpace>
    </template>
  </NModal>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { NModal, NInput, NColorPicker, NButton, NSpace, useMessage } from "naive-ui";
import { useLibraryStore } from "@/stores/library";

const libraryStore = useLibraryStore();
const message = useMessage();

const name = ref("");
const selectedColor = ref("#0078D4");

const colorPresets = [
  "#0078D4", // Blue
  "#107C41", // Green
  "#8B5CF6", // Purple
  "#F59E0B", // Amber
  "#EF4444", // Red
  "#06B6D4", // Cyan
  "#EC4899", // Pink
  "#64748B", // Slate
];

function closeModal() {
  libraryStore.isCategoryModalOpen = false;
  name.value = "";
  selectedColor.value = "#0078D4";
}

async function handleSubmit() {
  if (!name.value.trim()) return;
  const created = await libraryStore.addCategory({
    name: name.value.trim(),
    color: selectedColor.value,
    icon: "Folder",
  });
  if (created) {
    message.success(`已创建分类 "${created.name}"`);
  }
  closeModal();
}
</script>
