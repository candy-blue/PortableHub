<template>
  <div
    v-if="libraryStore.isCommandPaletteOpen"
    class="fixed inset-0 z-50 flex items-start justify-center pt-20 px-4 select-none"
  >
    <!-- Backdrop -->
    <div
      class="fixed inset-0 bg-[var(--bg-overlay)] backdrop-blur-xs animate-palette-backdrop transition-opacity"
      @click="close"
    ></div>

    <!-- Modal Box -->
    <div
      class="animate-palette-modal relative w-full max-w-xl rounded-2xl border border-[var(--n-border-color)] bg-[var(--n-color-modal)] shadow-xl overflow-hidden z-10 flex flex-col"
    >
      <!-- Search Input Area -->
      <div class="flex items-center gap-3 px-4 py-3.5 border-b border-[var(--n-border-color)] bg-transparent">
        <Search class="w-5 h-5 text-[var(--n-primary-color)] shrink-0" />
        <input
          ref="searchInputRef"
          v-model="query"
          type="text"
          placeholder="快速搜索便携软件、分类、拼音首字母..."
          class="w-full bg-transparent text-sm text-[var(--n-text-color)] placeholder:text-[var(--n-text-color)] placeholder:opacity-40 outline-none"
          @keydown.down.prevent="navigate(1)"
          @keydown.up.prevent="navigate(-1)"
          @keydown.enter.prevent="handleEnter"
          @keydown.esc.prevent="close"
        />
        <button
          v-if="query"
          @click="query = ''"
          class="text-[var(--n-text-color)] opacity-60 hover:opacity-100 p-1 rounded-md transition-opacity cursor-pointer"
        >
          <X class="w-4 h-4" />
        </button>
        <kbd class="px-1.5 py-0.5 rounded text-[10px] font-mono bg-transparent border border-[var(--n-border-color)] text-[var(--n-text-color)] opacity-60">ESC</kbd>
      </div>

      <!-- Results List -->
      <div class="max-h-80 overflow-y-auto p-2 space-y-0.5">
        <div
          v-if="results.length === 0"
          class="py-10 text-center text-xs text-[var(--n-text-color)] opacity-60"
        >
          未找到相关软件
        </div>

        <div
          v-for="(item, index) in results"
          :key="item.id"
          @click="selectAndLaunch(item)"
          @mouseenter="selectedIndex = index"
          class="flex items-center justify-between p-2.5 rounded-xl cursor-pointer transition-colors"
          :class="selectedIndex === index
            ? 'bg-[var(--n-primary-color)] bg-opacity-10 dark:bg-opacity-20'
            : 'hover:bg-black/5 dark:hover:bg-white/10 text-[var(--n-text-color)]'"
        >
          <!-- Left: App Icon + Name + Category -->
          <div class="flex items-center gap-3 min-w-0 flex-1">
            <div
              class="w-8 h-8 rounded-lg flex items-center justify-center font-bold text-xs shrink-0 overflow-hidden bg-gradient-to-br from-blue-500 to-indigo-600 text-white"
            >
              <img v-if="item.iconPath" :src="item.iconPath" :alt="item.name" class="w-full h-full object-cover" />
              <span v-else>{{ item.name.slice(0, 1).toUpperCase() }}</span>
            </div>

            <div class="min-w-0 flex-1">
              <div class="flex items-center gap-2">
                <span 
                  class="text-xs font-medium truncate"
                  :class="selectedIndex === index ? 'text-[var(--n-primary-color)]' : 'text-[var(--n-text-color)]'"
                >{{ item.name }}</span>
                <span
                  v-if="item.isFavorite"
                  class="text-[10px]"
                  :class="selectedIndex === index ? 'text-amber-500 opacity-100' : 'text-amber-500 opacity-60'"
                >★</span>
                <span
                  v-if="item.isRunning"
                  class="w-1.5 h-1.5 rounded-full bg-emerald-500"
                ></span>
              </div>
              <p
                class="text-[11px] truncate mt-0.5 transition-colors"
                :class="selectedIndex === index ? 'text-[var(--n-primary-color)] opacity-80' : 'text-[var(--n-text-color)] opacity-50'"
              >
                {{ item.description || item.exePath }}
              </p>
            </div>
          </div>

          <!-- Right: Category Tag & Return Action -->
          <div class="flex items-center gap-3 pl-3 shrink-0">
            <span
              class="text-[10px] truncate"
              :class="selectedIndex === index ? 'text-[var(--n-primary-color)] opacity-80' : 'text-[var(--n-text-color)] opacity-40'"
            >
              {{ item.categoryName }}
            </span>
            <div
              v-if="selectedIndex === index"
              class="flex items-center gap-1 text-[10px] text-[var(--n-primary-color)] font-medium"
            >
              <CornerDownLeft class="w-3 h-3" />
              <span>启动</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Footer Hints -->
      <div class="px-4 py-2 bg-transparent border-t border-[var(--n-border-color)] flex items-center justify-between text-[11px] text-[var(--n-text-color)] opacity-60">
        <div class="flex items-center gap-3">
          <span class="flex items-center gap-1"><kbd class="px-1 py-0.5 rounded font-mono bg-transparent text-[10px]">↑↓</kbd> 选择</span>
          <span class="flex items-center gap-1"><kbd class="px-1 py-0.5 rounded font-mono bg-transparent text-[10px]">↵</kbd> 启动</span>
          <span class="flex items-center gap-1"><kbd class="px-1 py-0.5 rounded font-mono bg-transparent text-[10px]">Ctrl+↵</kbd> 管理员</span>
        </div>
        <span>共 {{ results.length }} 项</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick, onMounted, onUnmounted } from "vue";
