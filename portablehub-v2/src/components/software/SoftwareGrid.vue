<template>
  <div class="h-full">
    <!-- Empty State -->
    <div v-if="softwareList.length === 0" class="h-full flex items-center justify-center">
      <NEmpty
        class="h-72 flex flex-col items-center justify-center p-8 rounded-xl border border-dashed border-[var(--n-border-color)] bg-[var(--n-color-modal)] opacity-80"
        :description="libraryStore.searchQuery ? '未找到匹配软件' : '暂无便携软件'"
        size="large"
      >
        <template #extra>
          <div class="flex items-center gap-3 mt-2">
            <NButton type="primary" size="small" @click="libraryStore.openAddModal">
              <template #icon>
                <Plus class="w-3.5 h-3.5" />
              </template>
              <span>添加应用</span>
            </NButton>

            <NButton secondary size="small" @click="libraryStore.isScannerModalOpen = true">
              <template #icon>
                <FolderSearch class="w-3.5 h-3.5" />
              </template>
              <span>扫描导入</span>
            </NButton>
          </div>
        </template>
      </NEmpty>
    </div>

    <!-- Cards Grid -->
    <div v-else class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5 gap-4">
      <SoftwareCard
        v-for="(item, index) in softwareList"
        :key="item.id"
        :software="item"
        class="stagger-item shadow-sm hover:shadow-md transition-shadow"
        :style="{ '--stagger-index': Math.min(index, 20) }"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { NEmpty, NButton } from "naive-ui";
import { Plus, FolderSearch } from "@lucide/vue";
import type { Software } from "@/types";
import SoftwareCard from "./SoftwareCard.vue";
import { useLibraryStore } from "@/stores/library";

defineProps<{
  softwareList: Software[];
}>();

const libraryStore = useLibraryStore();
</script>
