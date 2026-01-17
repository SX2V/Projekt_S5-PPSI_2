<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue';
import { useAuthStore } from '../stores/auth';
import { useToastStore } from '../stores/toast';
import apiClient from '../api/axios';
import { useI18n } from 'vue-i18n';
import type { 
  UpdateProfileDto, 
  UserSportDto, 
  Sport, 
  AddSportDto,
  User
} from '../types/api';
import BaseModal from '../components/BaseModal.vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faCamera } from '@fortawesome/free-solid-svg-icons';
import { AxiosError } from 'axios';

const authStore = useAuthStore();
const toast = useToastStore();
const { t } = useI18n();

const localUser = ref<User | null>(null);

const isEditingProfile = ref(false);
const isLoadingSports = ref(false);
const isUploadingPhoto = ref(false);
const userSports = ref<UserSportDto[]>([]);
const availableSports = ref<Sport[]>([]);
const isAddSportModalOpen = ref(false);
const newSportId = ref('');
const fileInput = ref<HTMLInputElement | null>(null);
const imageRefreshKey = ref(Date.now());

const profileForm = ref<UpdateProfileDto>({
  name: '',
  email: '',
  age: null,
  description: ''
});

const profilePictureUrl = computed(() => {
    if (!localUser.value?.profilePicturePath) return null;
    
    let url = localUser.value.profilePicturePath;
    
    if (!url.startsWith('http')) {
        const baseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5254'; 
        const cleanBase = baseUrl.replace(/\/$/, '');
        const cleanPath = url.replace(/^\//, '');
        url = `${cleanBase}/${cleanPath}`;
    }
    
    return `${url}?t=${imageRefreshKey.value}`;
});

const fetchUserSports = async () => {
  if (!localUser.value?.id) return;
  isLoadingSports.value = true;
  try {
    const response = await apiClient.get<UserSportDto[]>(`/Users/${localUser.value.id}/assigned-sports`, {
        params: { _t: Date.now() }
    });
    userSports.value = response.data;
  } catch (error) {
    console.error('Failed to fetch user sports', error);
    toast.error(t('match.loadSportsFailed'));
  } finally {
    isLoadingSports.value = false;
  }
};

const fetchAvailableSports = async () => {
  try {
    const response = await apiClient.get<Sport[]>('/Sports');
    availableSports.value = response.data;
  } catch (error) {
    console.error('Failed to fetch sports', error);
  }
};

const initProfileForm = () => {
  if (localUser.value) {
    profileForm.value = {
      name: localUser.value.name,
      email: localUser.value.email,
      age: localUser.value.age,
      description: localUser.value.description
    };
  }
};

watch(() => authStore.user, (newUser) => {
    if (newUser) {
        localUser.value = { ...newUser }; 
        initProfileForm();
    }
}, { immediate: true, deep: true });

const updateProfile = async () => {
  try {
    const payload: any = {};
    
    if (profileForm.value.name) payload.name = profileForm.value.name;
    if (profileForm.value.email) payload.email = profileForm.value.email;
    
    if (profileForm.value.age !== null && profileForm.value.age !== undefined && profileForm.value.age !== 0) {
        payload.age = Number(profileForm.value.age);
    } else {
        payload.age = null;
    }

    if (profileForm.value.description !== undefined) {
        payload.description = profileForm.value.description;
    }
    
    await apiClient.patch('/Users/me/profile', payload);
    
    await authStore.fetchUser();
    
    isEditingProfile.value = false;
    toast.success(t('profile.profileUpdated'));
  } catch (error) {
    console.error('Failed to update profile', error);
    const axiosError = error as AxiosError;
    
    if (axiosError.response?.data) {
        console.log('Backend validation errors:', axiosError.response.data);
    }

    const msg = (axiosError.response?.data as any)?.title || t('profile.updateFailed');
    toast.error(msg);
  }
};

const toggleAvailability = async () => {
  if (!localUser.value) return;
  
  const oldStatus = localUser.value.isAvailableNow;
  const newStatus = !oldStatus;
  
  try {
    await apiClient.patch('/Users/status', newStatus, {
        headers: { 'Content-Type': 'application/json' }
    });
    
    await authStore.fetchUser();
    
    if (newStatus) {
        toast.success(t('profile.nowOnline'));
    } else {
        toast.info(t('profile.nowOffline'));
    }
  } catch (error) {
    console.error('Failed to update status', error);
    toast.error(t('profile.statusChangeFailed'));
  }
};

const triggerFileInput = () => {
  fileInput.value?.click();
};

const handleFileUpload = async (event: Event) => {
  const target = event.target as HTMLInputElement;
  const file = target.files?.[0];
  
  if (!file) return;

  const formData = new FormData();
  formData.append('file', file);

  isUploadingPhoto.value = true;
  try {
    await apiClient.post('/Users/upload-profile-picture', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    });
    
    imageRefreshKey.value = Date.now();
    
    await authStore.fetchUser(); 
    
    toast.success(t('profile.pictureUpdated'));
  } catch (error) {
    console.error('Failed to upload photo', error);
    toast.error(t('profile.uploadFailed'));
  } finally {
    isUploadingPhoto.value = false;
    if (fileInput.value) fileInput.value.value = '';
  }
};

