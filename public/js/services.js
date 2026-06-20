import { auth, db } from './firebase-config.js';
import { uid, calcDocument } from './utils.js';
import {
  createUserWithEmailAndPassword,
  onAuthStateChanged,
  signInWithEmailAndPassword,
  signOut,
  updateProfile
} from 'https://www.gstatic.com/firebasejs/10.12.5/firebase-auth.js';
import {
  collection,
  deleteDoc,
  doc,
  getDoc,
  getDocs,
  orderBy,
  query,
  serverTimestamp,
  setDoc
} from 'https://www.gstatic.com/firebasejs/10.12.5/firebase-firestore.js';

const PREFIX = { orcamento: 'ORC', recibo: 'REC' };
const DEMO_KEY = 'orcafacil:demo-enabled';

function friendlyError(err){
  const code = err?.code || '';
  const messages = {
    'auth/invalid-email': 'E-mail inválido.',
    'auth/weak-password': 'Senha fraca. Use pelo menos 6 caracteres.',
    'auth/user-not-found': 'Usuário não encontrado.',
    'auth/invalid-credential': 'E-mail ou senha incorretos.',
    'auth/wrong-password': 'Senha incorreta.',
    'auth/email-already-in-use': 'E-mail já cadastrado.',
    'auth/network-request-failed': 'Erro de conexão. Verifique sua internet.',
    'auth/operation-not-allowed': 'Operação não permitida. Habilite e-mail/senha no Firebase Authentication.'
  };
  return new Error(messages[code] || err?.message || 'Não foi possível concluir a operação.');
}
function numberValue(number){
  const m = String(number || '').match(/(\d+)$/);
  return m ? Number(m[1]) : Number(number) || 0;
}
function formatNumber(type, n){return `${PREFIX[type] || 'DOC'}-${String(n).padStart(6,'0')}`;}
function normalizeDoc(doc){
  const totals = calcDocument(doc);
  return {
    id: doc.id || '',
    type: doc.type || 'orcamento',
    number: doc.number || '',
    clientName: doc.clientName || '',
    clientDocument: doc.clientDocument || doc.clientDoc || '',
    clientPhone: doc.clientPhone || doc.clientContact || '',
    clientEmail: doc.clientEmail || '',
    clientCity: doc.clientCity || '',
    issueDate: doc.issueDate || doc.date || '',
    dueDate: doc.dueDate || doc.validUntil || '',
    date: doc.date || doc.issueDate || '',
    validUntil: doc.validUntil || doc.dueDate || '',
    clientDoc: doc.clientDoc || doc.clientDocument || '',
    clientContact: doc.clientContact || doc.clientPhone || '',
    items: doc.items || [],
    subtotal: totals.subtotal,
    discount: totals.discount,
    total: totals.total,
    notes: doc.notes || '',
    status: doc.status || 'rascunho'
  };
}
function normalizeProfile(profile, user){
  return {
    businessName: profile.businessName || profile.name || '',
    documentNumber: profile.documentNumber || profile.document || '',
    phone: profile.phone || '',
    email: profile.email || user?.email || '',
    address: profile.address || profile.city || '',
    pix: profile.pix || '',
    logoBase64: profile.logoBase64 || profile.logo || '',
    plan: profile.plan || 'free',
    name: profile.name || profile.businessName || '',
    document: profile.document || profile.documentNumber || '',
    city: profile.city || profile.address || '',
    logo: profile.logo || profile.logoBase64 || ''
  };
}

class LocalService{
  constructor(){this.mode='demo';this.user=null;}
  key(k){return `orcafacil:${this.user?.uid||'demo-user'}:${k}`;}
  async init(){this.user=JSON.parse(localStorage.getItem('orcafacil:user')||'null');return {configured:true,mode:'firebase'};}
  onAuth(cb){setTimeout(()=>cb(localStorage.getItem(DEMO_KEY)==='1'?this.user:null),0);return()=>{};}
  async login(email){this.user={uid:'demo-user',email:email||'demo@orcafacil.app',displayName:'Usuário demonstração',demo:true};localStorage.setItem(DEMO_KEY,'1');localStorage.setItem('orcafacil:user',JSON.stringify(this.user));return this.user;}
  async register(email){return this.login(email);}
  async demo(){return this.login('demo@orcafacil.app');}
  async logout(){localStorage.removeItem(DEMO_KEY);localStorage.removeItem('orcafacil:user');this.user=null;}
  async ensureUserDocument(){}
  async getProfile(){return JSON.parse(localStorage.getItem(this.key('profile'))||'{}');}
  async saveProfile(profile){localStorage.setItem(this.key('profile'),JSON.stringify({...normalizeProfile(profile,this.user),updatedAt:new Date().toISOString()}));}
  async listDocuments(){return JSON.parse(localStorage.getItem(this.key('docs'))||'[]').sort((a,b)=>String(b.createdAt).localeCompare(String(a.createdAt)));}
  async nextNumber(type){const docs=await this.listDocuments();const max=docs.filter(d=>d.type===type).reduce((m,d)=>Math.max(m,numberValue(d.number)),0);return formatNumber(type,max+1);}
  async saveDocument(docData){const docs=await this.listDocuments();const id=docData.id||uid();const idx=docs.findIndex(d=>d.id===id);const now=new Date().toISOString();const data={...normalizeDoc(docData),id,updatedAt:now,createdAt:docData.createdAt||now};if(idx>=0)docs[idx]=data;else docs.unshift(data);localStorage.setItem(this.key('docs'),JSON.stringify(docs));return data;}
  async getDocumentById(id){return (await this.listDocuments()).find(d=>d.id===id)||null;}
  async deleteDocument(id){localStorage.setItem(this.key('docs'),JSON.stringify((await this.listDocuments()).filter(d=>d.id!==id)));}
  async duplicateDocument(id){const doc=await this.getDocumentById(id);if(!doc)throw new Error('Documento não encontrado.');return {...doc,id:'',number:'',clientName:`${doc.clientName} (cópia)`};}
}

