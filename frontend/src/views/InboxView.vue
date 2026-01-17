<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import apiClient from '../api/axios';
import { useAuthStore } from '../stores/auth';
import { useToastStore } from '../stores/toast';
import { useI18n } from 'vue-i18n';
import type { MatchRequestViewDto, MatchRequestStatusDto, User, UserSportDto, CreateTrainingRequestDto, UpdateTrainingRequestStatusDto, Sport } from '../types/api';
import { MatchRequestStatusEnum, TrainingRequestStatusEnum } from '../types/enums';
import BaseModal from '../components/BaseModal.vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faUser, faRunning, faEnvelope, faCommentDots, faTrash, faDumbbell, faTimes, faHandshake } from '@fortawesome/free-solid-svg-icons';
import { AxiosError } from 'axios';

const authStore = useAuthStore();
const toast = useToastStore();
const { t } = useI18n();

interface TrainingRequestViewDto {
    id: string;
    senderId: string;
    receiverId: string;
    status: string | number; 
    createdAt: string;
    trainingDateTime?: string;
    location?: string;
    message?: string;
}

const activeTab = ref<'matches' | 'trainings' | 'connections'>('matches');
const incomingRequests = ref<MatchRequestViewDto[]>([]);
const outgoingRequests = ref<MatchRequestViewDto[]>([]);
const connections = ref<MatchRequestViewDto[]>([]); 
const incomingTrainingRequests = ref<TrainingRequestViewDto[]>([]);
const outgoingTrainingRequests = ref<TrainingRequestViewDto[]>([]);
const isLoading = ref(false);
const processingId = ref<string | null>(null);
const userDetails = ref<Record<string, User>>({}); 
const userPictures = ref<Record<string, string | null>>({}); 
const sportsDistances = ref<Record<string, number>>({}); 

let pollingInterval: ReturnType<typeof setInterval> | null = null;

const isProfileModalOpen = ref(false);
const selectedUserProfile = ref<User | null>(null);
const selectedUserSports = ref<UserSportDto[]>([]);
const isLoadingProfile = ref(false);
const selectedUserProfilePic = ref<string | null>(null);
const selectedRequestStatus = ref<string | null>(null); 
const selectedRequestId = ref<string | null>(null);
const selectedIsOutgoing = ref(false);

const isTrainingModalOpen = ref(false);
const trainingForm = ref({
    date: '',
    time: '',
    location: '',
    message: ''
});

const fetchUserDetails = async (userId: string) => {
    if (userDetails.value[userId]) return; 
    try {
        const response = await apiClient.get<User>(`/Users/${userId}`);
        userDetails.value[userId] = response.data;
        const picUrl = await fetchProfilePicture(userId);
        userPictures.value[userId] = picUrl;
    } catch (error) {
        console.error(`Failed to fetch details for user ${userId}`, error);
    }
};

