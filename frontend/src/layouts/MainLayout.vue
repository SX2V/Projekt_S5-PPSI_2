<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { useI18n } from 'vue-i18n';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const { t, locale } = useI18n();

const handleLogout = () => {
  authStore.logout();
  router.push('/login');
};

const isActive = (path: string) => route.path.startsWith(path);

const toggleLanguage = () => {
  locale.value = locale.value === 'en' ? 'pl' : 'en';
};
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 flex flex-col transition-colors duration-200">
    <!-- Desktop Header -->
    <header class="hidden md:block bg-white dark:bg-gray-800 shadow-sm sticky top-0 z-50 transition-colors duration-200">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between h-16">
          <div class="flex">
            <div class="flex-shrink-0 flex items-center">
              <span class="text-2xl font-bold text-indigo-600 dark:text-indigo-400">SportConnect</span>
            </div>
            <div class="hidden md:ml-6 md:flex md:space-x-8">
              <router-link 
                to="/match" 
                class="inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium transition-colors duration-200"
                :class="isActive('/match') ? 'border-indigo-500 text-gray-900 dark:text-white' : 'border-transparent text-gray-500 dark:text-gray-300 hover:border-gray-300 hover:text-gray-700 dark:hover:text-white'"
              >
                {{ t('nav.findMatch') }}
              </router-link>
              <router-link 
                to="/inbox" 
                class="inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium transition-colors duration-200"
                :class="isActive('/inbox') ? 'border-indigo-500 text-gray-900 dark:text-white' : 'border-transparent text-gray-500 dark:text-gray-300 hover:border-gray-300 hover:text-gray-700 dark:hover:text-white'"
              >
                {{ t('nav.inbox') }}
              </router-link>
              <router-link 
                to="/profile" 
                class="inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium transition-colors duration-200"
                :class="isActive('/profile') ? 'border-indigo-500 text-gray-900 dark:text-white' : 'border-transparent text-gray-500 dark:text-gray-300 hover:border-gray-300 hover:text-gray-700 dark:hover:text-white'"
              >
                {{ t('nav.profile') }}
              </router-link>
            </div>
          </div>
          <div class="flex items-center space-x-4">
            <button 
              @click="toggleLanguage"
              class="p-2 text-gray-500 dark:text-gray-400 hover:text-indigo-600 dark:hover:text-indigo-400"
              :title="locale === 'en' ? 'Switch to Polish' : 'Switch to English'"
            >
              <span class="font-bold text-sm">{{ locale.toUpperCase() }}</span>
            </button>
            <button
              @click="handleLogout"
              class="px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
            >
              {{ t('common.logout') }}
            </button>
          </div>
        </div>
      </div>
    </header>

    <!-- Main Content -->
    <main class="flex-1 overflow-y-auto pb-16 md:pb-0">
      <router-view />
    </main>

    <!-- Mobile Bottom Navigation -->
    <nav class="md:hidden fixed bottom-0 w-full bg-white dark:bg-gray-800 border-t border-gray-200 dark:border-gray-700 flex justify-around py-2 z-50 pb-safe transition-colors duration-200">
      <router-link 
        to="/match" 
        class="flex flex-col items-center p-2 text-xs font-medium transition-colors duration-200"
        :class="isActive('/match') ? 'text-indigo-600 dark:text-indigo-400' : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200'"
      >
        <font-awesome-icon icon="search" class="text-xl mb-1" />
        {{ t('nav.match') }}
      </router-link>
      
      <router-link 
        to="/inbox" 
        class="flex flex-col items-center p-2 text-xs font-medium transition-colors duration-200"
        :class="isActive('/inbox') ? 'text-indigo-600 dark:text-indigo-400' : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200'"
      >
        <font-awesome-icon icon="inbox" class="text-xl mb-1" />
        {{ t('nav.inbox') }}
      </router-link>
      
      <router-link 
        to="/profile" 
        class="flex flex-col items-center p-2 text-xs font-medium transition-colors duration-200"
        :class="isActive('/profile') ? 'text-indigo-600 dark:text-indigo-400' : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200'"
      >
        <font-awesome-icon icon="user" class="text-xl mb-1" />
        {{ t('nav.profile') }}
      </router-link>

      <button 
        @click="toggleLanguage"
        class="flex flex-col items-center p-2 text-xs font-medium text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 transition-colors duration-200"
      >
        <font-awesome-icon icon="globe" class="text-xl mb-1" />
        {{ locale.toUpperCase() }}
      </button>

      <button 
        @click="handleLogout"
        class="flex flex-col items-center p-2 text-xs font-medium text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 transition-colors duration-200"
      >
        <font-awesome-icon icon="sign-out-alt" class="text-xl mb-1" />
        {{ t('common.logout') }}
      </button>
    </nav>
  </div>
</template>

<style scoped>
.pb-safe {
  padding-bottom: env(safe-area-inset-bottom);
}
</style>