const addSport = async () => {
  if (!newSportId.value) return;
  
  const selectedSport = availableSports.value.find(s => s.id === newSportId.value);
  const defaultDistance = selectedSport?.typicalDistanceKm || 5;

  const payload: AddSportDto = {
    sportId: newSportId.value,
    typicalDistanceKm: defaultDistance
  };

  try {
    await apiClient.post('/Users/me/sports', payload);
    await fetchUserSports();
    isAddSportModalOpen.value = false;
    newSportId.value = '';
    toast.success(t('profile.sportAdded'));
  } catch (error) {
    console.error('Failed to add sport', error);
    toast.error(t('profile.addSportFailed'));
  }
};

const removeSport = async (sportId: string) => {
  if (!confirm(t('profile.confirmRemoveSport'))) return;
  try {
    await apiClient.delete(`/Users/me/sports/${sportId}`);
    await fetchUserSports();
    toast.success(t('profile.sportRemoved'));
  } catch (error) {
    console.error('Failed to remove sport', error);
    toast.error(t('profile.removeSportFailed'));
  }
};

onMounted(async () => {
  if (!authStore.user) {
    await authStore.fetchUser();
  }
  await fetchUserSports();
  await fetchAvailableSports();
});
</script>

<template>
  <div class="min-h-screen bg-gray-100 dark:bg-gray-900 pb-20 transition-colors duration-200">
    <!-- Header / Cover -->
    <div class="bg-indigo-600 dark:bg-indigo-800 h-40 w-full relative transition-colors duration-200"> 
        <div class="absolute -bottom-20 left-1/2 transform -translate-x-1/2 group cursor-pointer" @click="triggerFileInput">
             <div class="h-40 w-40 rounded-full bg-white dark:bg-gray-800 p-1 shadow-lg relative transition-colors duration-200"> 
                 <div class="h-full w-full rounded-full bg-gray-300 dark:bg-gray-600 flex items-center justify-center overflow-hidden relative">
                     <img v-if="profilePictureUrl" :src="profilePictureUrl" alt="Profile" class="h-full w-full object-cover" />
                     <font-awesome-icon v-else icon="user" class="text-gray-500 dark:text-gray-400 text-6xl" /> 
                     
                     <!-- Overlay for upload -->
                     <div class="absolute inset-0 bg-black bg-opacity-0 group-hover:bg-opacity-30 flex items-center justify-center transition-all duration-200">
                        <font-awesome-icon v-if="isUploadingPhoto" icon="spinner" spin class="text-white text-3xl" />
                        <font-awesome-icon v-else :icon="faCamera" class="text-white text-3xl opacity-0 group-hover:opacity-100" />
                     </div>
                 </div>
             </div>
             <!-- Hidden File Input -->
             <input 
                type="file" 
                ref="fileInput" 
                class="hidden" 
                accept="image/*" 
                @change="handleFileUpload"
             />
        </div>
    </div>

    <div class="mt-24 px-4 sm:px-6 lg:px-8 max-w-3xl mx-auto"> 
      <!-- User Info & Status -->
      <div class="bg-white dark:bg-gray-800 rounded-xl shadow-sm p-6 mb-6 text-center transition-colors duration-200"> 
        <div class="flex flex-col items-center">
            <h1 class="text-3xl font-bold text-gray-900 dark:text-white">{{ localUser?.name }}</h1>
            <p class="text-gray-500 dark:text-gray-400 text-sm mb-4">{{ localUser?.email }}</p>
            
            <button 
                @click="toggleAvailability"
                class="flex items-center space-x-2 px-6 py-2 rounded-full transition-colors duration-200 border"
                :class="localUser?.isAvailableNow 
                ? 'bg-green-100 text-green-800 border-green-200 hover:bg-green-200 dark:bg-green-900 dark:text-green-200 dark:border-green-800 dark:hover:bg-green-800' 
                : 'bg-gray-100 text-gray-800 border-gray-200 hover:bg-gray-200 dark:bg-gray-700 dark:text-gray-300 dark:border-gray-600 dark:hover:bg-gray-600'"
            >
                <font-awesome-icon :icon="localUser?.isAvailableNow ? 'toggle-on' : 'toggle-off'" class="text-lg" />
                <span class="text-sm font-medium">{{ localUser?.isAvailableNow ? t('common.online') : t('common.offline') }}</span>
            </button>
        </div>

        <!-- Profile Details -->
        <div class="mt-8 border-t border-gray-100 dark:border-gray-700 pt-6 text-left"> 
          <div class="flex justify-between items-center mb-4">
            <h2 class="text-lg font-semibold text-gray-900 dark:text-white">{{ t('profile.aboutMe') }}</h2>
            <button 
              v-if="!isEditingProfile"
              @click="isEditingProfile = true"
              class="text-indigo-600 dark:text-indigo-400 hover:text-indigo-800 dark:hover:text-indigo-300 text-sm font-medium flex items-center"
            >
              <font-awesome-icon icon="edit" class="mr-1" /> {{ t('common.edit') }}
            </button>
          </div>

          <div v-if="!isEditingProfile">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <span class="text-gray-500 dark:text-gray-400 text-sm block">{{ t('match.age') }}</span>
                <span class="text-gray-900 dark:text-white">{{ localUser?.age || t('profile.notSpecified') }}</span>
              </div>
              <div class="sm:col-span-2">
                <span class="text-gray-500 dark:text-gray-400 text-sm block">{{ t('profile.description') }}</span>
                <p class="text-gray-900 dark:text-white mt-1 whitespace-pre-wrap">{{ localUser?.description || t('profile.noDescriptionYet') }}</p>
              </div>
            </div>
          </div>

          <!-- Edit Form -->
          <form v-else @submit.prevent="updateProfile" class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">{{ t('common.name') }}</label>
              <input v-model="profileForm.name" type="text" required class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">{{ t('match.age') }}</label>
              <input v-model.number="profileForm.age" type="number" min="1" max="120" class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">{{ t('profile.description') }}</label>
              <textarea v-model="profileForm.description" rows="3" class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"></textarea>
            </div>
            <div class="flex justify-end space-x-3">
              <button type="button" @click="isEditingProfile = false" class="px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700">{{ t('common.cancel') }}</button>
              <button type="submit" class="px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600">{{ t('common.save') }}</button>
            </div>
          </form>
        </div>
      </div>

      <!-- Sports Section -->
      <div class="bg-white dark:bg-gray-800 rounded-xl shadow-sm p-6 transition-colors duration-200">
        <div class="flex justify-between items-center mb-6">
          <h2 class="text-lg font-semibold text-gray-900 dark:text-white">{{ t('profile.mySports') }}</h2>
          <button 
            @click="isAddSportModalOpen = true"
            class="inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-medium rounded-full shadow-sm text-white bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
          >
            <font-awesome-icon icon="plus" class="mr-1" /> {{ t('profile.addSport') }}
          </button>
        </div>

        <div v-if="isLoadingSports" class="text-center py-4">
          <font-awesome-icon icon="spinner" spin class="text-indigo-500 dark:text-indigo-400 text-2xl" />
        </div>

        <div v-else-if="userSports.length === 0" class="text-center py-8 text-gray-500 dark:text-gray-400">
          <font-awesome-icon icon="running" class="text-4xl mb-2 text-gray-300 dark:text-gray-600" />
          <p>{{ t('profile.noSportsAdded') }}</p>
        </div>

        <div v-else class="space-y-4">
          <div v-for="sport in userSports" :key="sport.sportId" class="flex items-center justify-between p-4 bg-gray-50 dark:bg-gray-700 rounded-lg border border-gray-100 dark:border-gray-600">
            <div class="flex-1">
              <h3 class="font-medium text-gray-900 dark:text-white">{{ sport.name }}</h3>
              <div v-if="sport.typicalDistanceKm && sport.typicalDistanceKm > 0" class="flex items-center mt-1 text-sm text-gray-500 dark:text-gray-400">
                 <font-awesome-icon icon="map-marker-alt" class="mr-1.5 text-gray-400 dark:text-gray-500" />
                 <span>{{ t('profile.typicalDistance') }}: {{ sport.typicalDistanceKm }} km</span>
              </div>
            </div>
            <button 
              @click="sport.sportId && removeSport(sport.sportId)"
              class="ml-4 p-2 text-gray-400 hover:text-red-500 dark:text-gray-500 dark:hover:text-red-400 transition-colors"
              :title="t('profile.removeSport')"
            >
              <font-awesome-icon icon="trash" />
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Add Sport Modal -->
    <BaseModal 
      :is-open="isAddSportModalOpen" 
      :title="t('profile.addNewSport')"
      @close="isAddSportModalOpen = false"
    >
      <form @submit.prevent="addSport" class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">{{ t('profile.selectSport') }}</label>
          <select 
            v-model="newSportId" 
            required
            class="mt-1 block w-full pl-3 pr-10 py-2 text-base border-gray-300 dark:border-gray-600 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm rounded-md border bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
          >
            <option value="" disabled>{{ t('profile.chooseSport') }}</option>
            <option 
              v-for="sport in availableSports" 
              :key="sport.id" 
              :value="sport.id"
              :disabled="userSports.some(us => us.sportId === sport.id)"
            >
              {{ sport.name }} {{ (sport.typicalDistanceKm && sport.typicalDistanceKm > 0) ? `(${sport.typicalDistanceKm} km)` : '' }} {{ userSports.some(us => us.sportId === sport.id) ? `(${t('profile.alreadyAdded')})` : '' }}
            </option>
          </select>
        </div>
        
        <div class="mt-5 sm:mt-6">
          <button 
            type="submit" 
            class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-indigo-600 text-base font-medium text-white hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:text-sm"
            :disabled="!newSportId"
          >
            {{ t('profile.addSport') }}
          </button>
        </div>
      </form>
    </BaseModal>
  </div>
</template>
