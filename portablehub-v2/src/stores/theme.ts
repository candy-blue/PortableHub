import { defineStore } from "pinia";
import { ref } from "vue";

export type ThemeMode = "System" | "Light" | "Dark";

export const useThemeStore = defineStore("theme", () => {
  const mode = ref<ThemeMode>("System");
  const isDark = ref(false);

  function applyTheme(targetMode: ThemeMode) {
    mode.value = targetMode;
    const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");

    if (targetMode === "System") {
      isDark.value = mediaQuery.matches;
    } else {
      isDark.value = targetMode === "Dark";
    }

    if (isDark.value) {
      document.documentElement.setAttribute("data-theme", "dark");
    } else {
      document.documentElement.removeAttribute("data-theme");
    }
  }

  function initTheme() {
    applyTheme(mode.value);
    window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", (e) => {
      if (mode.value === "System") {
        isDark.value = e.matches;
        if (e.matches) {
          document.documentElement.setAttribute("data-theme", "dark");
        } else {
          document.documentElement.removeAttribute("data-theme");
        }
      }
    });
  }

  function toggleTheme() {
    const nextMode: ThemeMode = isDark.value ? "Light" : "Dark";
    applyTheme(nextMode);
  }

  return {
    mode,
    isDark,
    applyTheme,
    initTheme,
    toggleTheme,
  };
});
