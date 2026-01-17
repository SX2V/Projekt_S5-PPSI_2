<script setup lang="ts">
import { ref, onMounted } from 'vue';
import apiClient from '../api/axios';
import { useToastStore } from '../stores/toast';
import { useI18n } from 'vue-i18n';

const logs = ref<any[]>([]);
const isLoading = ref(false);
const toast = useToastStore();
const { t } = useI18n();

const fetchLogs = async () => {
  isLoading.value = true;
  try {
    const response = await apiClient.get<any[]>('/admin/logs');
    logs.value = response.data;
  } catch (error) {
    console.error('Failed to fetch logs', error);
    toast.error(t('admin.loadLogsFailed'));
  } finally {
    isLoading.value = false;
  }
};

const formatDate = (date: string) => {
    try {
        return new Date(date).toLocaleString();
    } catch {
        return date;
    }
};

onMounted(() => {
  fetchLogs();
});
</script>

<template>
  <div>
    <div class="flex justify-between items-center mb-6">
      <h2 class="text-2xl font-bold text-gray-900 dark:text-white">{{ t('admin.systemLogs') }}</h2>
      <button 
        @click="fetchLogs"
        class="inline-flex items-center px-3 py-1.5 border border-gray-300 dark:border-gray-600 shadow-sm text-sm font-medium rounded-md text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 hover:bg-gray-50 dark:hover:bg-gray-600 focus:outline-none"
      >
        <font-awesome-icon icon="sync" class="mr-2" />
        {{ t('common.refresh') }}
      </button>
    </div>

    <div class="bg-white dark:bg-gray-800 shadow overflow-hidden sm:rounded-lg transition-colors duration-200">
      <div v-if="isLoading" class="flex justify-center py-12">
        <font-awesome-icon icon="spinner" spin class="text-indigo-500 dark:text-indigo-400 text-3xl" />
      </div>

      <div v-else-if="logs.length === 0" class="text-center py-12 text-gray-500 dark:text-gray-400">
        <p>{{ t('admin.loadLogsFailed') }}</p>
      </div>

      <div v-else class="flex flex-col">
        <div class="-my-2 overflow-x-auto sm:-mx-6 lg:-mx-8">
          <div class="py-2 align-middle inline-block min-w-full sm:px-6 lg:px-8">
            <div class="shadow overflow-hidden border-b border-gray-200 dark:border-gray-700 sm:rounded-lg">
              <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead class="bg-gray-50 dark:bg-gray-700">
                  <tr>
                    <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                      {{ t('admin.logEntry') }}
                    </th>
                  </tr>
                </thead>
                <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                  <tr v-for="(log, index) in logs" :key="index">
                    <td class="px-6 py-4 text-sm text-gray-500 dark:text-gray-400 font-mono whitespace-pre-wrap">
                      <!-- Display log content. If object, try to format nicely -->
                      {{ typeof log === 'object' ? JSON.stringify(log, null, 2) : log }}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
