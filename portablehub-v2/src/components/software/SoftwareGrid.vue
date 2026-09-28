<template>
  <div class="h-full">
    <!-- Empty State -->
    <div v-if="softwareList.length === 0" class="h-full flex items-center justify-center">
      <NEmpty
        class="flex flex-col items-center justify-center p-8 rounded-xl bg-transparent"
        :description="libraryStore.searchQuery ? '未找到匹配软件' : '还没添加任何便携软件'"
        size="huge"
      >
        <template #extra>
          <div class="flex items-center gap-3 mt-4">
            <NButton type="primary" @click="libraryStore.openAddModal">
              <template #icon>
                <Plus class="w-4 h-4" />
              </template>
              <span>手动添加</span>
            </NButton>

            <NButton secondary @click="libraryStore.isScannerModalOpen = true">
              <template #icon>
                <FolderSearch class="w-4 h-4" />
              </template>
              <span>扫描目录</span>
            </NButton>
          </div>
        </template>
      </NEmpty>
    </div>

    <!-- Cards Grid -->
    <div v-else class="grid gap-3 grid-cols-[repeat(auto-fill,minmax(160px,1fr))]">
      <SoftwareCard
        v-for="(item, index) in softwareList"
        :key="item.id"
        :software="item"
        class="stagger-item"
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
