<template>
  <AppShell />
</template>

<script setup lang="ts">
import { onMounted } from "vue";
import AppShell from "@/layouts/AppShell.vue";
import { useThemeStore } from "@/stores/theme";
import { useLibraryStore } from "@/stores/library";

const themeStore = useThemeStore();
const libraryStore = useLibraryStore();

onMounted(() => {
  themeStore.initTheme();
  libraryStore.loadFromBackend();
});
</script>