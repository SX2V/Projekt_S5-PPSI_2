<script setup lang="ts">
import { ref, computed } from 'vue';
import { useAuthStore } from '../stores/auth';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import type { RegisterDto } from '../types/api';

const authStore = useAuthStore();
const router = useRouter();
const { t } = useI18n();

const name = ref('');
const email = ref('');
const password = ref('');
const confirmPassword = ref('');
const role = ref<string>('User'); 
const errorMessage = ref('');

const passwordsMatch = computed(() => password.value === confirmPassword.value);

const isPasswordValid = computed(() => {
  const p = password.value;
  const hasMinLength = p.length >= 8;
  const hasUpperCase = /[A-Z]/.test(p);
  const hasLowerCase = /[a-z]/.test(p);
  const hasNumber = /[0-9]/.test(p);

  return hasMinLength && hasUpperCase && hasLowerCase && hasNumber;
});

const handleRegister = async () => {
  errorMessage.value = '';
  
  if (!name.value || !email.value || !password.value || !confirmPassword.value) {
    errorMessage.value = t('auth.fillAllFields');
    return;
  }

  if (!isPasswordValid.value) {
      errorMessage.value = t('auth.passwordInvalid');
      return;
  }

  if (!passwordsMatch.value) {
    errorMessage.value = t('auth.passwordMismatch');
    return;
  }

  const registerData: RegisterDto = {
    name: name.value,
    email: email.value,
    password: password.value,
    role: role.value,
  };

  try {
    await authStore.register(registerData);
    router.push('/login');
  } catch (error) {
    errorMessage.value = t('auth.registrationFailed');
  }
};
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-100 dark:bg-gray-900 px-4 sm:px-6 lg:px-8 transition-colors duration-200">
    <div class="max-w-md w-full space-y-8 bg-white dark:bg-gray-800 p-8 rounded-xl shadow-lg transition-colors duration-200">
      <div>
        <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-white">
          {{ t('auth.createAccountTitle') }}
        </h2>
        <p class="mt-2 text-center text-sm text-gray-600 dark:text-gray-400">
          {{ t('auth.alreadyHaveAccount') }}
          <a href="#" class="font-medium text-indigo-600 hover:text-indigo-500 dark:text-indigo-400 dark:hover:text-indigo-300" @click.prevent="router.push('/login')">
            {{ t('auth.signIn') }}
          </a>
        </p>
      </div>
      <form class="mt-8 space-y-6" @submit.prevent="handleRegister">
        <div class="rounded-md shadow-sm -space-y-px">
          <div class="relative">
            <label for="name" class="sr-only">{{ t('common.name') }}</label>
            <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
              <font-awesome-icon icon="user" class="text-gray-400 dark:text-gray-500" />
            </div>
            <input
              id="name"
              name="name"
              type="text"
              required
              v-model="name"
              class="appearance-none rounded-none rounded-t-md relative block w-full px-3 py-2 pl-10 border border-gray-300 dark:border-gray-600 placeholder-gray-500 dark:placeholder-gray-400 text-gray-900 dark:text-white focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm bg-white dark:bg-gray-700"
              :placeholder="t('common.name')"
            />
          </div>
          <div class="relative">
            <label for="email-address" class="sr-only">{{ t('common.email') }}</label>
            <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
              <font-awesome-icon icon="envelope" class="text-gray-400 dark:text-gray-500" />
            </div>
            <input
              id="email-address"
              name="email"
              type="email"
              autocomplete="email"
              required
              v-model="email"
              class="appearance-none relative block w-full px-3 py-2 pl-10 border border-gray-300 dark:border-gray-600 placeholder-gray-500 dark:placeholder-gray-400 text-gray-900 dark:text-white focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm bg-white dark:bg-gray-700"
              :placeholder="t('common.email')"
            />
          </div>
          <div class="relative">
            <label for="password" class="sr-only">{{ t('common.password') }}</label>
            <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
              <font-awesome-icon icon="lock" class="text-gray-400 dark:text-gray-500" />
            </div>
            <input
              id="password"
              name="password"
              type="password"
              autocomplete="new-password"
              required
              v-model="password"
              class="appearance-none relative block w-full px-3 py-2 pl-10 border border-gray-300 dark:border-gray-600 placeholder-gray-500 dark:placeholder-gray-400 text-gray-900 dark:text-white focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm bg-white dark:bg-gray-700"
              :placeholder="t('common.password')"
            />
          </div>
          <div class="relative">
            <label for="confirm-password" class="sr-only">{{ t('common.confirmPassword') }}</label>
            <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
              <font-awesome-icon icon="lock" class="text-gray-400 dark:text-gray-500" />
            </div>
            <input
              id="confirm-password"
              name="confirm-password"
              type="password"
              autocomplete="new-password"
              required
              v-model="confirmPassword"
              class="appearance-none rounded-none rounded-b-md relative block w-full px-3 py-2 pl-10 border border-gray-300 dark:border-gray-600 placeholder-gray-500 dark:placeholder-gray-400 text-gray-900 dark:text-white focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm bg-white dark:bg-gray-700"
              :placeholder="t('common.confirmPassword')"
            />
          </div>
        </div>

        <!-- Password Requirements Hint -->
        <div v-if="password && !isPasswordValid" class="text-xs text-gray-500 dark:text-gray-400 mt-1">
            <p>{{ t('auth.passwordRequirements') }}</p>
            <ul class="list-disc list-inside pl-2">
                <li :class="{'text-green-600': password.length >= 8, 'text-red-500': password.length < 8}">{{ t('auth.reqLength') }}</li>
                <li :class="{'text-green-600': /[A-Z]/.test(password), 'text-red-500': !/[A-Z]/.test(password)}">{{ t('auth.reqUpper') }}</li>
                <li :class="{'text-green-600': /[a-z]/.test(password), 'text-red-500': !/[a-z]/.test(password)}">{{ t('auth.reqLower') }}</li>
                <li :class="{'text-green-600': /[0-9]/.test(password), 'text-red-500': !/[0-9]/.test(password)}">{{ t('auth.reqNumber') }}</li>
            </ul>
        </div>

        <div v-if="errorMessage" class="text-red-500 text-sm text-center">
          {{ errorMessage }}
        </div>

        <div>
          <button
            type="submit"
            :disabled="authStore.isLoading"
            class="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <span class="absolute left-0 inset-y-0 flex items-center pl-3">
              <font-awesome-icon v-if="!authStore.isLoading" icon="user-plus" class="h-5 w-5 text-indigo-500 group-hover:text-indigo-400 dark:text-indigo-300 dark:group-hover:text-indigo-200" />
              <font-awesome-icon v-else icon="spinner" spin class="h-5 w-5 text-indigo-500 dark:text-indigo-300" />
            </span>
            {{ authStore.isLoading ? t('auth.creatingAccount') : t('auth.createAccount') }}
          </button>
        </div>

        <div class="mt-6">
          <div class="relative">
            <div class="absolute inset-0 flex items-center">
              <div class="w-full border-t border-gray-300 dark:border-gray-600"></div>
            </div>
            <div class="relative flex justify-center text-sm">
              <span class="px-2 bg-white dark:bg-gray-800 text-gray-500 dark:text-gray-400">
                {{ t('auth.continueWith') }}
              </span>
            </div>
          </div>

          <div class="mt-6 grid grid-cols-2 gap-3">
            <div>
              <a href="#" class="w-full inline-flex justify-center py-2 px-4 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-sm font-medium text-gray-500 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-600">
                <span class="sr-only">{{ t('auth.signUpFacebook') }}</span>
                <font-awesome-icon :icon="['fab', 'facebook']" class="h-5 w-5 text-blue-600" />
              </a>
            </div>

            <div>
              <a href="#" class="w-full inline-flex justify-center py-2 px-4 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-sm font-medium text-gray-500 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-600">
                <span class="sr-only">{{ t('auth.signUpStrava') }}</span>
                <font-awesome-icon :icon="['fab', 'strava']" class="h-5 w-5 text-orange-600" />
              </a>
            </div>
          </div>
        </div>
      </form>
    </div>
  </div>
</template>
