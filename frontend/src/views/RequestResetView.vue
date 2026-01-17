<script setup lang="ts">
import { ref } from 'vue';
import apiClient from '../api/axios';
import { useRouter } from 'vue-router';
import { useToastStore } from '../stores/toast';
import { useI18n } from 'vue-i18n';
import type { RequestPasswordResetDto } from '../types/api';

const router = useRouter();
const toast = useToastStore();
const { t } = useI18n();

const email = ref('');
const isLoading = ref(false);

const handleRequestReset = async () => {
  if (!email.value) return;
  
  isLoading.value = true;
  const payload: RequestPasswordResetDto = { email: email.value };

  try {
    await apiClient.post('/auth/request-reset', payload);
    toast.success(t('auth.resetLinkSent'));
    setTimeout(() => router.push('/login'), 3000);
  } catch (error) {
    console.error('Failed to request password reset', error);
    toast.error(t('auth.resetRequestFailed'));
  } finally {
    isLoading.value = false;
  }
};
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-100 dark:bg-gray-900 px-4 sm:px-6 lg:px-8 transition-colors duration-200">
    <div class="max-w-md w-full space-y-8 bg-white dark:bg-gray-800 p-8 rounded-xl shadow-lg transition-colors duration-200">
      <div>
        <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-white">
          {{ t('auth.resetPasswordTitle') }}
        </h2>
        <p class="mt-2 text-center text-sm text-gray-600 dark:text-gray-400">
          {{ t('auth.resetPasswordDesc') }}
        </p>
      </div>
      <form class="mt-8 space-y-6" @submit.prevent="handleRequestReset">
        <div class="rounded-md shadow-sm -space-y-px">
          <div>
            <label for="email-address" class="sr-only">{{ t('common.email') }}</label>
            <input
              id="email-address"
              name="email"
              type="email"
              autocomplete="email"
              required
              v-model="email"
              class="appearance-none rounded-md relative block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 placeholder-gray-500 dark:placeholder-gray-400 text-gray-900 dark:text-white focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm bg-white dark:bg-gray-700"
              :placeholder="t('common.email')"
            />
          </div>
        </div>

        <div>
          <button
            type="submit"
            :disabled="isLoading"
            class="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50"
          >
            <span class="absolute left-0 inset-y-0 flex items-center pl-3">
              <font-awesome-icon v-if="!isLoading" icon="envelope" class="h-5 w-5 text-indigo-500 group-hover:text-indigo-400 dark:text-indigo-300 dark:group-hover:text-indigo-200" />
              <font-awesome-icon v-else icon="spinner" spin class="h-5 w-5 text-indigo-500 dark:text-indigo-300" />
            </span>
            {{ isLoading ? t('auth.sending') : t('auth.sendResetLink') }}
          </button>
        </div>
        
        <div class="text-center">
            <a href="#" class="font-medium text-indigo-600 hover:text-indigo-500 dark:text-indigo-400 dark:hover:text-indigo-300 text-sm" @click.prevent="router.push('/login')">
              {{ t('auth.backToSignIn') }}
            </a>
        </div>
      </form>
    </div>
  </div>
</template>
