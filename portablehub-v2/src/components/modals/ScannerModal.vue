<template>
  <div
    v-if="libraryStore.isScannerModalOpen"
    class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs animate-fade-in"
    @click.self="closeModal"
  >
    <div
      class="w-full max-w-xl rounded-xl border border-[var(--border-strong)] bg-[var(--bg-surface)] shadow-2xl overflow-hidden flex flex-col text-[var(--text-primary)]"
      @keydown.esc="closeModal"
    >
      <!-- Modal Header -->
      <div class="h-12 px-5 flex items-center justify-between border-b border-[var(--border-subtle)] bg-[var(--bg-surface-elevated)]">
        <div class="flex items-center gap-2">
          <div class="w-6 h-6 rounded-md bg-[var(--accent-primary-subtle)] text-[var(--accent-primary)] flex items-center justify-center">
            <FolderSearch class="w-4 h-4" />
          </div>
          <h3 class="font-semibold text-sm">目录智能扫描导入</h3>
        </div>
        <button
          @click="closeModal"
          class="w-7 h-7 rounded-md flex items-center justify-center text-[var(--text-muted)] hover:text-[var(--text-primary)] hover:bg-[var(--bg-card-hover)] transition-colors"
        >
          <X class="w-4 h-4" />
        </button>
      </div>

      <!-- Modal Body -->
      <div class="p-5 space-y-4 max-h-[75vh] overflow-y-auto text-xs">
        <!-- Scan Path & Category Inputs -->
        <div class="space-y-3 p-3.5 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-app)]">
          <div>
            <label class="block font-medium text-[var(--text-secondary)] mb-1">
              扫描目录绝对路径
            </label>
            <div class="flex items-center gap-2">
              <input
                v-model="scanPath"
                type="text"
                placeholder="例如：D:\PortableApps 或 D:\Tools"
                class="flex-1 h-8 px-2.5 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-card)] text-[var(--text-primary)] font-mono text-[11px] focus:border-[var(--accent-primary)] outline-none"
              />
              <button
                @click="startScan"
                :disabled="isScanning || !scanPath"
                class="h-8 px-4 rounded-lg bg-[var(--accent-primary)] hover:bg-[var(--accent-primary-hover)] disabled:opacity-50 text-white font-medium flex items-center gap-1.5 shadow-xs transition-colors shrink-0 cursor-pointer"
              >
                <RefreshCw class="w-3.5 h-3.5" :class="{ 'animate-spin': isScanning }" />
                <span>{{ isScanning ? '扫描中...' : '开始扫描' }}</span>
              </button>
            </div>
          </div>

          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <span class="text-[var(--text-secondary)]">默认归属分类:</span>
              <select
                v-model="defaultCatId"
                class="h-7 px-2 rounded-md border border-[var(--border-subtle)] bg-[var(--bg-card)] text-[var(--text-primary)] outline-none cursor-pointer"
              >
                <option v-for="cat in libraryStore.categories" :key="cat.id" :value="cat.id">
                  {{ cat.name }}
                </option>
              </select>
            </div>

            <div v-if="candidates.length > 0" class="flex items-center gap-2">
              <button
                @click="selectAll(true)"
                class="text-[11px] text-[var(--accent-primary)] hover:underline"
              >
                全选
              </button>
              <span class="text-[var(--text-muted)]">|</span>
              <button
                @click="selectAll(false)"
                class="text-[11px] text-[var(--text-muted)] hover:underline"
              >
                清空
              </button>
            </div>
          </div>
        </div>

        <!-- Scan Candidate Results -->
        <div v-if="hasScanned">
          <div class="flex items-center justify-between mb-2">
            <span class="font-semibold text-[var(--text-secondary)]">
              发现应用候选 ({{ selectedCount }} / {{ candidates.length }})
            </span>
          </div>

          <div v-if="candidates.length === 0" class="py-8 text-center text-[var(--text-muted)]">
            未在指定目录及其子目录中发现便携可执行程序。
          </div>

          <div v-else class="space-y-1.5 max-h-60 overflow-y-auto pr-1">
            <div
              v-for="item in candidates"
              :key="item.exePath"
              @click="item.selected = !item.selected"
              class="flex items-center justify-between p-2 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-card)] hover:border-[var(--accent-primary)] cursor-pointer transition-colors"
            >
              <div class="flex items-center gap-2.5 min-w-0 flex-1">
                <input
                  type="checkbox"
                  v-model="item.selected"
                  @click.stop
                  class="w-3.5 h-3.5 rounded text-[var(--accent-primary)] border-[var(--border-strong)]"
                />
                <div class="min-w-0 flex-1">
                  <div class="font-medium text-[var(--text-primary)] truncate">
                    {{ item.name }}
                  </div>
                  <div class="text-[10px] text-[var(--text-muted)] font-mono truncate">
                    {{ item.exePath }}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Modal Footer -->
      <div class="h-12 px-5 flex items-center justify-between border-t border-[var(--border-subtle)] bg-[var(--bg-surface-elevated)]">
        <span class="text-[11px] text-[var(--text-muted)]">
          自动过滤卸载向导与冗余运行时
        </span>
        <div class="flex items-center gap-2">
          <button
            @click="closeModal"
            class="h-8 px-3.5 rounded-lg border border-[var(--border-subtle)] hover:bg-[var(--bg-card-hover)] text-xs font-medium transition-colors"
          >
            取消
          </button>
          <button
            @click="handleImport"
            :disabled="selectedCount === 0 || isImporting"
            class="h-8 px-4 rounded-lg bg-[var(--accent-primary)] hover:bg-[var(--accent-primary-hover)] disabled:opacity-50 disabled:cursor-not-allowed text-white text-xs font-medium transition-colors shadow-xs"
          >
            {{ isImporting ? '导入中...' : `一键导入 (${selectedCount})` }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from "vue";
import { X, FolderSearch, RefreshCw } from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";
import type { SoftwareScanCandidate } from "@/types";

const libraryStore = useLibraryStore();

const scanPath = ref("D:\\PortableApps");
const defaultCatId = ref(1);
const isScanning = ref(false);
const isImporting = ref(false);
const hasScanned = ref(false);
const candidates = ref<SoftwareScanCandidate[]>([]);

const selectedCount = computed(() => candidates.value.filter(c => c.selected).length);

function closeModal() {
  libraryStore.isScannerModalOpen = false;
  candidates.value = [];
  hasScanned.value = false;
}

function selectAll(val: boolean) {
  for (const item of candidates.value) {
    item.selected = val;
  }
}

async function startScan() {
  if (!scanPath.value.trim()) return;
  isScanning.value = true;
  hasScanned.value = true;
  try {
    const list = await libraryStore.scanDirectory(scanPath.value.trim(), defaultCatId.value);
    candidates.value = list.map(item => ({ ...item, selected: true }));
  } finally {
    isScanning.value = false;
  }
}

async function handleImport() {
  const selected = candidates.value.filter(c => c.selected);
  if (selected.length === 0) return;

  isImporting.value = true;
  try {
    await libraryStore.batchAddSoftware(selected);
    closeModal();
  } finally {
    isImporting.value = false;
  }
}
</script>
