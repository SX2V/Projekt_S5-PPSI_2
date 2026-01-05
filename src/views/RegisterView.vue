<script setup lang="ts">
import { ref, computed } from 'vue';
import { useAuthStore } from '../stores/auth';
import { useRouter } from 'vue-router';
import type { RegisterDto } from '../types/api';
import { UserRoleEnum } from '../types/enums';

const authStore = useAuthStore();
const router = useRouter();

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

const passwordRequirements = computed(() => {
    const reqs = [];
    if (password.value.length < 8) reqs.push("At least 8 characters");
    if (!/[A-Z]/.test(password.value)) reqs.push("One uppercase letter");
    if (!/[a-z]/.test(password.value)) reqs.push("One lowercase letter");
    if (!/[0-9]/.test(password.value)) reqs.push("One number");
    return reqs;
});

const handleRegister = async () => {
  errorMessage.value = '';
  
  if (!name.value || !email.value || !password.value || !confirmPassword.value) {
    errorMessage.value = 'Please fill in all fields.';
    return;
  }

  if (!isPasswordValid.value) {
      errorMessage.value = 'Password does not meet requirements.';
      return;
  }

  if (!passwordsMatch.value) {
    errorMessage.value = 'Passwords do not match.';
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
    errorMessage.value = 'Registration failed. Please try again.';
  }
};
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-100 dark:bg-gray-900 px-4 sm:px-6 lg:px-8 transition-colors duration-200">
    <div class="max-w-md w-full space-y-8 bg-white dark:bg-gray-800 p-8 rounded-xl shadow-lg transition-colors duration-200">
      <div>
        <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-white">
          Create your account
        </h2>
        <p class="mt-2 text-center text-sm text-gray-600 dark:text-gray-400">
          Already have an account?
          <a href="#" class="font-medium text-indigo-600 hover:text-indigo-500 dark:text-indigo-400 dark:hover:text-indigo-300" @click.prevent="router.push('/login')">
            Sign in
          </a>
        </p>
      </div>
      <form class="mt-8 space-y-6" @submit.prevent="handleRegister">
        <div class="rounded-md shadow-sm -space-y-px">
          <div class="relative">
            <label for="name" class="sr-only">Name</label>
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
              placeholder="Name"
            />
          </div>
          <div class="relative">
            <label for="email-address" class="sr-only">Email address</label>
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
              placeholder="Email address"
            />
          </div>
          <div class="relative">
            <label for="password" class="sr-only">Password</label>
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
              placeholder="Password"
            />
          </div>
          <div class="relative">
            <label for="confirm-password" class="sr-only">Confirm Password</label>
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
              placeholder="Confirm Password"
            />
          </div>
        </div>

        <!-- Password Requirements Hint -->
        <div v-if="password && !isPasswordValid" class="text-xs text-gray-500 dark:text-gray-400 mt-1">
            <p>Password must contain:</p>
            <ul class="list-disc list-inside pl-2">
                <li :class="{'text-green-600': password.length >= 8, 'text-red-500': password.length < 8}">At least 8 characters</li>
                <li :class="{'text-green-600': /[A-Z]/.test(password), 'text-red-500': !/[A-Z]/.test(password)}">One uppercase letter</li>
                <li :class="{'text-green-600': /[a-z]/.test(password), 'text-red-500': !/[a-z]/.test(password)}">One lowercase letter</li>
                <li :class="{'text-green-600': /[0-9]/.test(password), 'text-red-500': !/[0-9]/.test(password)}">One number</li>
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
            {{ authStore.isLoading ? 'Creating account...' : 'Create account' }}
          </button>
        </div>

        <div class="mt-6">
          <div class="relative">
            <div class="absolute inset-0 flex items-center">
              <div class="w-full border-t border-gray-300 dark:border-gray-600"></div>
            </div>
            <div class="relative flex justify-center text-sm">
              <span class="px-2 bg-white dark:bg-gray-800 text-gray-500 dark:text-gray-400">
                Or sign up with
              </span>
            </div>
          </div>

          <div class="mt-6 grid grid-cols-2 gap-3">
            <div>
              <a href="#" class="w-full inline-flex justify-center py-2 px-4 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-sm font-medium text-gray-500 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-600">
                <span class="sr-only">Sign up with Facebook</span>
                <font-awesome-icon :icon="['fab', 'facebook']" class="h-5 w-5 text-blue-600" />
              </a>
            </div>

            <div>
              <a href="#" class="w-full inline-flex justify-center py-2 px-4 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-sm font-medium text-gray-500 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-600">
                <span class="sr-only">Sign up with Strava</span>
                <font-awesome-icon :icon="['fab', 'strava']" class="h-5 w-5 text-orange-600" />
              </a>
            </div>
          </div>
        </div>
      </form>
    </div>
  </div>
</template>
