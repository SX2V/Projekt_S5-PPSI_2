<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue';
import { LMap, LTileLayer, LMarker, LCircle, LPopup } from '@vue-leaflet/vue-leaflet';
import L from 'leaflet';
import apiClient from '../api/axios';
import { useAuthStore } from '../stores/auth';
import { useToastStore } from '../stores/toast';
import type { 
  UserMatchDto, 
  Sport, 
  MatchRequestDto, 
  UpdateLocationDto,
  MatchRequestViewDto,
  User,
  UserSportDto,
  CreateTrainingRequestDto
} from '../types/api';
import BaseModal from '../components/BaseModal.vue';

import iconUrl from 'leaflet/dist/images/marker-icon.png';
import iconRetinaUrl from 'leaflet/dist/images/marker-icon-2x.png';
import shadowUrl from 'leaflet/dist/images/marker-shadow.png';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faUser, faRunning, faMapMarkerAlt, faEnvelope, faCommentDots, faDumbbell, faTimes, faEdit, faCheck, faClock, faPaperPlane } from '@fortawesome/free-solid-svg-icons';
import { AxiosError } from 'axios';

const fixLeafletIcons = () => {
  delete (L.Icon.Default.prototype as any)._getIconUrl;
  L.Icon.Default.mergeOptions({
    iconRetinaUrl,
    iconUrl,
    shadowUrl,
  });
};
fixLeafletIcons();

const authStore = useAuthStore();
const toast = useToastStore();

interface ExtendedUserMatchDto extends UserMatchDto {
    profilePictureUrl?: string | null;
    existingRequestStatus?: string | null;
    isIncoming?: boolean; 
    requestId?: string; 
}

const zoom = ref(13);
const center = ref<[number, number]>([52.2297, 21.0122]); 
const userLocation = ref<[number, number] | null>(null);
const searchRadiusKm = ref(10);
const selectedSportId = ref<string>('');
const availableSports = ref<Sport[]>([]);
const matchedUsers = ref<ExtendedUserMatchDto[]>([]);
const outgoingRequests = ref<MatchRequestViewDto[]>([]); 
const incomingRequests = ref<MatchRequestViewDto[]>([]); 
const isLoadingMatches = ref(false);
const isUpdatingLocation = ref(false);
const requestStatus = ref<Record<string, 'idle' | 'sending' | 'sent' | 'error' | 'cancelling'>>({});
const isManualLocationMode = ref(false);

const isLocationModalOpen = ref(false);
const manualLocation = ref({ lat: 0, lng: 0 });

const isProfileModalOpen = ref(false);
const selectedUserProfile = ref<User | null>(null);
const selectedUserSports = ref<UserSportDto[]>([]);
const isLoadingProfile = ref(false);
const selectedUserProfilePic = ref<string | null>(null);
const selectedUserRequestStatus = ref<string | null>(null); 
const selectedUserIsIncoming = ref<boolean>(false);
const selectedUserRequestId = ref<string | undefined>(undefined);

const isTrainingModalOpen = ref(false);
const trainingForm = ref({
    date: '',
    time: '',
    location: '',
    message: ''
});

const circleRadiusMeters = computed(() => searchRadiusKm.value * 1000);

const filteredUsers = computed(() => {
    if (!authStore.user?.id) return [];

    const globallyConnectedUserIds = new Set<string>();
    
    outgoingRequests.value.forEach(req => {
        if (req.status === 'Accepted' && req.toUserId) globallyConnectedUserIds.add(req.toUserId);
    });
    
    incomingRequests.value.forEach(req => {
        if (req.status === 'Accepted' && req.fromUserId) globallyConnectedUserIds.add(req.fromUserId);
    });

    return matchedUsers.value.filter(u => {
        if (u.id === authStore.user?.id) return false;
        
        if (u.id && globallyConnectedUserIds.has(u.id)) return false;

        if (u.distanceKm !== undefined && u.distanceKm > searchRadiusKm.value) return false;

        return true;
    });
});

const canSendRequest = (status: string | null | undefined) => {
    return !status || status === 'Rejected' || status === 'Cancelled';
};

const calculateDistance = (lat1: number, lon1: number, lat2: number, lon2: number) => {
    const R = 6371; 
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a = 
        Math.sin(dLat/2) * Math.sin(dLat/2) +
        Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) * 
        Math.sin(dLon/2) * Math.sin(dLon/2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
    return R * c; 
};

