import { initializeApp } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-app.js";
import { getAuth } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-auth.js";
import { getFirestore } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-firestore.js";
import { getAnalytics, isSupported } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-analytics.js";
import { initializeAppCheck, ReCaptchaV3Provider } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-app-check.js";

export const firebaseConfig = {
  apiKey: "AIzaSyDfNFeiUSr8lq6UHZoQN6tR-Y_DkuWjVnw",
  authDomain: "orcafacil-b771c.firebaseapp.com",
  projectId: "orcafacil-b771c",
  storageBucket: "orcafacil-b771c.firebasestorage.app",
  messagingSenderId: "124049832916",
  appId: "1:124049832916:web:0f30944c6e2e8695e6f441",
  measurementId: "G-WXJGMB50K3"
};

export const APP_CHECK_ENABLED = false;
export const APP_CHECK_SITE_KEY = "";
export const app = initializeApp(firebaseConfig);
export const auth = getAuth(app);
export const db = getFirestore(app);
export const appCheck = APP_CHECK_ENABLED && APP_CHECK_SITE_KEY
  ? initializeAppCheck(app, { provider: new ReCaptchaV3Provider(APP_CHECK_SITE_KEY), isTokenAutoRefreshEnabled: true })
  : null;

export const analyticsReady = isSupported()
  .then((supported) => (supported ? getAnalytics(app) : null))
  .catch(() => null);