const fetchProfilePicture = async (userId: string): Promise<string | null> => {
    try {
        const response = await apiClient.get<any>(`/Users/profile-picture-url/${userId}`);
        let url = response.data;
        if (typeof url === 'object' && url.url) url = url.url; 
        
        if (url && typeof url === 'string') {
             if (!url.startsWith('http')) {
                const baseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5254'; 
                const cleanBase = baseUrl.replace(/\/$/, '');
                const cleanPath = url.replace(/^\//, '');
                return `${cleanBase}/${cleanPath}`;
            }
            return url;
        }
        return null;
    } catch (e) {
        return null;
    }
};

const fetchSports = async () => {
    try {
        const response = await apiClient.get<Sport[]>('/Sports');
        const map: Record<string, number> = {};
        response.data.forEach(s => {
            if (s.id && s.typicalDistanceKm) {
                map[s.id] = s.typicalDistanceKm;
            }
        });
        sportsDistances.value = map;
    } catch (error) {
        console.error('Failed to fetch sports', error);
    }
};

const sortByDateDesc = (a: { createdAt?: string }, b: { createdAt?: string }) => {
    const dateA = a.createdAt ? new Date(a.createdAt).getTime() : 0;
    const dateB = b.createdAt ? new Date(b.createdAt).getTime() : 0;
    return dateB - dateA;
};

const fetchMatchRequests = async (background = false) => {
  if (!authStore.user?.id) return;
  try {
    const [inRes, outRes] = await Promise.all([
        apiClient.get<MatchRequestViewDto[]>('/Match/incoming', { params: { userId: authStore.user.id } }),
        apiClient.get<MatchRequestViewDto[]>('/Match/outgoing', { params: { userId: authStore.user.id } })
    ]);
    
    incomingRequests.value = inRes.data
        .filter(req => {
            const s = String(req.status);
            return s !== 'Cancelled' && s !== '3';
        })
        .sort(sortByDateDesc);

    outgoingRequests.value = outRes.data.sort(sortByDateDesc);
    
    incomingRequests.value.forEach(req => { if (req.fromUserId) fetchUserDetails(req.fromUserId); });
    outgoingRequests.value.forEach(req => { if (req.toUserId) fetchUserDetails(req.toUserId); });

  } catch (error) {
    if (!background) console.error('Failed to fetch match requests', error);
  }
};

const fetchConnections = async (background = false) => {
    if (!authStore.user?.id) return;
    try {
        const response = await apiClient.get<MatchRequestViewDto[]>('/Match/history', { params: { userId: authStore.user.id } });
        connections.value = response.data.sort(sortByDateDesc);

        connections.value.forEach(conn => {
            const partnerId = getPartnerId(conn);
            if (partnerId) fetchUserDetails(partnerId);
        });
    } catch (error) {
        if (!background) console.error('Failed to fetch connections', error);
    }
};

const fetchTrainingRequests = async (background = false) => {
    try {
        const [inRes, outRes] = await Promise.all([
            apiClient.get<TrainingRequestViewDto[]>('/TrainingRequests/me/received'),
            apiClient.get<TrainingRequestViewDto[]>('/TrainingRequests/me/sent')
        ]);

        incomingTrainingRequests.value = inRes.data
            .filter(req => {
                const s = String(req.status);
                return s !== 'Cancelled' && s !== '3';
            })
            .sort(sortByDateDesc);

        outgoingTrainingRequests.value = outRes.data.sort(sortByDateDesc);

        incomingTrainingRequests.value.forEach(req => { if (req.senderId) fetchUserDetails(req.senderId); });
        outgoingTrainingRequests.value.forEach(req => { if (req.receiverId) fetchUserDetails(req.receiverId); });

    } catch (error) {
        if (!background) console.error('Failed to fetch training requests', error);
    }
};

const fetchAll = async (background = false) => {
  if (!background) isLoading.value = true;
  await Promise.all([
      fetchMatchRequests(background), 
      fetchConnections(background),
      fetchTrainingRequests(background)
  ]);
  if (!background) isLoading.value = false;
};

const respondToMatchRequest = async (requestId: string, status: MatchRequestStatusEnum) => {
  if (!requestId) return;
  processingId.value = requestId;
  const payload: MatchRequestStatusDto = { status };
  try {
    await apiClient.patch(`/Match/${requestId}`, payload);
    toast.success(status === MatchRequestStatusEnum.Accepted ? t('inbox.requestAccepted') : t('inbox.requestDeclined'));
    await fetchAll(); 
  } catch (error) {
    toast.error(t('inbox.respondFailed'));
  } finally {
    processingId.value = null;
  }
};

const cancelMatchRequest = async (requestId: string) => {
    if (!requestId) return;
    if (!confirm(t('inbox.confirmCancelRequest'))) return;
    
    processingId.value = requestId;
    try {
        await apiClient.patch(`/Match/${requestId}/cancel`);
        toast.success(t('match.requestCancelled'));
        await fetchMatchRequests();
    } catch (error) {
        console.error('Failed to cancel request', error);
        toast.error(t('match.cancelRequestFailed'));
    } finally {
        processingId.value = null;
    }
};

const respondToTrainingRequest = async (requestId: string, status: TrainingRequestStatusEnum) => {
    if (!requestId) return;
    processingId.value = requestId;
    
    const payload: UpdateTrainingRequestStatusDto = { status: status };
    
    console.log(`Responding to training request ${requestId} with status ${status}`);

    try {
        await apiClient.patch(`/TrainingRequests/${requestId}`, payload);
        toast.success(status === TrainingRequestStatusEnum.Accepted ? t('inbox.trainingAccepted') : t('inbox.trainingDeclined'));
        await fetchTrainingRequests();
    } catch (error) {
        console.error('Failed to respond to training request', error);
        const axiosError = error as AxiosError;
        if (axiosError.response?.data) {
             console.log('Backend error:', axiosError.response.data);
        }
        toast.error(t('inbox.respondTrainingFailed'));
    } finally {
        processingId.value = null;
    }
};

const cancelTrainingRequest = async (requestId: string) => {
    if (!requestId) return;
    if (!confirm(t('inbox.confirmCancelTraining'))) return;
    
    processingId.value = requestId;
    try {
        await apiClient.patch(`/TrainingRequests/${requestId}/cancel`);
        toast.success(t('match.requestCancelled'));
        await fetchTrainingRequests();
    } catch (error) {
        console.error('Failed to cancel training request', error);
        toast.error(t('match.cancelRequestFailed'));
    } finally {
        processingId.value = null;
    }
};

const openTrainingModal = () => {
    trainingForm.value = { date: '', time: '', location: '', message: '' };
    isTrainingModalOpen.value = true;
};

const submitTrainingRequest = async () => {
    if (!selectedUserProfile.value?.id) return;
    
    if (trainingForm.value.date && trainingForm.value.time) {
        const selectedDateTime = new Date(`${trainingForm.value.date}T${trainingForm.value.time}`);
        if (selectedDateTime < new Date()) {
            toast.error(t('inbox.pastDateError'));
            return;
        }
    }

    let dateTime = null;
    if (trainingForm.value.date && trainingForm.value.time) {
        const d = new Date(`${trainingForm.value.date}T${trainingForm.value.time}`);
        dateTime = new Date(d.getTime() - (d.getTimezoneOffset() * 60000)).toISOString().slice(0, -1);
    }

    const payload: CreateTrainingRequestDto = { 
        receiverId: selectedUserProfile.value.id,
        trainingDateTime: dateTime,
        location: trainingForm.value.location,
        message: trainingForm.value.message
    };
    
    console.log('Sending training request payload:', payload);

    try {
        await apiClient.post('/TrainingRequests', payload);
        toast.success(t('match.trainingRequestSent'));
        isTrainingModalOpen.value = false;
        isProfileModalOpen.value = false;
        fetchTrainingRequests();
    } catch (error) {
        console.error('Failed to send training request', error);
        const axiosError = error as AxiosError;
        if (axiosError.response?.data) {
            console.log('Backend error details:', axiosError.response.data);
        }
        toast.error(t('match.trainingRequestFailed'));
    }
};

const openUserProfile = async (userId: string, status: string | null, requestId: string | null = null, isOutgoing: boolean = false) => {
    isProfileModalOpen.value = true;
    isLoadingProfile.value = true;
    selectedUserProfile.value = null;
    selectedUserSports.value = [];
    selectedUserProfilePic.value = null;
    selectedRequestStatus.value = status;
    selectedRequestId.value = requestId;
    selectedIsOutgoing.value = isOutgoing;

    try {
        if (userDetails.value[userId]) {
            selectedUserProfile.value = userDetails.value[userId];
        } else {
            const userResponse = await apiClient.get<User>(`/Users/${userId}`);
            selectedUserProfile.value = userResponse.data;
        }

        const sportsResponse = await apiClient.get<UserSportDto[]>(`/Users/${userId}/assigned-sports`);
        selectedUserSports.value = sportsResponse.data;

        if (userPictures.value[userId] !== undefined) {
            selectedUserProfilePic.value = userPictures.value[userId];
        } else {
            const picUrl = await fetchProfilePicture(userId);
            selectedUserProfilePic.value = picUrl;
        }

    } catch (error) {
        console.error("Failed to fetch user profile", error);
        toast.error(t('match.loadProfileFailed'));
        isProfileModalOpen.value = false;
    } finally {
        isLoadingProfile.value = false;
    }
};

const getPartnerId = (req: MatchRequestViewDto) => {
    if (!authStore.user?.id) return null;
    return req.fromUserId === authStore.user.id ? req.toUserId : req.fromUserId;
};

const formatDate = (dateString?: string) => {
  if (!dateString) return '';
  return new Date(dateString).toLocaleDateString(undefined, {
    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
  });
};

const getStatusColor = (status?: string | number | null) => {
    if (status === null || status === undefined) return 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300';
    
    const s = String(status).toLowerCase();
    if (s.includes('accept') || s === '1') return 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200';
    if (s.includes('reject') || s === '2') return 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200';
    if (s.includes('cancel') || s === '3') return 'bg-gray-200 text-gray-800 dark:bg-gray-600 dark:text-gray-300';
    return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200'; 
};

const getStatusText = (status?: string | number | null) => {
    if (status === null || status === undefined) return 'Unknown';
    const s = String(status);
    if (s === '0' || s === 'Pending') return t('match.requestPending');
    if (s === '1' || s === 'Accepted') return t('match.connected');
    if (s === '2' || s === 'Rejected') return t('match.rejected');
    if (s === '3' || s === 'Cancelled') return t('match.requestCancelled');
    return s;
};

const isPending = (status?: string | number | null) => {
    if (status === null || status === undefined) return false;
    const s = String(status);
    return s === '0' || s === 'Pending';
};

const sendMessage = () => {
    if (selectedUserProfile.value?.email) {
        window.location.href = `mailto:${selectedUserProfile.value.email}`;
    }
};

onMounted(async () => {
  if (!authStore.user) {
    await authStore.fetchUser();
  }
  await fetchSports(); 
  fetchAll();

  pollingInterval = setInterval(() => {
      fetchAll(true);
  }, 10000);
});

onUnmounted(() => {
    if (pollingInterval) clearInterval(pollingInterval);
});
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 pb-20 transition-colors duration-200">
    <!-- Header -->
    <div class="bg-white dark:bg-gray-800 shadow-sm sticky top-0 z-10 transition-colors duration-200">
      <div class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between h-16 items-center">
          <h1 class="text-xl font-bold text-gray-900 dark:text-white">{{ t('nav.inbox') }}</h1>
          <button @click="fetchAll(false)" class="text-gray-500 dark:text-gray-400 hover:text-indigo-600 dark:hover:text-indigo-400">
              <font-awesome-icon icon="sync" :spin="isLoading" />
          </button>
        </div>
        
        <!-- Tabs -->
        <div class="flex border-b border-gray-200 dark:border-gray-700">
          <button 
            @click="activeTab = 'matches'"
            class="flex-1 py-4 px-1 text-center border-b-2 font-medium text-sm focus:outline-none transition-colors"
            :class="activeTab === 'matches' ? 'border-indigo-500 text-indigo-600 dark:text-indigo-400' : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 hover:border-gray-300 dark:hover:border-gray-600'"
          >
            {{ t('inbox.matchRequests') }}
          </button>
          <button 
            @click="activeTab = 'connections'"
            class="flex-1 py-4 px-1 text-center border-b-2 font-medium text-sm focus:outline-none transition-colors"
            :class="activeTab === 'connections' ? 'border-indigo-500 text-indigo-600 dark:text-indigo-400' : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 hover:border-gray-300 dark:hover:border-gray-600'"
          >
            {{ t('inbox.connections') }}
          </button>
          <button 
            @click="activeTab = 'trainings'"
            class="flex-1 py-4 px-1 text-center border-b-2 font-medium text-sm focus:outline-none transition-colors"
            :class="activeTab === 'trainings' ? 'border-indigo-500 text-indigo-600 dark:text-indigo-400' : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 hover:border-gray-300 dark:hover:border-gray-600'"
          >
            {{ t('inbox.trainingRequests') }}
          </button>
        </div>
      </div>
    </div>

    <!-- Content -->
    <div class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
      
      <div v-if="isLoading" class="flex justify-center py-12">
        <font-awesome-icon icon="spinner" spin class="text-indigo-500 dark:text-indigo-400 text-3xl" />
      </div>

      <!-- Match Requests -->
      <div v-if="activeTab === 'matches'">
        <h3 class="text-lg font-semibold mb-4 text-gray-900 dark:text-white">{{ t('inbox.incoming') }}</h3>
        <div v-if="incomingRequests.length === 0" class="text-center py-8 text-gray-500 dark:text-gray-400">
          <p>{{ t('inbox.noIncomingMatch') }}</p>
        </div>
        <div v-else class="space-y-4">
          <div v-for="req in incomingRequests" :key="req.id" class="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-100 dark:border-gray-700 p-4 transition-all hover:shadow-md cursor-pointer" @click="req.fromUserId && openUserProfile(req.fromUserId, req.status || null, req.id, false)">
            <div class="flex justify-between items-start mb-3">
              <div class="flex items-start">
                <div class="h-12 w-12 rounded-full bg-gray-200 dark:bg-gray-600 flex-shrink-0 overflow-hidden mr-3 mt-1">
                    <img v-if="req.fromUserId && userPictures[req.fromUserId]" :src="userPictures[req.fromUserId]!" alt="Avatar" class="h-full w-full object-cover" />
                    <div v-else class="h-full w-full flex items-center justify-center text-gray-500 dark:text-gray-300"><font-awesome-icon :icon="faUser" /></div>
                </div>
                <div>
                    <h3 class="text-lg font-semibold text-gray-900 dark:text-white">{{ t('inbox.matchRequest') }}</h3>
                    <p class="text-sm text-gray-500 dark:text-gray-400">{{ t('inbox.from') }}: <span class="font-medium text-gray-900 dark:text-white">{{ userDetails[req.fromUserId!]?.name || t('common.loading') }}</span></p>
                    <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">{{ formatDate(req.createdAt) }}</p>
                </div>
              </div>
              <span class="px-2 py-1 rounded-full text-xs font-medium" :class="getStatusColor(req.status)">{{ getStatusText(req.status) }}</span>
            </div>
            <div v-if="isPending(req.status)" class="flex space-x-3 mt-4 border-t border-gray-50 dark:border-gray-700 pt-3" @click.stop>
              <button @click="req.id && respondToMatchRequest(req.id, MatchRequestStatusEnum.Accepted)" :disabled="processingId === req.id" class="flex-1 flex items-center justify-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50">
                <font-awesome-icon v-if="processingId === req.id" icon="spinner" spin class="mr-2" /><font-awesome-icon v-else icon="check" class="mr-2" /> {{ t('inbox.accept') }}
              </button>
              <button @click="req.id && respondToMatchRequest(req.id, MatchRequestStatusEnum.Rejected)" :disabled="processingId === req.id" class="flex-1 flex items-center justify-center px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 hover:bg-gray-50 dark:hover:bg-gray-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50">
                <font-awesome-icon icon="times" class="mr-2 text-red-500" /> {{ t('inbox.decline') }}
              </button>
            </div>
          </div>
        </div>

        <h3 class="text-lg font-semibold mt-8 mb-4 text-gray-900 dark:text-white">{{ t('inbox.sent') }}</h3>
        <div v-if="outgoingRequests.length === 0" class="text-center py-8 text-gray-500 dark:text-gray-400">
          <p>{{ t('inbox.noOutgoingMatch') }}</p>
        </div>
        <div v-else class="space-y-4">
          <div v-for="req in outgoingRequests" :key="req.id" class="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-100 dark:border-gray-700 p-4 cursor-pointer hover:shadow-md" @click="req.toUserId && openUserProfile(req.toUserId, req.status || null, req.id, true)">
            <div class="flex justify-between items-center">
              <div class="flex items-center">
                <div class="h-12 w-12 rounded-full bg-gray-200 dark:bg-gray-600 flex-shrink-0 overflow-hidden mr-3">
                    <img v-if="req.toUserId && userPictures[req.toUserId]" :src="userPictures[req.toUserId]!" alt="Avatar" class="h-full w-full object-cover" />
                    <div v-else class="h-full w-full flex items-center justify-center text-gray-500 dark:text-gray-300"><font-awesome-icon :icon="faUser" /></div>
                </div>
                <div>
                    <h3 class="font-medium text-gray-900 dark:text-white">{{ t('inbox.matchRequest') }}</h3>
                    <p class="text-sm text-gray-500 dark:text-gray-400">{{ t('inbox.to') }}: <span class="font-medium text-gray-900 dark:text-white">{{ userDetails[req.toUserId!]?.name || t('common.loading') }}</span></p>
                    <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">{{ formatDate(req.createdAt) }}</p>
                </div>
              </div>
              <div class="flex items-center space-x-2">
                  <span class="px-2 py-1 rounded-full text-xs font-medium" :class="getStatusColor(req.status)">{{ getStatusText(req.status) }}</span>
                  
                  <!-- Cancel Button for Pending Requests -->
                  <button 
                    v-if="isPending(req.status)"
                    @click.stop="req.id && cancelMatchRequest(req.id)"
                    :disabled="processingId === req.id"
                    class="text-red-500 hover:text-red-700 dark:hover:text-red-400 p-2"
                    :title="t('match.cancelRequest')"
                  >
                    <font-awesome-icon v-if="processingId === req.id" icon="spinner" spin />
                    <font-awesome-icon v-else icon="trash" />
                  </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Connections (History) -->
      <div v-if="activeTab === 'connections'">
        <h3 class="text-lg font-semibold mb-4 text-gray-900 dark:text-white">{{ t('inbox.myConnections') }}</h3>
        <div v-if="connections.length === 0" class="text-center py-8 text-gray-500 dark:text-gray-400">
          <p>{{ t('inbox.noConnections') }}</p>
          <p class="text-sm mt-2">{{ t('inbox.goToMatch') }}</p>
        </div>
        <div v-else class="space-y-4">
          <div v-for="conn in connections" :key="conn.id" class="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-100 dark:border-gray-700 p-4 cursor-pointer hover:shadow-md" @click="getPartnerId(conn) && openUserProfile(getPartnerId(conn)!, 'Accepted', conn.id)">
            <div class="flex justify-between items-center">
              <div class="flex items-center">
                <div class="h-12 w-12 rounded-full bg-gray-200 dark:bg-gray-600 flex-shrink-0 overflow-hidden mr-3">
                    <img v-if="getPartnerId(conn) && userPictures[getPartnerId(conn)!]" :src="userPictures[getPartnerId(conn)!]!" alt="Avatar" class="h-full w-full object-cover" />
                    <div v-else class="h-full w-full flex items-center justify-center text-gray-500 dark:text-gray-300"><font-awesome-icon :icon="faUser" /></div>
                </div>
                <div>
                    <h3 class="font-medium text-gray-900 dark:text-white flex items-center">
                        {{ userDetails[getPartnerId(conn)!]?.name || t('common.loading') }}
                    </h3>
                    <p class="text-sm text-gray-500 dark:text-gray-400">
                        {{ t('inbox.connectedVia') }}: {{ conn.sportName }}
                        <span v-if="sportsDistances[conn.sportId] && sportsDistances[conn.sportId] > 0" class="text-xs text-gray-400 dark:text-gray-500 ml-1">
                            ({{ sportsDistances[conn.sportId] }} km)
                        </span>
                    </p>
                    <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">{{ t('inbox.connectedSince') }}: {{ formatDate(conn.createdAt) }}</p>
                </div>
              </div>
              <div class="text-indigo-500 dark:text-indigo-400">
                  <font-awesome-icon :icon="faHandshake" class="text-xl" />
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Training Requests -->
      <div v-if="activeTab === 'trainings'">
        <h3 class="text-lg font-semibold mb-4 text-gray-900 dark:text-white">{{ t('inbox.incoming') }}</h3>
        <div v-if="incomingTrainingRequests.length === 0" class="text-center py-8 text-gray-500 dark:text-gray-400">
          <p>{{ t('inbox.noIncomingTraining') }}</p>
        </div>
        <div v-else class="space-y-4">
          <div v-for="req in incomingTrainingRequests" :key="req.id" class="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-100 dark:border-gray-700 p-4 transition-all hover:shadow-md cursor-pointer" @click="req.senderId && openUserProfile(req.senderId, 'Accepted')">
            <div class="flex justify-between items-start mb-3">
              <div class="flex items-start">
                <div class="h-12 w-12 rounded-full bg-gray-200 dark:bg-gray-600 flex-shrink-0 overflow-hidden mr-3 mt-1">
                    <img v-if="req.senderId && userPictures[req.senderId]" :src="userPictures[req.senderId]!" alt="Avatar" class="h-full w-full object-cover" />
                    <div v-else class="h-full w-full flex items-center justify-center text-gray-500 dark:text-gray-300"><font-awesome-icon :icon="faUser" /></div>
                </div>
                <div>
                    <h3 class="text-lg font-semibold text-gray-900 dark:text-white flex items-center">
                        {{ t('inbox.trainingInvite') }}
                    </h3>
                    <p class="text-sm text-gray-500 dark:text-gray-400">{{ t('inbox.from') }}: <span class="font-medium text-gray-900 dark:text-white">{{ userDetails[req.senderId]?.name || t('common.loading') }}</span></p>
                    <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">{{ formatDate(req.createdAt) }}</p>
                    
                    <!-- Display Training Details -->
                    <div v-if="req.trainingDateTime || req.location || req.message" class="mt-2 text-sm text-gray-600 dark:text-gray-300 bg-gray-50 dark:bg-gray-700 p-2 rounded">
                        <p v-if="req.trainingDateTime"><strong>{{ t('inbox.when') }}:</strong> {{ new Date(req.trainingDateTime).toLocaleString() }}</p>
                        <p v-if="req.location"><strong>{{ t('inbox.where') }}:</strong> {{ req.location }}</p>
                        <p v-if="req.message"><strong>{{ t('inbox.note') }}:</strong> {{ req.message }}</p>
                    </div>
                </div>
              </div>
              <span class="px-2 py-1 rounded-full text-xs font-medium" :class="getStatusColor(req.status)">{{ getStatusText(req.status) }}</span>
            </div>
            <div v-if="isPending(req.status)" class="flex space-x-3 mt-4 border-t border-gray-50 dark:border-gray-700 pt-3" @click.stop>
              <button @click="req.id && respondToTrainingRequest(req.id, TrainingRequestStatusEnum.Accepted)" :disabled="processingId === req.id" class="flex-1 flex items-center justify-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50">
                <font-awesome-icon v-if="processingId === req.id" icon="spinner" spin class="mr-2" /><font-awesome-icon v-else icon="check" class="mr-2" /> {{ t('inbox.accept') }}
              </button>
              <button @click="req.id && respondToTrainingRequest(req.id, TrainingRequestStatusEnum.Rejected)" :disabled="processingId === req.id" class="flex-1 flex items-center justify-center px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 hover:bg-gray-50 dark:hover:bg-gray-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50">
                <font-awesome-icon icon="times" class="mr-2 text-red-500" /> {{ t('inbox.decline') }}
              </button>
            </div>
          </div>
        </div>

        <h3 class="text-lg font-semibold mt-8 mb-4 text-gray-900 dark:text-white">{{ t('inbox.sent') }}</h3>
        <div v-if="outgoingTrainingRequests.length === 0" class="text-center py-8 text-gray-500 dark:text-gray-400">
          <p>{{ t('inbox.noOutgoingTraining') }}</p>
        </div>
        <div v-else class="space-y-4">
          <div v-for="req in outgoingTrainingRequests" :key="req.id" class="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-100 dark:border-gray-700 p-4 cursor-pointer hover:shadow-md" @click="req.receiverId && openUserProfile(req.receiverId, 'Accepted')">
            <div class="flex justify-between items-center">
              <div class="flex items-center">
                <div class="h-12 w-12 rounded-full bg-gray-200 dark:bg-gray-600 flex-shrink-0 overflow-hidden mr-3">
                    <img v-if="req.receiverId && userPictures[req.receiverId]" :src="userPictures[req.receiverId]!" alt="Avatar" class="h-full w-full object-cover" />
                    <div v-else class="h-full w-full flex items-center justify-center text-gray-500 dark:text-gray-300"><font-awesome-icon :icon="faUser" /></div>
                </div>
                <div>
                    <h3 class="font-medium text-gray-900 dark:text-white flex items-center">
                        {{ t('inbox.trainingInvite') }}
                    </h3>
                    <p class="text-sm text-gray-500 dark:text-gray-400">{{ t('inbox.to') }}: <span class="font-medium text-gray-900 dark:text-white">{{ userDetails[req.receiverId]?.name || t('common.loading') }}</span></p>
                    <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">{{ formatDate(req.createdAt) }}</p>
                    
                    <!-- Display Training Details -->
                    <div v-if="req.trainingDateTime || req.location || req.message" class="mt-2 text-sm text-gray-600 dark:text-gray-300 bg-gray-50 dark:bg-gray-700 p-2 rounded">
                        <p v-if="req.trainingDateTime"><strong>{{ t('inbox.when') }}:</strong> {{ new Date(req.trainingDateTime).toLocaleString() }}</p>
                        <p v-if="req.location"><strong>{{ t('inbox.where') }}:</strong> {{ req.location }}</p>
                        <p v-if="req.message"><strong>{{ t('inbox.note') }}:</strong> {{ req.message }}</p>
                    </div>
                </div>
              </div>
              <div class="flex items-center space-x-2">
                  <span class="px-2 py-1 rounded-full text-xs font-medium" :class="getStatusColor(req.status)">{{ getStatusText(req.status) }}</span>
                  
                  <!-- Cancel Button for Pending Requests -->
                  <button 
                    v-if="isPending(req.status)"
                    @click.stop="req.id && cancelTrainingRequest(req.id)"
                    :disabled="processingId === req.id"
                    class="text-red-500 hover:text-red-700 dark:hover:text-red-400 p-2"
                    :title="t('match.cancelRequest')"
                  >
                    <font-awesome-icon v-if="processingId === req.id" icon="spinner" spin />
                    <font-awesome-icon v-else icon="trash" />
                  </button>
              </div>
            </div>
          </div>
        </div>
      </div>

    </div>

    <!-- User Profile Modal -->
    <BaseModal 
        :is-open="isProfileModalOpen" 
        :title="t('match.userProfile')"
        @close="isProfileModalOpen = false"
    >
        <div v-if="isLoadingProfile" class="flex justify-center py-8">
            <font-awesome-icon icon="spinner" spin class="text-indigo-500 dark:text-indigo-400 text-3xl" />
        </div>
        <div v-else-if="selectedUserProfile" class="text-center">
            <!-- Avatar -->
            <div class="h-24 w-24 rounded-full bg-gray-200 dark:bg-gray-600 mx-auto mb-4 overflow-hidden">
                <img v-if="selectedUserProfilePic" :src="selectedUserProfilePic" alt="Avatar" class="h-full w-full object-cover" />
                <div v-else class="h-full w-full flex items-center justify-center text-gray-500 dark:text-gray-300">
                    <font-awesome-icon :icon="faUser" class="text-3xl" />
                </div>
            </div>

            <h3 class="text-xl font-bold text-gray-900 dark:text-white mb-4">{{ selectedUserProfile.name }}</h3>
            
            <!-- Send Message Button -->
            <button 
                v-if="selectedRequestStatus === 'Accepted'" 
                @click="sendMessage"
                class="w-full flex items-center justify-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 mb-4"
            >
                <font-awesome-icon :icon="faCommentDots" class="mr-2" />
                {{ t('match.sendMessage') }}
            </button>

            <!-- Invite to Training Button -->
            <button 
                v-if="selectedRequestStatus === 'Accepted'" 
                @click="openTrainingModal"
                class="w-full flex items-center justify-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 mb-4"
            >
                <font-awesome-icon :icon="faDumbbell" class="mr-2" />
                {{ t('match.inviteToTraining') }}
            </button>

            <!-- Cancel Request Button in Modal -->
            <button 
                v-else-if="selectedIsOutgoing && isPending(selectedRequestStatus)"
                @click="selectedRequestId && cancelMatchRequest(selectedRequestId); isProfileModalOpen = false;"
                :disabled="processingId === selectedRequestId"
                class="w-full flex items-center justify-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-red-500 hover:bg-red-600 transition-colors mb-4"
            >
                <font-awesome-icon 
                    v-if="processingId === selectedRequestId" 
                    icon="spinner" 
                    spin 
                    class="mr-2" 
                />
                <font-awesome-icon v-else icon="times" class="mr-2" />
                {{ t('match.cancelRequest') }}
            </button>

            <p v-if="selectedRequestStatus !== 'Accepted' && !(selectedIsOutgoing && isPending(selectedRequestStatus))" class="text-gray-400 text-xs mb-4 italic">
                {{ t('inbox.connectToInteract') }}
            </p>

            <div class="text-left bg-gray-50 dark:bg-gray-700 p-4 rounded-lg mb-4">
                <p class="text-sm text-gray-600 dark:text-gray-300 mb-1"><strong>{{ t('match.age') }}:</strong> {{ selectedUserProfile.age || 'N/A' }}</p>
                <p class="text-sm text-gray-600 dark:text-gray-300"><strong>{{ t('match.about') }}:</strong> {{ selectedUserProfile.description || t('match.noDescription') }}</p>
            </div>

            <div class="text-left mb-6">
                <h4 class="font-semibold text-gray-900 dark:text-white mb-2">{{ t('match.sports') }}</h4>
                <div v-if="selectedUserSports.length === 0" class="text-sm text-gray-500 dark:text-gray-400">{{ t('match.noSportsAssigned') }}</div>
                <ul v-else class="space-y-2">
                    <li v-for="sport in selectedUserSports" :key="sport.sportId" class="flex items-center text-sm text-gray-700 dark:text-gray-300">
                        <font-awesome-icon :icon="faRunning" class="mr-2 text-indigo-500 dark:text-indigo-400" />
                        {{ sport.name }} 
                        <span v-if="sport.typicalDistanceKm && sport.typicalDistanceKm > 0" class="text-gray-400 dark:text-gray-500 ml-1">({{ sport.typicalDistanceKm }} km)</span>
                    </li>
                </ul>
            </div>
        </div>
    </BaseModal>

    <!-- Training Request Modal -->
    <BaseModal 
        :is-open="isTrainingModalOpen" 
        :title="t('match.inviteToTraining')"
        @close="isTrainingModalOpen = false"
    >
        <form @submit.prevent="submitTrainingRequest" class="space-y-4">
            <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">{{ t('match.dateTime') }}</label>
                <div class="flex space-x-2">
                    <input v-model="trainingForm.date" type="date" required class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
                    <input v-model="trainingForm.time" type="time" required class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
                </div>
            </div>
            <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">{{ t('match.location') }}</label>
                <input v-model="trainingForm.location" type="text" required placeholder="e.g. Central Park" class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
            </div>
            <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">{{ t('match.message') }}</label>
                <textarea v-model="trainingForm.message" rows="3" :placeholder="t('match.optionalMessage')" class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"></textarea>
            </div>
            <div class="mt-5 sm:mt-6 flex justify-end space-x-3">
                <button type="button" @click="isTrainingModalOpen = false" class="px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700">{{ t('common.cancel') }}</button>
                <button type="submit" class="px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600">{{ t('match.sendInvite') }}</button>
            </div>
        </form>
    </BaseModal>
  </div>
</template>
