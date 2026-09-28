<template>
  <NModal
    v-model:show="libraryStore.isScannerModalOpen"
    preset="card"
    title="目录智能扫描导入"
    style="width: 580px; max-width: 95vw;"
    :bordered="false"
    size="medium"
    :on-after-leave="handleAfterLeave"
  >
    <div class="space-y-4">
      <!-- Input Group -->
      <div class="space-y-2 p-3 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-app)]">
        <label class="block text-xs font-medium text-[var(--text-secondary)]">
          扫描目录绝对路径
        </label>
        <NInputGroup>
          <NInput
            v-model:value="scanPath"
            placeholder="例如：D:\PortableApps 或 D:\Tools"
            clearable
          />
          <NButton
            type="primary"
            :loading="isScanning"
            :disabled="!scanPath.trim()"
            @click="startScan"
          >
            <template #icon>
              <RefreshCw class="w-3.5 h-3.5" :class="{ 'animate-spin': isScanning }" />
            </template>
            <span>开始扫描</span>
          </NButton>
        </NInputGroup>

        <div class="flex items-center justify-between pt-1">
          <div class="flex items-center gap-2">
            <span class="text-xs text-[var(--text-muted)]">默认分类:</span>
            <NSelect
              v-model:value="defaultCatId"
              :options="categoryOptions"
              size="small"
              style="width: 130px"
            />
          </div>

          <div v-if="candidates.length > 0" class="flex items-center gap-3">
            <NButton text type="primary" size="tiny" @click="selectAll(true)">全选</NButton>
            <NButton text size="tiny" @click="selectAll(false)">清空</NButton>
          </div>
        </div>
      </div>

      <!-- Candidate Results -->
      <div v-if="hasScanned">
        <div class="flex items-center justify-between mb-2">
          <span class="text-xs font-semibold text-[var(--text-secondary)]">
            发现便携应用候选 ({{ selectedCount }} / {{ candidates.length }})
          </span>
        </div>

        <NEmpty
          v-if="candidates.length === 0"
          description="未在指定目录中检索到便携可执行程序"
          size="small"
          class="py-6"
        />

        <NScrollbar v-else style="max-height: 260px;" class="pr-2">
          <div class="space-y-1.5">
            <div
              v-for="item in candidates"
              :key="item.exePath"
              class="flex items-center justify-between p-2 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-card)] hover:border-[var(--accent-primary)] cursor-pointer transition-colors"
              @click="item.selected = !item.selected"
            >
              <div class="flex items-center gap-2.5 min-w-0 flex-1">
                <NCheckbox
                  v-model:checked="item.selected"
                  @click.stop
                />
                <div class="min-w-0 flex-1">
                  <div class="text-xs font-medium text-[var(--text-primary)] truncate">
                    {{ item.name }}
                  </div>
                  <div class="text-[10px] text-[var(--text-muted)] font-mono truncate">
                    {{ item.exePath }}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </NScrollbar>
      </div>
    </div>

    <template #footer>
      <div class="flex items-center justify-between w-full">
        <span class="text-[11px] text-[var(--text-muted)]">
          自动过滤卸载向导与冗余运行时文件
        </span>
        <NSpace :size="12">
          <NButton @click="closeModal">取消</NButton>
          <NButton
            type="primary"
            :loading="isImporting"
            :disabled="selectedCount === 0"
            @click="handleImport"
          >
            一键导入 ({{ selectedCount }})
          </NButton>
        </NSpace>
      </div>
    </template>
  </NModal>
</template>

<script setup lang="ts">
import { ref, computed } from "vue";
import {
  NModal,
  NInputGroup,
  NInput,
  NButton,
  NSelect,
  NEmpty,
  NScrollbar,
  NCheckbox,
  NSpace,
  useMessage,
} from "naive-ui";
import { RefreshCw } from "@lucide/vue";
import { useLibraryStore } from "@/stores/library";
import type { SoftwareScanCandidate } from "@/types";

const libraryStore = useLibraryStore();
const message = useMessage();

const scanPath = ref("D:\\PortableApps");
const defaultCatId = ref(1);
const isScanning = ref(false);
const isImporting = ref(false);
const hasScanned = ref(false);
const candidates = ref<SoftwareScanCandidate[]>([]);

const selectedCount = computed(() => candidates.value.filter(c => c.selected).length);

const categoryOptions = computed(() =>
  libraryStore.categories.map(c => ({
    label: c.name,
    value: c.id,
  }))
);

function closeModal() {
  libraryStore.isScannerModalOpen = false;
  candidates.value = [];
  hasScanned.value = false;
}

function handleAfterLeave() {
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
    if (candidates.value.length === 0) {
      message.info("未发现可执行程序");
    } else {
      message.success(`扫描完成，发现 ${candidates.value.length} 个候选应用`);
    }
  } catch (err) {
    message.error("扫描失败，请检查路径权限");
  } finally {
    isScanning.value = false;
  }
}

async function handleImport() {
  const selected = candidates.value.filter(c => c.selected);
  if (selected.length === 0) return;

  isImporting.value = true;
  try {
    const count = await libraryStore.batchAddSoftware(selected);
    message.success(`成功导入 ${count} 个便携应用！`);
    closeModal();
  } catch {
    message.error("导入失败");
  } finally {
    isImporting.value = false;
  }
}
</script>
