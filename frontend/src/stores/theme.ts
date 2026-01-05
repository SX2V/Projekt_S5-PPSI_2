import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useThemeStore = defineStore('theme', () => {
  const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
  const isDark = ref(mediaQuery.matches);

  const applyTheme = (dark: boolean) => {
    if (dark) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  };

  const initTheme = () => {
    applyTheme(isDark.value);

    mediaQuery.addEventListener('change', (e) => {
      isDark.value = e.matches;
      applyTheme(e.matches);
    });
  };

  return {
    isDark,
    initTheme,
  };
});
