<script setup lang="ts">
import { ref, onMounted } from 'vue';
import apiClient from '../api/axios';
import { useToastStore } from '../stores/toast';
import { useI18n } from 'vue-i18n';
import type { UserMatchDto, Sport } from '../types/api';

interface AdminUser extends UserMatchDto {
    isBlocked?: boolean;
}

const users = ref<AdminUser[]>([]);
const isLoading = ref(false);
const processingId = ref<string | null>(null);
const toast = useToastStore();
const { t } = useI18n();

const fetchUsers = async () => {
  isLoading.value = true;
  try {
    const sportsResponse = await apiClient.get<Sport[]>('/Sports');
    const sports = sportsResponse.data;
    
    if (sports.length === 0) {
        toast.warning(t('admin.noSportsFound'));
        return;
    }

    const allUsersMap = new Map<string, AdminUser>();

    const promises = sports.map(async sport => {
        try {
            const res = await apiClient.get<AdminUser[]>('/Users', {
                params: {
                    sportId: sport.id,
                    maxSearchRadiusKm: 10000, 
                }
            });
            return res.data;
        } catch (err) {
            return [];
        }
    });

    const results = await Promise.all(promises);

    results.flat().forEach(user => {
        if (user.id && !allUsersMap.has(user.id)) {
            allUsersMap.set(user.id, user);
        }
    });

    users.value = Array.from(allUsersMap.values());

  } catch (error) {
    console.error('Failed to fetch users', error);
    toast.error(t('admin.loadUsersFailed'));
  } finally {
    isLoading.value = false;
  }
};

const toggleBlockStatus = async (user: AdminUser) => {
  if (!user.id) return;
  processingId.value = user.id;
  
  try {
    if (user.isBlocked) {
      await apiClient.patch(`/Users/${user.id}/unblock`);
      user.isBlocked = false;
      toast.success(t('admin.userUnblocked', { name: user.name }));
    } else {
      await apiClient.patch(`/Users/${user.id}/block`);
      user.isBlocked = true;
      toast.success(t('admin.userBlocked', { name: user.name }));
    }
  } catch (error) {
    console.error('Failed to update block status', error);
    toast.error(t('admin.updateStatusFailed'));
  } finally {
    processingId.value = null;
  }
};

onMounted(() => {
  fetchUsers();
});
</script>

<template>
  <div>
    <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-6">{{ t('admin.userManagement') }}</h2>

    <div class="bg-white dark:bg-gray-800 shadow overflow-hidden sm:rounded-lg transition-colors duration-200">
      <div v-if="isLoading" class="flex justify-center py-12">
        <font-awesome-icon icon="spinner" spin class="text-indigo-500 dark:text-indigo-400 text-3xl" />
      </div>

      <div v-else-if="users.length === 0" class="text-center py-12 text-gray-500 dark:text-gray-400">
        <p>{{ t('admin.noUsersFound') }}</p>
        <p class="text-xs mt-2">{{ t('admin.usersNote') }}</p>
      </div>

      <table v-else class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-700">
          <tr>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
              {{ t('common.name') }}
            </th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
              {{ t('common.email') }}
            </th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
              {{ t('admin.status') }}
            </th>
            <th scope="col" class="relative px-6 py-3">
              <span class="sr-only">{{ t('common.actions') }}</span>
            </th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="user in users" :key="user.id">
            <td class="px-6 py-4 whitespace-nowrap">
              <div class="flex items-center">
                <div class="flex-shrink-0 h-10 w-10">
                  <div class="h-10 w-10 rounded-full bg-indigo-100 dark:bg-indigo-900 flex items-center justify-center text-indigo-500 dark:text-indigo-300 font-bold overflow-hidden">
                    {{ user.name ? user.name.charAt(0).toUpperCase() : 'U' }}
                  </div>
                </div>
                <div class="ml-4">
                  <div class="text-sm font-medium text-gray-900 dark:text-white">
                    {{ user.name }}
                  </div>
                </div>
              </div>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <div class="text-sm text-gray-500 dark:text-gray-400">{{ user.email }}</div>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <span 
                class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full"
                :class="user.isBlocked ? 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200' : 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200'"
              >
                {{ user.isBlocked ? t('admin.blocked') : t('admin.active') }}
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button 
                @click="toggleBlockStatus(user)"
                :disabled="processingId === user.id"
                class="text-indigo-600 hover:text-indigo-900 dark:text-indigo-400 dark:hover:text-indigo-300 focus:outline-none disabled:opacity-50"
              >
                <span v-if="processingId === user.id">
                    <font-awesome-icon icon="spinner" spin />
                </span>
                <span v-else>
                    <font-awesome-icon :icon="user.isBlocked ? 'unlock' : 'ban'" :class="user.isBlocked ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'" />
                    <span class="ml-1">{{ user.isBlocked ? t('admin.unblock') : t('admin.block') }}</span>
                </span>
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
