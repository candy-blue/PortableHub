<template>
  <div>
    <!-- Empty State -->
    <div
      v-if="softwareList.length === 0"
      class="h-64 flex flex-col items-center justify-center text-center p-6 rounded-2xl border border-dashed border-[var(--border-subtle)]"
    >
      <div class="w-12 h-12 rounded-full bg-[var(--bg-card)] flex items-center justify-center text-[var(--text-muted)] mb-3 shadow-xs">
        <PackageOpen class="w-6 h-6" />
      </div>
      <h4 class="text-sm font-semibold text-[var(--text-primary)]">未找到匹配软件</h4>
      <p class="text-xs text-[var(--text-muted)] mt-1 max-w-xs">
        当前筛选或搜索条件下无软件。尝试清空搜索词，或点击下方按钮添加新应用。
      </p>
    </div>

    <!-- Grid Cards -->
    <div
      v-else
      class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5 gap-3"
    >
      <div
        v-for="(item, index) in softwareList"
        :key="item.id"
        class="stagger-item"
        :style="{ '--stagger-index': Math.min(index, 12) }"
      >
        <SoftwareCard :software="item" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { Software } from "@/types";
import { PackageOpen } from "@lucide/vue";
import SoftwareCard from "./SoftwareCard.vue";

defineProps<{
  softwareList: Software[];
}>();
</script>
