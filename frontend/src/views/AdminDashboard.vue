<script setup lang="ts">
import { ref, onMounted } from 'vue';
import apiClient from '../api/axios';
import type { TrainingRequestStatsDto } from '../types/api';
import { useToastStore } from '../stores/toast';
import { useI18n } from 'vue-i18n';

const stats = ref<TrainingRequestStatsDto | null>(null);
const isLoading = ref(false);
const toast = useToastStore();
const { t } = useI18n();

const fetchStats = async () => {
  isLoading.value = true;
  try {
    const response = await apiClient.get<TrainingRequestStatsDto>('/admin/statistics');
    stats.value = response.data;
  } catch (error) {
    console.error('Failed to fetch statistics', error);
    toast.error(t('admin.loadStatsFailed'));
  } finally {
    isLoading.value = false;
  }
};

onMounted(() => {
  fetchStats();
});
</script>

<template>
  <div>
    <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-6">{{ t('admin.dashboardOverview') }}</h2>
    
    <div v-if="isLoading" class="flex justify-center py-12">
      <font-awesome-icon icon="spinner" spin class="text-indigo-500 dark:text-indigo-400 text-3xl" />
    </div>

    <div v-else-if="stats" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
      <!-- Requests Stats -->
      <div class="bg-white dark:bg-gray-800 overflow-hidden shadow rounded-lg transition-colors duration-200">
        <div class="p-5">
          <div class="flex items-center">
            <div class="flex-shrink-0 bg-indigo-500 rounded-md p-3">
              <font-awesome-icon icon="paper-plane" class="h-6 w-6 text-white" />
            </div>
            <div class="ml-5 w-0 flex-1">
              <dl>
                <dt class="text-sm font-medium text-gray-500 dark:text-gray-400 truncate">{{ t('admin.dailyRequests') }}</dt>
                <dd class="text-lg font-medium text-gray-900 dark:text-white">{{ stats.dailyRequests }}</dd>
              </dl>
            </div>
          </div>
        </div>
      </div>

      <div class="bg-white dark:bg-gray-800 overflow-hidden shadow rounded-lg transition-colors duration-200">
        <div class="p-5">
          <div class="flex items-center">
            <div class="flex-shrink-0 bg-indigo-500 rounded-md p-3">
              <font-awesome-icon icon="paper-plane" class="h-6 w-6 text-white" />
            </div>
            <div class="ml-5 w-0 flex-1">
              <dl>
                <dt class="text-sm font-medium text-gray-500 dark:text-gray-400 truncate">{{ t('admin.weeklyRequests') }}</dt>
                <dd class="text-lg font-medium text-gray-900 dark:text-white">{{ stats.weeklyRequests }}</dd>
              </dl>
            </div>
          </div>
        </div>
      </div>

      <div class="bg-white dark:bg-gray-800 overflow-hidden shadow rounded-lg transition-colors duration-200">
        <div class="p-5">
          <div class="flex items-center">
            <div class="flex-shrink-0 bg-indigo-500 rounded-md p-3">
              <font-awesome-icon icon="paper-plane" class="h-6 w-6 text-white" />
            </div>
            <div class="ml-5 w-0 flex-1">
              <dl>
                <dt class="text-sm font-medium text-gray-500 dark:text-gray-400 truncate">{{ t('admin.monthlyRequests') }}</dt>
                <dd class="text-lg font-medium text-gray-900 dark:text-white">{{ stats.monthlyRequests }}</dd>
              </dl>
            </div>
          </div>
        </div>
      </div>

      <!-- Responses Stats -->
      <div class="bg-white dark:bg-gray-800 overflow-hidden shadow rounded-lg transition-colors duration-200">
        <div class="p-5">
          <div class="flex items-center">
            <div class="flex-shrink-0 bg-green-500 rounded-md p-3">
              <font-awesome-icon icon="check" class="h-6 w-6 text-white" />
            </div>
            <div class="ml-5 w-0 flex-1">
              <dl>
                <dt class="text-sm font-medium text-gray-500 dark:text-gray-400 truncate">{{ t('admin.dailyResponses') }}</dt>
                <dd class="text-lg font-medium text-gray-900 dark:text-white">{{ stats.dailyResponses }}</dd>
              </dl>
            </div>
          </div>
        </div>
      </div>

      <div class="bg-white dark:bg-gray-800 overflow-hidden shadow rounded-lg transition-colors duration-200">
        <div class="p-5">
          <div class="flex items-center">
            <div class="flex-shrink-0 bg-green-500 rounded-md p-3">
              <font-awesome-icon icon="check" class="h-6 w-6 text-white" />
            </div>
            <div class="ml-5 w-0 flex-1">
              <dl>
                <dt class="text-sm font-medium text-gray-500 dark:text-gray-400 truncate">{{ t('admin.weeklyResponses') }}</dt>
                <dd class="text-lg font-medium text-gray-900 dark:text-white">{{ stats.weeklyResponses }}</dd>
              </dl>
            </div>
          </div>
        </div>
      </div>

      <div class="bg-white dark:bg-gray-800 overflow-hidden shadow rounded-lg transition-colors duration-200">
        <div class="p-5">
          <div class="flex items-center">
            <div class="flex-shrink-0 bg-green-500 rounded-md p-3">
              <font-awesome-icon icon="check" class="h-6 w-6 text-white" />
            </div>
            <div class="ml-5 w-0 flex-1">
              <dl>
                <dt class="text-sm font-medium text-gray-500 dark:text-gray-400 truncate">{{ t('admin.monthlyResponses') }}</dt>
                <dd class="text-lg font-medium text-gray-900 dark:text-white">{{ stats.monthlyResponses }}</dd>
              </dl>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
