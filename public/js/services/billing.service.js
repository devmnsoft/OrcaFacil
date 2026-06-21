import { auth, db, app } from '../firebase-config.js';
import { collection, doc, getDoc, getDocs, limit, orderBy, query } from 'https://www.gstatic.com/firebasejs/10.12.5/firebase-firestore.js';
import { getFunctions, httpsCallable } from 'https://www.gstatic.com/firebasejs/10.12.5/firebase-functions.js';

const functions = getFunctions(app, 'us-central1');

export class BillingService {
  constructor(user) { this.user = user || auth.currentUser; }
  async createCheckoutPreference(billingCycle = 'monthly') {
    if (!this.user || this.user.demo) throw new Error('Faça login com Firebase para assinar o Pro.');
    const callable = httpsCallable(functions, 'createCheckoutPreference');
    const result = await callable({ uid: this.user.uid, plan: 'pro', billingCycle });
    return result.data || {};
  }
  async getSubscription() {
    if (!this.user || this.user.demo) return { status: 'none', plan: 'free' };
    const snap = await getDoc(doc(db, 'users', this.user.uid, 'billing', 'subscription'));
    return snap.exists() ? { id: snap.id, ...snap.data() } : { status: 'none', plan: 'free' };
  }
  async listPayments(max = 10) {
    if (!this.user || this.user.demo) return [];
    const snap = await getDocs(query(collection(db, 'users', this.user.uid, 'billing', 'payments'), orderBy('updatedAt', 'desc'), limit(max)));
    return snap.docs.map((d) => ({ id: d.id, ...d.data() }));
  }
}
