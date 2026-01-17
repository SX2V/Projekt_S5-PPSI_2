<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import apiClient from '../api/axios';
import { useRouter, useRoute } from 'vue-router';
import { useToastStore } from '../stores/toast';
import { useI18n } from 'vue-i18n';
import type { ResetPasswordDto } from '../types/api';

const router = useRouter();
const route = useRoute();
const toast = useToastStore();
const { t } = useI18n();

const password = ref('');
const confirmPassword = ref('');
const token = ref('');
const isLoading = ref(false);

const passwordsMatch = computed(() => password.value === confirmPassword.value);
const isPasswordValid = computed(() => {
  const p = password.value;
  return p.length >= 8 && /[A-Z]/.test(p) && /[a-z]/.test(p) && /[0-9]/.test(p);
});

const handleResetPassword = async () => {
  if (!password.value || !token.value) return;
  
  if (!isPasswordValid.value) {
      toast.error(t('auth.passwordInvalid'));
      return;
  }

  if (!passwordsMatch.value) {
      toast.error(t('auth.passwordMismatch'));
      return;
  }
  
  isLoading.value = true;
  const payload: ResetPasswordDto = { 
      token: token.value,
      newPassword: password.value 
  };

  try {
    await apiClient.post('/auth/reset-password', payload);
    toast.success(t('auth.resetSuccess'));
    setTimeout(() => router.push('/login'), 2000);
  } catch (error) {
    console.error('Failed to reset password', error);
    toast.error(t('auth.resetFailed'));
  } finally {
    isLoading.value = false;
  }
};

onMounted(() => {
    const tokenParam = route.query.token;
    if (tokenParam) {
        token.value = tokenParam as string;
    } else {
        toast.error(t('auth.missingToken'));
    }
});
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-100 dark:bg-gray-900 px-4 sm:px-6 lg:px-8 transition-colors duration-200">
    <div class="max-w-md w-full space-y-8 bg-white dark:bg-gray-800 p-8 rounded-xl shadow-lg transition-colors duration-200">
      <div>
        <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-white">
          {{ t('auth.setNewPassword') }}
        </h2>
      </div>
      <form class="mt-8 space-y-6" @submit.prevent="handleResetPassword">
        <div class="rounded-md shadow-sm -space-y-px">
          <div class="relative">
            <label for="password" class="sr-only">{{ t('auth.newPassword') }}</label>
            <input
              id="password"
              name="password"
              type="password"
              autocomplete="new-password"
              required
              v-model="password"
              class="appearance-none rounded-none rounded-t-md relative block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 placeholder-gray-500 dark:placeholder-gray-400 text-gray-900 dark:text-white focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm bg-white dark:bg-gray-700"
              :placeholder="t('auth.newPassword')"
            />
          </div>
          <div class="relative">
            <label for="confirm-password" class="sr-only">{{ t('auth.confirmNewPassword') }}</label>
            <input
              id="confirm-password"
              name="confirm-password"
              type="password"
              autocomplete="new-password"
              required
              v-model="confirmPassword"
              class="appearance-none rounded-none rounded-b-md relative block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 placeholder-gray-500 dark:placeholder-gray-400 text-gray-900 dark:text-white focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm bg-white dark:bg-gray-700"
              :placeholder="t('auth.confirmNewPassword')"
            />
          </div>
        </div>

        <!-- Password Requirements Hint -->
        <div v-if="password && !isPasswordValid" class="text-xs text-gray-500 dark:text-gray-400 mt-1">
            <p>{{ t('auth.passwordRequirements') }}</p>
            <ul class="list-disc list-inside pl-2">
                <li :class="{'text-green-600 dark:text-green-400': password.length >= 8, 'text-red-500 dark:text-red-400': password.length < 8}">{{ t('auth.reqLength') }}</li>
                <li :class="{'text-green-600 dark:text-green-400': /[A-Z]/.test(password), 'text-red-500 dark:text-red-400': !/[A-Z]/.test(password)}">{{ t('auth.reqUpper') }}</li>
                <li :class="{'text-green-600 dark:text-green-400': /[a-z]/.test(password), 'text-red-500 dark:text-red-400': !/[a-z]/.test(password)}">{{ t('auth.reqLower') }}</li>
                <li :class="{'text-green-600 dark:text-green-400': /[0-9]/.test(password), 'text-red-500 dark:text-red-400': !/[0-9]/.test(password)}">{{ t('auth.reqNumber') }}</li>
            </ul>
        </div>

        <div>
          <button
            type="submit"
            :disabled="isLoading || !token"
            class="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50"
          >
            <span class="absolute left-0 inset-y-0 flex items-center pl-3">
              <font-awesome-icon v-if="!isLoading" icon="lock" class="h-5 w-5 text-indigo-500 group-hover:text-indigo-400 dark:text-indigo-300 dark:group-hover:text-indigo-200" />
              <font-awesome-icon v-else icon="spinner" spin class="h-5 w-5 text-indigo-500 dark:text-indigo-300" />
            </span>
            {{ isLoading ? t('auth.resetting') : t('auth.resetPassword') }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