class FirebaseService{
  constructor(){this.mode='firebase';this.user=null;}
  async init(){return {configured:true,mode:'firebase'};}
  onAuth(cb){return onAuthStateChanged(auth,async u=>{this.user=u;if(u)await this.ensureUserDocument();cb(u);});}
  async login(email,password){try{const res=await signInWithEmailAndPassword(auth,email,password);this.user=res.user;await this.ensureUserDocument();return res.user;}catch(e){throw friendlyError(e);}}
  async register(email,password,name=''){try{const res=await createUserWithEmailAndPassword(auth,email,password);this.user=res.user;if(name)await updateProfile(res.user,{displayName:name});await this.ensureUserDocument(name);return res.user;}catch(e){throw friendlyError(e);}}
  async demo(){const local=new LocalService();return local.demo();}
  async logout(){localStorage.removeItem(DEMO_KEY);await signOut(auth);this.user=null;}
  userRef(){return doc(db,'users',this.user.uid);}
  profileRef(){return doc(db,'users',this.user.uid,'settings','profile');}
  docsCol(){return collection(db,'users',this.user.uid,'documents');}
  async ensureUserDocument(name=''){
    if(!this.user)return;
    const ref=this.userRef(); const snap=await getDoc(ref);
    const base={uid:this.user.uid,name:name||this.user.displayName||this.user.email?.split('@')[0]||'',email:this.user.email||'',plan:'free',updatedAt:serverTimestamp()};
    await setDoc(ref,snap.exists()?{...base,plan:snap.data().plan||'free'}:{...base,createdAt:serverTimestamp()},{merge:true});
  }
  async getProfile(){const [userSnap, profileSnap]=await Promise.all([getDoc(this.userRef()),getDoc(this.profileRef())]);return normalizeProfile({...userSnap.data(),...profileSnap.data()},this.user);}
  async saveProfile(profile){const data=normalizeProfile(profile,this.user);const userSnap=await getDoc(this.userRef());const currentPlan=userSnap.exists()?(userSnap.data().plan||'free'):'free';await setDoc(this.profileRef(),{businessName:data.businessName,documentNumber:data.documentNumber,phone:data.phone,email:data.email,address:data.address,pix:data.pix,logoBase64:data.logoBase64,updatedAt:serverTimestamp()},{merge:true});await setDoc(this.userRef(),{name:data.businessName||data.name,email:this.user.email,plan:currentPlan,updatedAt:serverTimestamp()},{merge:true});}
  async listDocuments(){const q=query(this.docsCol(),orderBy('createdAt','desc'));const s=await getDocs(q);return s.docs.map(d=>({id:d.id,...d.data()}));}
  async nextNumber(type){const docs=await this.listDocuments();const max=docs.filter(d=>d.type===type).reduce((m,d)=>Math.max(m,numberValue(d.number)),0);return formatNumber(type,max+1);}
  async saveDocument(docData){const id=docData.id||uid();const ref=doc(db,'users',this.user.uid,'documents',id);const snap=await getDoc(ref);const now=new Date().toISOString();const data={...normalizeDoc(docData),id,updatedAt:now,createdAt:snap.exists()?(snap.data().createdAt||now):now};await setDoc(ref,data,{merge:true});return data;}
  async getDocumentById(id){const snap=await getDoc(doc(db,'users',this.user.uid,'documents',id));return snap.exists()?{id:snap.id,...snap.data()}:null;}
  async deleteDocument(id){await deleteDoc(doc(db,'users',this.user.uid,'documents',id));}
  async duplicateDocument(id){const doc=await this.getDocumentById(id);if(!doc)throw new Error('Documento não encontrado.');return {...doc,id:'',number:'',clientName:`${doc.clientName} (cópia)`};}
}

let activeService = null;
export async function createService(){activeService = localStorage.getItem(DEMO_KEY)==='1' ? new LocalService() : new FirebaseService(); return activeService;}
export const storage = {
  saveProfile: p => activeService.saveProfile(p),
  getProfile: () => activeService.getProfile(),
  saveDocument: d => activeService.saveDocument(d),
  getDocuments: () => activeService.listDocuments(),
  getDocumentById: id => activeService.getDocumentById(id),
  deleteDocument: id => activeService.deleteDocument(id),
  duplicateDocument: id => activeService.duplicateDocument(id),
  getNextDocumentNumber: type => activeService.nextNumber(type)
};
