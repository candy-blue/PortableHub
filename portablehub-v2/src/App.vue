<template>
  <NConfigProvider
    :theme="themeStore.isDark ? darkTheme : null"
    :theme-overrides="themeOverrides"
    :locale="zhCN"
    :date-locale="dateZhCN"
  >
    <NLoadingBarProvider>
      <NDialogProvider>
        <NNotificationProvider>
          <NMessageProvider>
            <AppShell />
          </NMessageProvider>
        </NNotificationProvider>
      </NDialogProvider>
    </NLoadingBarProvider>
  </NConfigProvider>
</template>

<script setup lang="ts">
import { onMounted, computed } from "vue";
import {
  NConfigProvider,
  NLoadingBarProvider,
  NDialogProvider,
  NNotificationProvider,
  NMessageProvider,
  darkTheme,
  zhCN,
  dateZhCN,
  type GlobalThemeOverrides,
} from "naive-ui";
import AppShell from "@/layouts/AppShell.vue";
import { useThemeStore } from "@/stores/theme";
import { useLibraryStore } from "@/stores/library";

const themeStore = useThemeStore();
const libraryStore = useLibraryStore();

const themeOverrides = computed<GlobalThemeOverrides>(() => ({
  common: {
    primaryColor: "#0078D4",
    primaryColorHover: "#187BD1",
    primaryColorPressed: "#006CBE",
    primaryColorSuppl: "#0078D4",
    infoColor: "#0078D4",
    successColor: "#107C41",
    warningColor: "#F59E0B",
    errorColor: "#C42B1C",
    borderRadius: "6px",
    borderRadiusSmall: "4px",
    fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, "Microsoft YaHei", sans-serif',
  },
  Card: {
    borderRadius: "8px",
  },
  Button: {
    borderRadiusMedium: "6px",
    borderRadiusSmall: "4px",
    borderRadiusTiny: "4px",
  },
  Input: {
    borderRadius: "6px",
  },
  Select: {
    peers: {
      InternalSelection: {
        borderRadius: "6px",
      },
    },
  },
  Modal: {
    borderRadius: "10px",
  },
  Dropdown: {
    borderRadius: "8px",
  },
}));

onMounted(() => {
  themeStore.initTheme();
  libraryStore.loadFromBackend();
});
</script>