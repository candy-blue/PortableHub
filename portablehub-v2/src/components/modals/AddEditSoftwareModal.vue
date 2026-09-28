<template>
  <NModal
    v-model:show="libraryStore.isAddEditModalOpen"
    preset="card"
    :title="isEditing ? '编辑便携应用' : '添加便携应用'"
    style="width: 520px; max-width: 95vw;"
    :bordered="false"
    size="medium"
    :on-after-leave="handleAfterLeave"
  >
    <NForm label-placement="top" size="small" class="space-y-1">
      <NGrid :cols="2" :x-gap="12">
        <NFormItemGi label="应用名称" required>
          <NInput
            v-model:value="form.name"
            placeholder="例如：Visual Studio Code"
            clearable
          />
        </NFormItemGi>

        <NFormItemGi label="所属分类" required>
          <NSelect
            v-model:value="form.categoryId"
            :options="categoryOptions"
          />
        </NFormItemGi>
      </NGrid>

      <NFormItem label="程序路径 (.exe)" required>
        <NInput
          v-model:value="form.exePath"
          placeholder="例如：D:\PortableApps\VSCode\Code.exe"
          clearable
        />
      </NFormItem>

      <NFormItem label="软件描述">
        <NInput
          v-model:value="form.description"
          placeholder="简要功能描述..."
          clearable
        />
      </NFormItem>

      <NGrid :cols="2" :x-gap="12">
        <NFormItemGi label="启动参数 (可选)">
          <NInput
            v-model:value="form.arguments"
            placeholder="例如：--portable"
            clearable
          />
        </NFormItemGi>

        <NFormItemGi label="工作目录 (可选)">
          <NInput
            v-model:value="form.workingDirectory"
            placeholder="留空默认程序所在目录"
            clearable
          />
        </NFormItemGi>
      </NGrid>

      <NFormItem label="快捷搜索标签 (可选)">
        <NInput
          v-model:value="form.tags"
          placeholder="例如：Editor, 开发, IDE"
          clearable
        />
      </NFormItem>

      <div class="pt-3 border-t border-[var(--border-subtle)] flex items-center justify-between">
        <div class="flex items-center gap-2">
          <NSwitch v-model:value="form.runAsAdmin" size="small" />
          <span class="text-xs">以管理员身份运行</span>
        </div>

        <div class="flex items-center gap-2">
          <NSwitch v-model:value="form.singleInstance" size="small" />
          <span class="text-xs">限制单实例运行</span>
        </div>

        <div class="flex items-center gap-2">
          <NSwitch v-model:value="form.isFavorite" size="small" />
          <span class="text-xs">添加至收藏</span>
        </div>
      </div>
    </NForm>

    <template #footer>
      <NSpace justify="end" :size="12">
        <NButton @click="closeModal">取消</NButton>
        <NButton
          type="primary"
          :disabled="!form.name.trim() || !form.exePath.trim()"
          @click="handleSubmit"
        >
          {{ isEditing ? '保存修改' : '立即添加' }}
        </NButton>
      </NSpace>
    </template>
  </NModal>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue";
import {
  NModal,
  NForm,
  NFormItem,
  NFormItemGi,
  NGrid,
  NInput,
  NSelect,
  NSwitch,
  NButton,
  NSpace,
  useMessage,
} from "naive-ui";
import { useLibraryStore } from "@/stores/library";
import type { Software } from "@/types";

const libraryStore = useLibraryStore();
const message = useMessage();

const isEditing = computed(() => !!libraryStore.editingSoftware);

const categoryOptions = computed(() =>
  libraryStore.categories.map(c => ({
    label: c.name,
    value: c.id,
  }))
);

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

function handleAfterLeave() {
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
    message.success(`已更新 "${updated.name}"`);
  } else {
    const created = await libraryStore.addSoftware({
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
    if (created) {
      message.success(`已添加应用 "${created.name}"`);
    }
  }

  closeModal();
}
</script>
