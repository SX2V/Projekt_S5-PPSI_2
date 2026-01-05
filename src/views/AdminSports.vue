<script setup lang="ts">
import { ref, onMounted } from 'vue';
import apiClient from '../api/axios';
import { useToastStore } from '../stores/toast';
import type { Sport } from '../types/api';
import BaseModal from '../components/BaseModal.vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faEdit, faTrash, faPlus } from '@fortawesome/free-solid-svg-icons';

const sports = ref<Sport[]>([]);
const isLoading = ref(false);
const isModalOpen = ref(false);
const isEditing = ref(false);
const toast = useToastStore();

const form = ref<Sport>({
  name: '',
  description: '',
  typicalDistanceKm: 5
});

const fetchSports = async () => {
  isLoading.value = true;
  try {
    const response = await apiClient.get<Sport[]>('/Sports');
    sports.value = response.data;
  } catch (error) {
    console.error('Failed to fetch sports', error);
    toast.error('Failed to load sports');
  } finally {
    isLoading.value = false;
  }
};

const openAddModal = () => {
  isEditing.value = false;
  form.value = { name: '', description: '', typicalDistanceKm: 5 };
  isModalOpen.value = true;
};

const openEditModal = (sport: Sport) => {
  isEditing.value = true;
  form.value = { ...sport };
  isModalOpen.value = true;
};

const handleSubmit = async () => {
  if (!form.value.name) {
      toast.error('Name is required');
      return;
  }

  try {
    if (isEditing.value && form.value.id) {
      await apiClient.put(`/Sports/${form.value.id}`, form.value);
      toast.success('Sport updated');
    } else {
      await apiClient.post('/Sports', form.value);
      toast.success('Sport added');
    }
    isModalOpen.value = false;
    fetchSports();
  } catch (error) {
    console.error('Failed to save sport', error);
    toast.error('Failed to save sport');
  }
};

const deleteSport = async (id: string) => {
  if (!confirm('Are you sure? This will remove the sport for all users.')) return;
  
  try {
    await apiClient.delete(`/Sports/${id}`);
    toast.success('Sport deleted');
    fetchSports();
  } catch (error) {
    console.error('Failed to delete sport', error);
    toast.error('Failed to delete sport');
  }
};

onMounted(() => {
  fetchSports();
});
</script>

<template>
  <div>
    <div class="flex justify-between items-center mb-6">
      <h2 class="text-2xl font-bold text-gray-900 dark:text-white">Sports Management</h2>
      <button 
        @click="openAddModal"
        class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
      >
        <font-awesome-icon :icon="faPlus" class="mr-2" />
        Add Sport
      </button>
    </div>

    <div class="bg-white dark:bg-gray-800 shadow overflow-hidden sm:rounded-lg transition-colors duration-200">
      <div v-if="isLoading" class="flex justify-center py-12">
        <font-awesome-icon icon="spinner" spin class="text-indigo-500 dark:text-indigo-400 text-3xl" />
      </div>

      <div v-else-if="sports.length === 0" class="text-center py-12 text-gray-500 dark:text-gray-400">
        <p>No sports found.</p>
      </div>

      <table v-else class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-700">
          <tr>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
              Name
            </th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
              Description
            </th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
              Default Distance
            </th>
            <th scope="col" class="relative px-6 py-3">
              <span class="sr-only">Actions</span>
            </th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="sport in sports" :key="sport.id">
            <td class="px-6 py-4 whitespace-nowrap">
              <div class="text-sm font-medium text-gray-900 dark:text-white">{{ sport.name }}</div>
            </td>
            <td class="px-6 py-4">
              <div class="text-sm text-gray-500 dark:text-gray-400 truncate max-w-xs">{{ sport.description }}</div>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <div class="text-sm text-gray-500 dark:text-gray-400">{{ sport.typicalDistanceKm }} km</div>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button @click="openEditModal(sport)" class="text-indigo-600 hover:text-indigo-900 dark:text-indigo-400 dark:hover:text-indigo-300 mr-4">
                <font-awesome-icon :icon="faEdit" />
              </button>
              <button @click="sport.id && deleteSport(sport.id)" class="text-red-600 hover:text-red-900 dark:text-red-400 dark:hover:text-red-300">
                <font-awesome-icon :icon="faTrash" />
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Modal -->
    <BaseModal 
      :is-open="isModalOpen" 
      :title="isEditing ? 'Edit Sport' : 'Add New Sport'"
      @close="isModalOpen = false"
    >
      <form @submit.prevent="handleSubmit" class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Name</label>
          <input v-model="form.name" type="text" required class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Description</label>
          <textarea v-model="form.description" rows="3" class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"></textarea>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Typical Distance (km)</label>
          <input v-model.number="form.typicalDistanceKm" type="number" min="0" class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
        </div>

        <div class="mt-5 sm:mt-6 flex justify-end space-x-3">
          <button 
            type="button" 
            @click="isModalOpen = false"
            class="px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
          >
            Cancel
          </button>
          <button 
            type="submit" 
            class="px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
          >
            {{ isEditing ? 'Save Changes' : 'Add Sport' }}
          </button>
        </div>
      </form>
    </BaseModal>
  </div>
</template>
