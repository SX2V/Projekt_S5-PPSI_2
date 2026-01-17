<script setup lang="ts">
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { useI18n } from 'vue-i18n';

const router = useRouter();
const authStore = useAuthStore();
const { t, locale } = useI18n();

const handleLogout = () => {
  authStore.logout();
  router.push('/login');
};

const toggleLanguage = () => {
  locale.value = locale.value === 'en' ? 'pl' : 'en';
};
</script>

<template>
  <div class="min-h-screen bg-gray-100 dark:bg-gray-900 flex transition-colors duration-200">
    <!-- Sidebar -->
    <aside class="w-64 bg-gray-800 text-white flex flex-col hidden md:flex">
      <div class="h-16 flex items-center justify-center border-b border-gray-700">
        <h1 class="text-xl font-bold">{{ t('nav.adminPanel') }}</h1>
      </div>
      <nav class="flex-1 px-2 py-4 space-y-2">
        <router-link 
          to="/admin/dashboard" 
          class="flex items-center px-4 py-2 rounded-md hover:bg-gray-700 transition-colors"
          active-class="bg-gray-900 text-white"
        >
          <font-awesome-icon icon="tachometer-alt" class="mr-3 w-5" />
          {{ t('nav.dashboard') }}
        </router-link>
        <router-link 
          to="/admin/users" 
          class="flex items-center px-4 py-2 rounded-md hover:bg-gray-700 transition-colors"
          active-class="bg-gray-900 text-white"
        >
          <font-awesome-icon icon="users" class="mr-3 w-5" />
          {{ t('nav.users') }}
        </router-link>
        <router-link 
          to="/admin/sports" 
          class="flex items-center px-4 py-2 rounded-md hover:bg-gray-700 transition-colors"
          active-class="bg-gray-900 text-white"
        >
          <font-awesome-icon icon="running" class="mr-3 w-5" />
          {{ t('nav.sports') }}
        </router-link>
        <router-link 
          to="/admin/logs" 
          class="flex items-center px-4 py-2 rounded-md hover:bg-gray-700 transition-colors"
          active-class="bg-gray-900 text-white"
        >
          <font-awesome-icon icon="file-alt" class="mr-3 w-5" />
          {{ t('nav.logs') }}
        </router-link>
      </nav>
      <div class="p-4 border-t border-gray-700">
        <button 
          @click="toggleLanguage"
          class="flex items-center w-full px-4 py-2 text-sm text-gray-300 hover:text-white hover:bg-gray-700 rounded-md transition-colors mb-2"
        >
          <font-awesome-icon icon="globe" class="mr-3 w-5" />
          {{ locale.toUpperCase() }}
        </button>
        <button 
          @click="handleLogout"
          class="flex items-center w-full px-4 py-2 text-sm text-gray-300 hover:text-white hover:bg-gray-700 rounded-md transition-colors"
        >
          <font-awesome-icon icon="sign-out-alt" class="mr-3 w-5" />
          {{ t('common.logout') }}
        </button>
      </div>
    </aside>

    <!-- Mobile Header -->
    <div class="md:hidden fixed top-0 w-full bg-gray-800 text-white z-20 flex justify-between items-center px-4 h-16">
        <span class="font-bold">{{ t('nav.adminPanel') }}</span>
        <div class="flex items-center space-x-4">
            <button @click="toggleLanguage">
                <span class="font-bold text-sm">{{ locale.toUpperCase() }}</span>
            </button>
            <button @click="handleLogout">
                <font-awesome-icon icon="sign-out-alt" />
            </button>
        </div>
    </div>

    <!-- Main Content -->
    <main class="flex-1 p-4 md:p-8 mt-16 md:mt-0 overflow-y-auto">
      <router-view></router-view>
    </main>
    
    <!-- Mobile Bottom Nav -->
    <nav class="md:hidden fixed bottom-0 w-full bg-white dark:bg-gray-800 border-t border-gray-200 dark:border-gray-700 flex justify-around py-3 z-20 transition-colors duration-200">
        <router-link to="/admin/dashboard" class="flex flex-col items-center text-xs text-gray-500 dark:text-gray-400" active-class="text-indigo-600 dark:text-indigo-400">
            <font-awesome-icon icon="tachometer-alt" class="text-lg mb-1" />
            {{ t('nav.dashboard') }}
        </router-link>
        <router-link to="/admin/users" class="flex flex-col items-center text-xs text-gray-500 dark:text-gray-400" active-class="text-indigo-600 dark:text-indigo-400">
            <font-awesome-icon icon="users" class="text-lg mb-1" />
            {{ t('nav.users') }}
        </router-link>
        <router-link to="/admin/sports" class="flex flex-col items-center text-xs text-gray-500 dark:text-gray-400" active-class="text-indigo-600 dark:text-indigo-400">
            <font-awesome-icon icon="running" class="text-lg mb-1" />
            {{ t('nav.sports') }}
        </router-link>
        <router-link to="/admin/logs" class="flex flex-col items-center text-xs text-gray-500 dark:text-gray-400" active-class="text-indigo-600 dark:text-indigo-400">
            <font-awesome-icon icon="file-alt" class="text-lg mb-1" />
            {{ t('nav.logs') }}
        </router-link>
    </nav>
  </div>
</template>
