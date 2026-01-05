import { createApp } from 'vue'
import { createPinia } from 'pinia'
import './style.css'
import App from './App.vue'
import router from './router'
import 'leaflet/dist/leaflet.css'

import { library } from '@fortawesome/fontawesome-svg-core'
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'
import { 
  faEnvelope, 
  faLock, 
  faUser, 
  faSignInAlt, 
  faUserPlus, 
  faSpinner,
  faEdit,
  faTrash,
  faPlus,
  faSave,
  faRunning,
  faToggleOn,
  faToggleOff,
  faTimes,
  faMapMarkerAlt,
  faLocationArrow,
  faSearch,
  faPaperPlane,
  faFilter,
  faStreetView,
  faCheck,
  faInbox,
  faChartBar,
  faUsers,
  faBan,
  faUnlock,
  faTachometerAlt,
  faSignOutAlt,
  faCamera,
  faFileAlt,
  faSync,
  faDumbbell,
  faSun,
  faMoon,
  faCommentDots,
  faHandshake
} from '@fortawesome/free-solid-svg-icons'
import { faFacebook, faStrava } from '@fortawesome/free-brands-svg-icons'

library.add(
  faEnvelope, 
  faLock, 
  faUser, 
  faSignInAlt, 
  faUserPlus, 
  faSpinner,
  faEdit,
  faTrash,
  faPlus,
  faSave,
  faRunning,
  faToggleOn,
  faToggleOff,
  faTimes,
  faMapMarkerAlt,
  faLocationArrow,
  faSearch,
  faPaperPlane,
  faFilter,
  faStreetView,
  faCheck,
  faInbox,
  faChartBar,
  faUsers,
  faBan,
  faUnlock,
  faTachometerAlt,
  faSignOutAlt,
  faCamera,
  faFileAlt,
  faSync,
  faDumbbell,
  faSun,
  faMoon,
  faCommentDots,
  faHandshake,
  faFacebook,
  faStrava
)

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)
app.component('font-awesome-icon', FontAwesomeIcon)

app.config.errorHandler = (err, instance, info) => {
  console.error("Global Error Handler:", err);
  console.error("Info:", info);
};

app.mount('#app')
