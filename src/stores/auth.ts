import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import apiClient from '../api/axios';
import type { LoginDto, RegisterDto, User } from '../types/api';
import { AxiosError } from 'axios';
import { useToastStore } from './toast';

interface AuthMeResponse {
  userId: string;
  email: string;
  role: string;
}

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null);
  const token = ref<string | null>(localStorage.getItem('token'));
  const isAuthenticated = computed(() => !!token.value);
  const isLoading = ref(false);
  const error = ref<string | null>(null);
  const toast = useToastStore();

  const setToken = (newToken: string | null) => {
    token.value = newToken;
    if (newToken) {
      localStorage.setItem('token', newToken);
    } else {
      localStorage.removeItem('token');
    }
  };

  const login = async (credentials: LoginDto) => {
    isLoading.value = true;
    error.value = null;
    try {
      const response = await apiClient.post('/auth/login', credentials);
      
      const responseData = response.data as { token?: string }; 
      
      if (responseData.token) {
        setToken(responseData.token);
        await fetchUser();
        toast.success('Logged in successfully');
      } else {
        await fetchUser();
        toast.success('Logged in successfully');
      }
    } catch (err) {
      const axiosError = err as AxiosError;
      error.value = axiosError.message || 'Login failed';
      toast.error('Login failed. Please check your credentials.');
      throw err;
    } finally {
      isLoading.value = false;
    }
  };

  const register = async (data: RegisterDto) => {
    isLoading.value = true;
    error.value = null;
    try {
      await apiClient.post('/auth/register', data);
      toast.success('Registration successful! Please log in.');
    } catch (err) {
      const axiosError = err as AxiosError;
      error.value = axiosError.message || 'Registration failed';
      toast.error('Registration failed. Please try again.');
      throw err;
    } finally {
      isLoading.value = false;
    }
  };

  const fetchUser = async () => {
    isLoading.value = true;
    try {
      const authResponse = await apiClient.get<AuthMeResponse>('/auth/me', {
        params: { _t: Date.now() }
      });
      
      const userId = authResponse.data.userId;

      const userResponse = await apiClient.get<User>(`/Users/${userId}`, {
        params: { _t: Date.now() }
      });

      user.value = userResponse.data;
      
      console.log('Fetched user:', user.value);

    } catch (err) {
      console.error("Error fetching user details:", err);
      setToken(null);
      user.value = null;
      throw err;
    } finally {
      isLoading.value = false;
    }
  };

  const updateUserState = (updates: Partial<User>) => {
      if (user.value) {
          user.value = { ...user.value, ...updates };
      }
  };

  const logout = () => {
    setToken(null);
    user.value = null;
    toast.info('Logged out');
  };

  return {
    user,
    token,
    isAuthenticated,
    isLoading,
    error,
    login,
    register,
    fetchUser,
    logout,
    updateUserState
  };
});