import { Search, X, CornerDownLeft } from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";
import type { Software } from "@/types";
import PinyinMatch from "pinyin-match";

const libraryStore = useLibraryStore();
const query = ref("");
const selectedIndex = ref(0);
const searchInputRef = ref<HTMLInputElement | null>(null);

const results = computed(() => {
  const q = query.value.trim().toLowerCase();
  const all = libraryStore.softwareList;
  if (!q) {
    // Show favorites and frequently used first
    return [...all].sort((a, b) => {
      if (a.isFavorite !== b.isFavorite) return a.isFavorite ? -1 : 1;
      return b.launchCount - a.launchCount;
    });
  }

  return all.filter(s => {
    const nameMatch = s.name.toLowerCase().includes(q) || (PinyinMatch.match(s.name, q) !== false);
    const tagMatch = (s.tags || "").toLowerCase().includes(q);
    const descMatch = (s.description || "").toLowerCase().includes(q);
    const pathMatch = s.exePath.toLowerCase().includes(q);
    return nameMatch || tagMatch || descMatch || pathMatch;
  });
});

watch(() => libraryStore.isCommandPaletteOpen, (open) => {
  if (open) {
    query.value = "";
    selectedIndex.value = 0;
    nextTick(() => {
      searchInputRef.value?.focus();
    });
  }
});

watch(query, () => {
  selectedIndex.value = 0;
});

function close() {
  libraryStore.isCommandPaletteOpen = false;
}

function navigate(direction: number) {
  if (results.value.length === 0) return;
  const count = results.value.length;
  selectedIndex.value = (selectedIndex.value + direction + count) % count;
}

function handleEnter(e: KeyboardEvent) {
  if (results.value.length === 0) return;
  const target = results.value[selectedIndex.value];
  if (target) {
    selectAndLaunch(target, e.ctrlKey);
  }
}

function selectAndLaunch(software: Software, asAdmin = false) {
  libraryStore.launchSoftware(software.id, asAdmin);
  close();
}

function handleGlobalKeydown(e: KeyboardEvent) {
  if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
    e.preventDefault();
    libraryStore.isCommandPaletteOpen = !libraryStore.isCommandPaletteOpen;
  }
}

onMounted(() => {
  window.addEventListener("keydown", handleGlobalKeydown);
});

onUnmounted(() => {
  window.removeEventListener("keydown", handleGlobalKeydown);
});
</script>
