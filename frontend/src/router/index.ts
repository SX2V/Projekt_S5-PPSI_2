import { createRouter, createWebHistory } from 'vue-router';
import type { RouteRecordRaw } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import MainLayout from '../layouts/MainLayout.vue';
import AdminLayout from '../layouts/AdminLayout.vue';

const LoginView = () => import('../views/LoginView.vue');
const RegisterView = () => import('../views/RegisterView.vue');
const RequestResetView = () => import('../views/RequestResetView.vue');
const ResetPasswordView = () => import('../views/ResetPasswordView.vue');
const MatchView = () => import('../views/MatchView.vue');
const InboxView = () => import('../views/InboxView.vue');
const ProfileView = () => import('../views/ProfileView.vue');
const AdminDashboard = () => import('../views/AdminDashboard.vue');
const AdminUsers = () => import('../views/AdminUsers.vue');
const AdminSports = () => import('../views/AdminSports.vue');
const AdminLogs = () => import('../views/AdminLogs.vue');

const routes: Array<RouteRecordRaw> = [
  {
    path: '/login',
    name: 'Login',
    component: LoginView,
    meta: { requiresAuth: false, title: 'Login' }
  },
  {
    path: '/register',
    name: 'Register',
    component: RegisterView,
    meta: { requiresAuth: false, title: 'Register' }
  },
  {
    path: '/request-reset',
    name: 'RequestReset',
    component: RequestResetView,
    meta: { requiresAuth: false, title: 'Reset Password' }
  },
  {
    path: '/reset-password',
    name: 'ResetPassword',
    component: ResetPasswordView,
    meta: { requiresAuth: false, title: 'Set New Password' }
  },
  {
    path: '/',
    component: MainLayout,
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'Home',
        redirect: { name: 'Match' }
      },
      {
        path: 'match',
        name: 'Match',
        component: MatchView,
        meta: { title: 'Match' }
      },
      {
        path: 'inbox',
        name: 'Inbox',
        component: InboxView,
        meta: { title: 'Inbox' }
      },
      {
        path: 'profile',
        name: 'Profile',
        component: ProfileView,
        meta: { title: 'Profile' }
      }
    ]
  },
  {
    path: '/admin',
    component: AdminLayout,
    meta: { requiresAuth: true, requiresAdmin: true },
    children: [
      {
        path: '',
        redirect: { name: 'AdminDashboard' }
      },
      {
        path: 'dashboard',
        name: 'AdminDashboard',
        component: AdminDashboard,
        meta: { title: 'Admin Dashboard' }
      },
      {
        path: 'users',
        name: 'AdminUsers',
        component: AdminUsers,
        meta: { title: 'Admin Users' }
      },
      {
        path: 'sports',
        name: 'AdminSports',
        component: AdminSports,
        meta: { title: 'Admin Sports' }
      },
      {
        path: 'logs',
        name: 'AdminLogs',
        component: AdminLogs,
        meta: { title: 'Admin Logs' }
      }
    ]
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: { name: 'Match' }
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

router.beforeEach(async (to, from, next) => {
  const authStore = useAuthStore();

  const defaultTitle = 'SportConnect';
  document.title = to.meta.title ? `${to.meta.title} - ${defaultTitle}` : defaultTitle;

  if (!authStore.user && authStore.token) {
    try {
      await authStore.fetchUser();
    } catch (error) {
      authStore.logout();
      return next({ name: 'Login' });
    }
  }

  const requiresAuth = to.matched.some(record => record.meta.requiresAuth);
  const requiresAdmin = to.matched.some(record => record.meta.requiresAdmin);
  const isAuthenticated = authStore.isAuthenticated;
  
  const role = authStore.user?.role;
  const isAdmin = role === 1 || role === 'Admin'; 

  if (requiresAuth && !isAuthenticated) {
    next({ name: 'Login' });
  } else if (requiresAuth && isAuthenticated && requiresAdmin && !isAdmin) {
    next({ name: 'Home' });
  } else if ((to.name === 'Login' || to.name === 'Register') && isAuthenticated) {
    next({ name: 'Home' });
  } else {
    next();
  }
});

export default router;