const getUserLocation = () => {
  isManualLocationMode.value = false;
  if (navigator.geolocation) {
    navigator.geolocation.getCurrentPosition(
      async (position) => {
        const { latitude, longitude } = position.coords;
        userLocation.value = [latitude, longitude];
        center.value = [latitude, longitude];
        
        await updateBackendLocation(latitude, longitude);
        fetchMatches();
      },
      (error) => {
        console.error("Error getting location:", error);
        toast.error("Could not get your location. Using saved location.");
        if (authStore.user?.latitude && authStore.user?.longitude) {
            userLocation.value = [authStore.user.latitude, authStore.user.longitude];
            center.value = [authStore.user.latitude, authStore.user.longitude];
            fetchMatches();
        }
      }
    );
  } else {
      toast.warning("Geolocation is not supported by your browser.");
      if (authStore.user?.latitude && authStore.user?.longitude) {
          userLocation.value = [authStore.user.latitude, authStore.user.longitude];
          center.value = [authStore.user.latitude, authStore.user.longitude];
          fetchMatches();
      }
  }
};

const handleMarkerDrag = async (e: L.LeafletEvent) => {
    isManualLocationMode.value = true;
    const { lat, lng } = (e.target as L.Marker).getLatLng();
    
    userLocation.value = [lat, lng];
    center.value = [lat, lng];
    
    await updateBackendLocation(lat, lng);
    fetchMatches();
};

const openLocationModal = () => {
    if (userLocation.value) {
        manualLocation.value = { lat: userLocation.value[0], lng: userLocation.value[1] };
    }
    isLocationModalOpen.value = true;
};

const submitManualLocation = async () => {
    isManualLocationMode.value = true;
    const { lat, lng } = manualLocation.value;

    userLocation.value = [lat, lng];
    center.value = [lat, lng];
    
    await updateBackendLocation(lat, lng);
    fetchMatches();
    isLocationModalOpen.value = false;
};

const updateBackendLocation = async (lat: number, lng: number) => {
  isUpdatingLocation.value = true;
  const payload: UpdateLocationDto = {
    latitude: lat,
    longitude: lng,
    searchRadiusKm: searchRadiusKm.value
  };
  
  try {
    await apiClient.patch('/Users/me/location', payload);
  } catch (error) {
    console.error("Failed to update location on backend", error);
  } finally {
    isUpdatingLocation.value = false;
  }
};

const onRadiusChange = () => {
};

const fetchSports = async () => {
  try {
    const response = await apiClient.get<Sport[]>('/Sports');
    availableSports.value = response.data;
    if (availableSports.value.length > 0) {
        const userSportIds = authStore.user?.userSports?.map(us => us.sportId) || [];
        const preferredSport = availableSports.value.find(s => s.id && userSportIds.includes(s.id));
        selectedSportId.value = preferredSport?.id || availableSports.value[0].id || '';
    }
  } catch (error) {
    console.error("Failed to fetch sports", error);
    toast.error("Failed to load sports list.");
  }
};

