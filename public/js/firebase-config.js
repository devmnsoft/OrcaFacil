import { initializeApp } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-app.js";
import { getAuth } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-auth.js";
import { getFirestore } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-firestore.js";
import { getAnalytics, isSupported } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-analytics.js";

export const firebaseConfig = {
  apiKey: "AIzaSyDfNFeiUSr8lq6UHZoQN6tR-Y_DkuWjVnw",
  authDomain: "orcafacil-b771c.firebaseapp.com",
  projectId: "orcafacil-b771c",
  storageBucket: "orcafacil-b771c.firebasestorage.app",
  messagingSenderId: "124049832916",
  appId: "1:124049832916:web:0f30944c6e2e8695e6f441",
  measurementId: "G-WXJGMB50K3"
};

export const app = initializeApp(firebaseConfig);
export const auth = getAuth(app);
export const db = getFirestore(app);

export const analyticsReady = isSupported()
  .then((supported) => (supported ? getAnalytics(app) : null))
  .catch(() => null);
