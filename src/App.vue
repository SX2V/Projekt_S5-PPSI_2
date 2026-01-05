<script setup lang="ts">
import { onMounted } from 'vue';
import { useThemeStore } from './stores/theme';
import ToastContainer from './components/ToastContainer.vue';

const themeStore = useThemeStore();

onMounted(() => {
  themeStore.initTheme();
});
</script>

<template>
  <ToastContainer />
  <router-view />
</template>