const fetchRequests = async () => {
    if (!authStore.user?.id) return;
    try {
        const params = { 
            userId: authStore.user.id,
            _t: Date.now() 
        };
        
        const [outRes, inRes] = await Promise.all([
            apiClient.get<MatchRequestViewDto[]>('/Match/outgoing', { params }),
            apiClient.get<MatchRequestViewDto[]>('/Match/incoming', { params })
        ]);
        outgoingRequests.value = outRes.data;
        incomingRequests.value = inRes.data;
    } catch (error) {
        console.error("Failed to fetch requests", error);
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

const fetchMatches = async () => {
  if (!selectedSportId.value) return;
  
  isLoadingMatches.value = true;
  matchedUsers.value = [];
  
  requestStatus.value = {};

  try {
    await fetchRequests();

    const params: any = {
      sportId: selectedSportId.value,
      maxSearchRadiusKm: 10000, 
      isAvailableNow: true,
      _t: Date.now()
    };

    const response = await apiClient.get<UserMatchDto[]>('/Users', { params });
    
    const enrichedUsers = await Promise.all(response.data.map(async (u) => {
        if (!u.id) return null;

        let realDistance = u.distanceKm;
        let picUrl = null;

        try {
            const userDetails = await apiClient.get<User>(`/Users/${u.id}`);
            const targetLat = userDetails.data.latitude;
            const targetLng = userDetails.data.longitude;

            if (userLocation.value && targetLat && targetLng) {
                realDistance = calculateDistance(
                    userLocation.value[0], 
                    userLocation.value[1], 
                    targetLat, 
                    targetLng
                );
            }
            
            if (userDetails.data.profilePicturePath) {
                 let url = userDetails.data.profilePicturePath;
                 if (!url.startsWith('http')) {
                    const baseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5254'; 
                    const cleanBase = baseUrl.replace(/\/$/, '');
                    const cleanPath = url.replace(/^\//, '');
                    picUrl = `${cleanBase}/${cleanPath}`;
                } else {
                    picUrl = url;
                }
            }

        } catch (e) {
            console.error(`Failed to fetch details for user ${u.id}`, e);
        }

        let outgoing = outgoingRequests.value.find(req => 
            req.toUserId === u.id && req.status === 'Pending'
        );
        
        if (!outgoing) {
             outgoing = outgoingRequests.value.find(req => req.toUserId === u.id);
        }
        
        let incoming = incomingRequests.value.find(req => 
            req.fromUserId === u.id && req.status === 'Pending'
        );
        
        if (!incoming) {
            incoming = incomingRequests.value.find(req => req.fromUserId === u.id);
        }

        let status = null;
        let isIncoming = false;
        let requestId = undefined;

        if (outgoing) {
            status = outgoing.status;
            requestId = outgoing.id;
        } else if (incoming) {
            status = incoming.status;
            isIncoming = true;
            requestId = incoming.id;
        }

        return {
            ...u,
            distanceKm: realDistance, 
            profilePictureUrl: picUrl,
            existingRequestStatus: status,
            isIncoming: isIncoming,
            requestId: requestId
        } as ExtendedUserMatchDto;
    }));

    matchedUsers.value = enrichedUsers.filter((u): u is ExtendedUserMatchDto => u !== null);

  } catch (error) {
    console.error("Failed to fetch matches", error);
    toast.error("Failed to find matches.");
  } finally {
    isLoadingMatches.value = false;
  }
};

const sendMatchRequest = async (targetUserId: string) => {
  if (!targetUserId || !selectedSportId.value || !authStore.user?.id) return;
  
  requestStatus.value[targetUserId] = 'sending';
  
  const payload: MatchRequestDto = {
    fromUserId: authStore.user.id,
    toUserId: targetUserId,
    sportId: selectedSportId.value
  };
  
  try {
    await apiClient.post('/Match/request', payload);
    requestStatus.value[targetUserId] = 'sent';
    toast.success("Match request sent!");
    await fetchMatches(); 
    
    if (selectedUserProfile.value?.id === targetUserId) {
        selectedUserRequestStatus.value = 'Pending';
        selectedUserIsIncoming.value = false;
    }

  } catch (error) {
    console.error("Failed to send match request", error);
    requestStatus.value[targetUserId] = 'error';
    toast.error("Failed to send request.");
  }
};

const cancelMatchRequest = async (userId: string, requestId: string) => {
    if (!requestId) return;
    
    requestStatus.value[userId] = 'cancelling';
    
    try {
        await apiClient.patch(`/Match/${requestId}/cancel`);
        toast.success("Request cancelled.");
        
        const index = matchedUsers.value.findIndex(u => u.id === userId);
        if (index !== -1) {
            matchedUsers.value[index].existingRequestStatus = null;
            matchedUsers.value[index].requestId = undefined;
            matchedUsers.value[index].isIncoming = false;
        }
        
        if (selectedUserProfile.value?.id === userId) {
            selectedUserRequestStatus.value = null;
            selectedUserRequestId.value = undefined;
            selectedUserIsIncoming.value = false;
        }
        
        requestStatus.value[userId] = 'idle';
        await fetchRequests(); 
    } catch (error) {
        console.error("Failed to cancel request", error);
        toast.error("Failed to cancel request.");
        requestStatus.value[userId] = 'error';
    }
};

const openTrainingModal = () => {
    trainingForm.value = { date: '', time: '', location: '', message: '' };
    isTrainingModalOpen.value = true;
};

const submitTrainingRequest = async () => {
    if (!selectedUserProfile.value?.id) return;
    
    let dateTime = null;
    if (trainingForm.value.date && trainingForm.value.time) {
        dateTime = new Date(`${trainingForm.value.date}T${trainingForm.value.time}`).toISOString();
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
        toast.success('Training request sent!');
        isTrainingModalOpen.value = false;
        isProfileModalOpen.value = false;
    } catch (error) {
        console.error('Failed to send training request', error);
        const axiosError = error as AxiosError;
        if (axiosError.response?.data) {
            console.log('Backend error details:', axiosError.response.data);
        }
        toast.error('Failed to send training request.');
    }
};

const openUserProfile = async (userId: string) => {
    isProfileModalOpen.value = true;
    isLoadingProfile.value = true;
    selectedUserProfile.value = null;
    selectedUserSports.value = [];
    selectedUserProfilePic.value = null;
    selectedUserRequestStatus.value = null;
    selectedUserIsIncoming.value = false;
    selectedUserRequestId.value = undefined;

    const userInList = matchedUsers.value.find(u => u.id === userId);
    if (userInList) {
        selectedUserRequestStatus.value = userInList.existingRequestStatus || null;
        selectedUserIsIncoming.value = userInList.isIncoming || false;
        selectedUserRequestId.value = userInList.requestId;
    }

    try {
        const userResponse = await apiClient.get<User>(`/Users/${userId}`);
        selectedUserProfile.value = userResponse.data;

        const sportsResponse = await apiClient.get<UserSportDto[]>(`/Users/${userId}/assigned-sports`);
        selectedUserSports.value = sportsResponse.data;

        const picUrl = await fetchProfilePicture(userId);
        selectedUserProfilePic.value = picUrl;

    } catch (error) {
        console.error("Failed to fetch user profile", error);
        toast.error("Failed to load user profile");
        isProfileModalOpen.value = false;
    } finally {
        isLoadingProfile.value = false;
    }
};

const sendMessage = () => {
    if (selectedUserProfile.value?.email) {
        window.location.href = `mailto:${selectedUserProfile.value.email}`;
    }
};

watch(selectedSportId, () => {
  fetchMatches();
});

onMounted(async () => {
  if (!authStore.user) {
    await authStore.fetchUser();
  }
  
  if (authStore.user?.searchRadiusKm) {
    searchRadiusKm.value = authStore.user.searchRadiusKm;
  }

  await fetchSports();
  getUserLocation();
});
</script>

<template>
  <div class="flex flex-col h-[calc(100vh-64px)] bg-gray-50 dark:bg-gray-900 transition-colors duration-200"> 
    
    <!-- Map Section -->
    <div class="h-1/2 w-full relative z-0">
      <l-map 
        v-model:zoom="zoom" 
        v-model:center="center" 
        :use-global-leaflet="false"
        class="h-full w-full"
      >
        <l-tile-layer
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          layer-type="base"
          name="OpenStreetMap"
        ></l-tile-layer>
        
        <l-marker 
            v-if="userLocation" 
            :lat-lng="userLocation"
            :draggable="true"
            @dragend="handleMarkerDrag"
        >
           <l-popup>
               <div class="text-center">
                   <p>Your search location</p>
                   <p class="text-xs text-gray-500">(Drag me to change)</p>
               </div>
           </l-popup>
        </l-marker>
        
        <l-circle 
          v-if="userLocation"
          :lat-lng="userLocation" 
          :radius="circleRadiusMeters"
          color="#4F46E5"
          fill-color="#4F46E5"
          :fill-opacity="0.1"
        />
      </l-map>

      <!-- Controls -->
      <div class="absolute top-4 right-4 z-[1000] bg-white dark:bg-gray-800 p-3 rounded-lg shadow-md w-64 transition-colors duration-200">
        <div class="mb-3">
          <label class="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">Sport</label>
          <select 
            v-model="selectedSportId" 
            class="block w-full pl-3 pr-10 py-1 text-sm border-gray-300 dark:border-gray-600 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 rounded-md border bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
          >
            <option v-for="sport in availableSports" :key="sport.id" :value="sport.id">
              {{ sport.name }} {{ (sport.typicalDistanceKm && sport.typicalDistanceKm > 0) ? `(${sport.typicalDistanceKm} km)` : '' }}
            </option>
          </select>
        </div>
        
        <div>
          <label class="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">
            Radius: {{ searchRadiusKm }} km
          </label>
          <input 
            type="range" 
            v-model.number="searchRadiusKm" 
            min="1" 
            max="100" 
            step="1"
            @input="onRadiusChange"
            class="w-full h-2 bg-gray-200 dark:bg-gray-600 rounded-lg appearance-none cursor-pointer"
          />
        </div>
      </div>
      
      <!-- Location Buttons -->
      <div class="absolute bottom-4 right-4 z-[1000] flex flex-col space-y-2">
        <button 
          @click="openLocationModal"
          class="bg-white dark:bg-gray-800 p-2 rounded-full shadow-md text-indigo-600 dark:text-indigo-400 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors duration-200"
          title="Set Location Manually"
        >
          <font-awesome-icon :icon="faEdit" class="text-xl" />
        </button>
        <button 
          @click="getUserLocation"
          class="bg-white dark:bg-gray-800 p-2 rounded-full shadow-md text-indigo-600 dark:text-indigo-400 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors duration-200"
          title="Use My GPS Location"
        >
          <font-awesome-icon icon="location-arrow" class="text-xl" />
        </button>
      </div>
    </div>

    <!-- Results Section -->
    <div class="h-1/2 bg-gray-50 dark:bg-gray-900 overflow-y-auto p-4 transition-colors duration-200">
      <div class="max-w-3xl mx-auto">
        <h2 class="text-lg font-bold text-gray-900 dark:text-white mb-4 flex items-center">
          <font-awesome-icon icon="search" class="mr-2 text-indigo-500 dark:text-indigo-400" />
          Found Athletes ({{ filteredUsers.length }})
        </h2>

        <div v-if="isLoadingMatches" class="flex justify-center py-8">
          <font-awesome-icon icon="spinner" spin class="text-indigo-500 dark:text-indigo-400 text-3xl" />
        </div>

        <div v-else-if="filteredUsers.length === 0" class="text-center py-8 text-gray-500 dark:text-gray-400">
          <p>No athletes found nearby for this sport.</p>
          <p class="text-sm mt-2">Try increasing the radius or changing the sport.</p>
        </div>

        <div v-else class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div 
            v-for="user in filteredUsers" 
            :key="user.id" 
            class="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-100 dark:border-gray-700 p-4 flex flex-col justify-between transition-transform hover:scale-[1.02] cursor-pointer"
            @click="user.id && openUserProfile(user.id)"
          >
            <div>
              <div class="flex items-center justify-between mb-2">
                <div class="flex items-center">
                    <!-- Avatar -->
                    <div class="h-10 w-10 rounded-full bg-gray-200 dark:bg-gray-600 flex-shrink-0 overflow-hidden mr-3">
                        <img v-if="user.profilePictureUrl" :src="user.profilePictureUrl" alt="Avatar" class="h-full w-full object-cover" />
                        <div v-else class="h-full w-full flex items-center justify-center text-gray-500 dark:text-gray-300">
                            <font-awesome-icon :icon="faUser" />
                        </div>
                    </div>
                    <h3 class="font-bold text-gray-900 dark:text-white">{{ user.name }}</h3>
                </div>
                <span 
                  class="px-2 py-0.5 rounded-full text-xs font-medium"
                  :class="user.isAvailableNow ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' : 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'"
                >
                  {{ user.isAvailableNow ? 'Online' : 'Offline' }}
                </span>
              </div>
              
              <div class="text-sm text-gray-600 dark:text-gray-400 mb-3 space-y-1 ml-13"> 
                <div class="flex items-center">
                   <font-awesome-icon icon="street-view" class="w-4 mr-2 text-gray-400 dark:text-gray-500" />
                   <span>{{ user.distanceKm?.toFixed(1) }} km away</span>
                </div>
              </div>
            </div>

            <!-- Action Button (Stop propagation to prevent opening modal when clicking button) -->
            <div class="mt-2">
                <!-- Cancel Button for Pending Outgoing Requests -->
                <button 
                  v-if="user.existingRequestStatus === 'Pending' && !user.isIncoming"
                  @click.stop="user.id && user.requestId && cancelMatchRequest(user.id, user.requestId)"
                  :disabled="requestStatus[user.id] === 'cancelling'"
                  class="w-full flex items-center justify-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-red-500 hover:bg-red-600 transition-colors"
                >
                  <font-awesome-icon 
                    v-if="requestStatus[user.id] === 'cancelling'" 
                    icon="spinner" 
                    spin 
                    class="mr-2" 
                  />
                  <font-awesome-icon v-else icon="times" class="mr-2" />
                  Cancel Request
                </button>

                <!-- Standard Button for other states -->
                <button 
                  v-else
                  @click.stop="user.id && sendMatchRequest(user.id)"
                  :disabled="!user.id || !canSendRequest(user.existingRequestStatus) || requestStatus[user.id] === 'sent' || requestStatus[user.id] === 'sending'"
                  class="w-full flex items-center justify-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white transition-colors"
                  :class="{
                    'bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600': canSendRequest(user.existingRequestStatus) && (!requestStatus[user.id] || requestStatus[user.id] === 'idle' || requestStatus[user.id] === 'error'),
                    'bg-green-500 cursor-default': requestStatus[user.id] === 'sent' || user.existingRequestStatus === 'Accepted',
                    'bg-gray-400 cursor-default dark:bg-gray-600': user.existingRequestStatus === 'Pending' || (user.existingRequestStatus && !canSendRequest(user.existingRequestStatus)),
                    'bg-indigo-400 cursor-wait': requestStatus[user.id] === 'sending'
                  }"
                >
                  <font-awesome-icon 
                    v-if="requestStatus[user.id] === 'sending'" 
                    icon="spinner" 
                    spin 
                    class="mr-2" 
                  />
                  <font-awesome-icon 
                    v-else-if="requestStatus[user.id] === 'sent' || user.existingRequestStatus === 'Accepted'" 
                    icon="check" 
                    class="mr-2" 
                  />
                  <font-awesome-icon 
                    v-else-if="user.existingRequestStatus === 'Pending'"
                    icon="clock"
                    class="mr-2"
                  />
                  <font-awesome-icon 
                    v-else 
                    icon="paper-plane" 
                    class="mr-2" 
                  />
                  
                  <span v-if="requestStatus[user.id] === 'sent'">Request Sent</span>
                  <span v-else-if="requestStatus[user.id] === 'sending'">Sending...</span>
                  <span v-else-if="user.existingRequestStatus === 'Pending'">
                      {{ user.isIncoming ? 'Received' : 'Request Pending' }}
                  </span>
                  <span v-else-if="user.existingRequestStatus === 'Accepted'">Connected</span>
                  <span v-else-if="user.existingRequestStatus === 'Rejected'">Rejected</span>
                  <span v-else>Send Request</span>
                </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- User Profile Modal -->
    <BaseModal 
        :is-open="isProfileModalOpen" 
        title="User Profile"
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
                v-if="selectedUserRequestStatus === 'Accepted'" 
                @click="sendMessage"
                class="w-full flex items-center justify-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 mb-4"
            >
                <font-awesome-icon :icon="faCommentDots" class="mr-2" />
                Send Message
            </button>

            <!-- Invite to Training Button -->
            <button 
                v-if="selectedUserRequestStatus === 'Accepted'" 
                @click="openTrainingModal"
                class="w-full flex items-center justify-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 mb-4"
            >
                <font-awesome-icon :icon="faDumbbell" class="mr-2" />
                Invite to Training
            </button>

            <!-- Cancel Request Button in Modal -->
            <button 
                v-else-if="selectedUserRequestStatus === 'Pending' && !selectedUserIsIncoming"
                @click="selectedUserProfile.id && selectedUserRequestId && cancelMatchRequest(selectedUserProfile.id, selectedUserRequestId)"
                :disabled="requestStatus[selectedUserProfile.id!] === 'cancelling'"
                class="w-full flex items-center justify-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-red-500 hover:bg-red-600 transition-colors"
            >
                <font-awesome-icon 
                    v-if="requestStatus[selectedUserProfile.id!] === 'cancelling'" 
                    icon="spinner" 
                    spin 
                    class="mr-2" 
                />
                <font-awesome-icon v-else icon="times" class="mr-2" />
                Cancel Request
            </button>

            <button 
              v-else
              @click="selectedUserProfile.id && sendMatchRequest(selectedUserProfile.id)"
              :disabled="!selectedUserProfile.id || !canSendRequest(selectedUserRequestStatus) || (selectedUserProfile.id && requestStatus[selectedUserProfile.id] === 'sent') || (selectedUserProfile.id && requestStatus[selectedUserProfile.id] === 'sending')"
              class="w-full flex items-center justify-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white transition-colors"
              :class="{
                'bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600': (!selectedUserRequestStatus || selectedUserRequestStatus === 'Cancelled') && (!selectedUserProfile.id || requestStatus[selectedUserProfile.id] !== 'sending'),
                'bg-gray-400 cursor-default dark:bg-gray-600': selectedUserRequestStatus === 'Pending' || selectedUserRequestStatus === 'Rejected',
                'bg-indigo-400 cursor-wait': selectedUserProfile.id && requestStatus[selectedUserProfile.id] === 'sending'
              }"
            >
              <font-awesome-icon 
                v-if="requestStatus[selectedUserProfile.id!] === 'sending'" 
                icon="spinner" 
                spin 
                class="mr-2" 
              />
              <font-awesome-icon 
                v-else-if="requestStatus[selectedUserProfile.id!] === 'sent' || selectedUserRequestStatus === 'Accepted'" 
                icon="check" 
                class="mr-2" 
              />
              <font-awesome-icon 
                v-else-if="selectedUserRequestStatus === 'Pending'"
                icon="clock"
                class="mr-2"
              />
              <font-awesome-icon 
                v-else 
                icon="paper-plane" 
                class="mr-2" 
              />
              
              <span v-if="requestStatus[selectedUserProfile.id!] === 'sending'">Sending...</span>
              <span v-else-if="selectedUserRequestStatus === 'Pending'">
                  {{ selectedUserIsIncoming ? 'Received' : 'Request Pending' }}
              </span>
              <span v-else-if="selectedUserRequestStatus === 'Rejected'">Rejected</span>
              <span v-else>Send Match Request</span>
            </button>

            <div class="text-left bg-gray-50 dark:bg-gray-700 p-4 rounded-lg mb-4 mt-4">
                <p class="text-sm text-gray-600 dark:text-gray-300 mb-1"><strong>Age:</strong> {{ selectedUserProfile.age || 'N/A' }}</p>
                <p class="text-sm text-gray-600 dark:text-gray-300"><strong>About:</strong> {{ selectedUserProfile.description || 'No description.' }}</p>
            </div>

            <div class="text-left mb-6">
                <h4 class="font-semibold text-gray-900 dark:text-white mb-2">Sports</h4>
                <div v-if="selectedUserSports.length === 0" class="text-sm text-gray-500 dark:text-gray-400">No sports assigned.</div>
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
        title="Invite to Training"
        @close="isTrainingModalOpen = false"
    >
        <form @submit.prevent="submitTrainingRequest" class="space-y-4">
            <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Date & Time</label>
                <div class="flex space-x-2">
                    <input v-model="trainingForm.date" type="date" required class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
                    <input v-model="trainingForm.time" type="time" required class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
                </div>
            </div>
            <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Location</label>
                <input v-model="trainingForm.location" type="text" required placeholder="e.g. Central Park" class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
            </div>
            <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Message</label>
                <textarea v-model="trainingForm.message" rows="3" placeholder="Optional message..." class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"></textarea>
            </div>
            <div class="mt-5 sm:mt-6 flex justify-end space-x-3">
                <button type="button" @click="isTrainingModalOpen = false" class="px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700">Cancel</button>
                <button type="submit" class="px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600">Send Invite</button>
            </div>
        </form>
    </BaseModal>

    <!-- Manual Location Modal -->
    <BaseModal 
        :is-open="isLocationModalOpen" 
        title="Set Location Manually"
        @close="isLocationModalOpen = false"
    >
        <form @submit.prevent="submitManualLocation" class="space-y-4">
            <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Latitude</label>
                <input v-model.number="manualLocation.lat" type="number" step="any" required class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
            </div>
            <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Longitude</label>
                <input v-model.number="manualLocation.lng" type="number" step="any" required class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
            </div>
            <div class="mt-5 sm:mt-6 flex justify-end space-x-3">
                <button type="button" @click="isLocationModalOpen = false" class="px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700">Cancel</button>
                <button type="submit" class="px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600">Set Location</button>
            </div>
        </form>
    </BaseModal>
  </div>
</template>
