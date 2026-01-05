<script setup lang="ts">
import { useToastStore } from '../stores/toast';
import { storeToRefs } from 'pinia';

const toastStore = useToastStore();
const { toasts } = storeToRefs(toastStore);

const getToastClasses = (type: string) => {
  switch (type) {
    case 'success':
      return 'bg-green-500 text-white';
    case 'error':
      return 'bg-red-500 text-white';
    case 'warning':
      return 'bg-yellow-500 text-white';
    case 'info':
    default:
      return 'bg-blue-500 text-white';
  }
};
</script>

<template>
  <div class="fixed top-4 right-4 z-[9999] flex flex-col space-y-2 w-full max-w-xs pointer-events-none">
    <transition-group name="toast">
      <div
        v-for="toast in toasts"
        :key="toast.id"
        class="pointer-events-auto flex items-center justify-between px-4 py-3 rounded shadow-lg transform transition-all duration-300"
        :class="getToastClasses(toast.type)"
      >
        <span class="text-sm font-medium">{{ toast.message }}</span>
        <button @click="toastStore.remove(toast.id)" class="ml-4 focus:outline-none opacity-75 hover:opacity-100">
          <font-awesome-icon icon="times" />
        </button>
      </div>
    </transition-group>
  </div>
</template>

<style scoped>
.toast-enter-active,
.toast-leave-active {
  transition: all 0.3s ease;
}
.toast-enter-from {
  opacity: 0;
  transform: translateX(30px);
}
.toast-leave-to {
  opacity: 0;
  transform: translateX(30px);
}
</style>
