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
  setDoc,
  updateDoc,
  increment
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
    status: doc.status || 'rascunho',
    publicToken: doc.publicToken || '',
    publicEnabled: Boolean(doc.publicEnabled),
    publicCreatedAt: doc.publicCreatedAt || '',
    publicLastAccessAt: doc.publicLastAccessAt || '',
    clientDecision: doc.clientDecision || 'pendente',
    clientDecisionAt: doc.clientDecisionAt || null,
    clientDecisionNote: doc.clientDecisionNote || '',
    issuerProfile: doc.issuerProfile || null
  };
}

function publicBaseUrl(){return `${window.location.origin}${window.location.pathname.replace(/\/[^/]*$/, '/')}`;}
function publicApprovalUrl(token){return `${publicBaseUrl()}aprovar.html?t=${encodeURIComponent(token)}`;}
function newPublicToken(){return crypto.randomUUID ? crypto.randomUUID() : `${Date.now()}-${Math.random().toString(36).slice(2)}-${uid()}`;}

function publicQuotePayload(token, saved, profile, ownerUid){
  const totals = calcDocument(saved);
  return {
    token,
    ownerUid,
    documentId: saved.id,
    publicEnabled: true,
    issuerPublicName: profile?.name || profile?.businessName || '',
    issuerPublicContact: [profile?.phone, profile?.email, profile?.city || profile?.address].filter(Boolean).join(' • '),
    documentNumber: saved.number || '',
    clientName: saved.clientName || '',
    issueDate: saved.issueDate || saved.date || '',
    validUntil: saved.validUntil || saved.dueDate || '',
    items: (saved.items || []).map((item) => ({ description: item.description || '', qty: Number(item.qty) || 0, unit: Number(item.unit) || 0, discount: Number(item.discount) || 0 })),
    subtotal: totals.subtotal,
    discount: totals.discount,
    total: totals.total,
    notes: saved.notes || '',
    status: saved.status || 'emitido',
    clientDecision: saved.clientDecision || 'pendente',
    clientDecisionAt: saved.clientDecisionAt || null,
    clientDecisionNote: saved.clientDecisionNote || '',
    createdAt: saved.publicCreatedAt || new Date().toISOString()
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
  period(){return new Date().toISOString().slice(0,7).replace('-','');}
  usageKey(){return this.key(`usage:${this.period()}`);}
  async getUserAccount(){return {...(this.user||{}),plan:(await this.getProfile()).plan||'free',isActive:true,isBlocked:false,acceptedTermsAt:new Date().toISOString(),acceptedPrivacyAt:new Date().toISOString()};}
  async acceptTerms(){}
  async getCurrentUsage(){return JSON.parse(localStorage.getItem(this.usageKey())||JSON.stringify({uid:this.user?.uid||'demo-user',period:this.period(),documentsCreated:0,budgetsCreated:0,receiptsCreated:0,pdfGenerated:0,publicLinksCreated:0,backupExports:0,chatbotQuestions:0}));}
  async incrementUsage(updates={}){const u=await this.getCurrentUsage();for(const [k,v] of Object.entries(updates))u[k]=Number(u[k]||0)+Number(v||0);u.lastActivityAt=new Date().toISOString();u.updatedAt=u.lastActivityAt;localStorage.setItem(this.usageKey(),JSON.stringify(u));return u;}
  async getProfile(){return JSON.parse(localStorage.getItem(this.key('profile'))||'{}');}
  async saveProfile(profile){localStorage.setItem(this.key('profile'),JSON.stringify({...normalizeProfile(profile,this.user),updatedAt:new Date().toISOString()}));}
  async listDocuments(){return JSON.parse(localStorage.getItem(this.key('docs'))||'[]').sort((a,b)=>String(b.createdAt).localeCompare(String(a.createdAt)));}
  async nextNumber(type){const docs=await this.listDocuments();const max=docs.filter(d=>d.type===type).reduce((m,d)=>Math.max(m,numberValue(d.number)),0);return formatNumber(type,max+1);}
  async saveDocument(docData){const docs=await this.listDocuments();const id=docData.id||uid();const idx=docs.findIndex(d=>d.id===id);const now=new Date().toISOString();const data={...normalizeDoc(docData),id,updatedAt:now,createdAt:docData.createdAt||now};if(idx>=0)docs[idx]=data;else {docs.unshift(data); const incs={documentsCreated:1}; if(data.type==='recibo') incs.receiptsCreated=1; else incs.budgetsCreated=1; await this.incrementUsage(incs);}localStorage.setItem(this.key('docs'),JSON.stringify(docs));return data;}
  async getDocumentById(id){return (await this.listDocuments()).find(d=>d.id===id)||null;}
  async enablePublicApproval(docData){const token=docData.publicToken||newPublicToken();const now=new Date().toISOString();const saved=await this.saveDocument({...docData,issuerProfile:docData.issuerProfile||await this.getProfile(),publicToken:token,publicEnabled:true,publicCreatedAt:docData.publicCreatedAt||now,publicLastAccessAt:docData.publicLastAccessAt||'',clientDecision:docData.clientDecision||'pendente',clientDecisionAt:docData.clientDecisionAt||null,clientDecisionNote:docData.clientDecisionNote||''});const index=JSON.parse(localStorage.getItem('orcafacil:publicQuotes')||'{}');index[token]=publicQuotePayload(token,saved,await this.getProfile(),this.user?.uid||'demo-user');localStorage.setItem('orcafacil:publicQuotes',JSON.stringify(index));return {...saved,publicUrl:publicApprovalUrl(token)};}
  async disablePublicApproval(docData){if(!docData?.id)throw new Error('Documento não encontrado.');const saved=await this.saveDocument({...docData,publicEnabled:false});if(docData.publicToken){const index=JSON.parse(localStorage.getItem('orcafacil:publicQuotes')||'{}');index[docData.publicToken]={...(index[docData.publicToken]||{}),token:docData.publicToken,ownerUid:this.user?.uid||'demo-user',documentId:docData.id,publicEnabled:false};localStorage.setItem('orcafacil:publicQuotes',JSON.stringify(index));}return saved;}
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
    const firstUserAgent=navigator.userAgent||''; const firstUrl=location.href;
    const base={uid:this.user.uid,name:name||this.user.displayName||this.user.email?.split('@')[0]||'',email:this.user.email||'',phone:'',plan:'free',role:'user',isActive:true,isBlocked:false,blockReason:'',acceptedTermsAt:null,acceptedPrivacyAt:null,updatedAt:serverTimestamp(),lastLoginAt:serverTimestamp(),lastSeenAt:serverTimestamp(),loginCount:increment(1),documentsCount:0,pdfGeneratedCount:0,freeLimitNotifiedAt:null,metadata:{firstUserAgent,lastUserAgent:firstUserAgent,firstUrl,lastUrl:firstUrl}};
    const old=snap.exists()?snap.data():{};
    await setDoc(ref,snap.exists()?{...base,phone:old.phone||'',plan:old.plan||'free',role:old.role||'user',isActive:old.isActive!==false,isBlocked:old.isBlocked===true,blockReason:old.blockReason||'',acceptedTermsAt:old.acceptedTermsAt||null,acceptedPrivacyAt:old.acceptedPrivacyAt||null,createdAt:old.createdAt||serverTimestamp(),documentsCount:old.documentsCount||0,pdfGeneratedCount:old.pdfGeneratedCount||0,metadata:{...(old.metadata||{}),lastUserAgent:firstUserAgent,lastUrl:firstUrl}}:{...base,createdAt:serverTimestamp()},{merge:true});
  }
  async getUserAccount(){const s=await getDoc(this.userRef());return s.exists()?{id:s.id,...s.data()}:null;}
  period(){return new Date().toISOString().slice(0,7).replace('-','');}
  usageRef(period=this.period()){return doc(db,'users',this.user.uid,'usage',period);}
  async getCurrentUsage(){const period=this.period();const s=await getDoc(this.usageRef(period));return s.exists()?{id:s.id,...s.data()}:{uid:this.user.uid,period,documentsCreated:0,budgetsCreated:0,receiptsCreated:0,pdfGenerated:0,publicLinksCreated:0,backupExports:0,chatbotQuestions:0};}
  async incrementUsage(updates={}){const period=this.period();const payload={uid:this.user.uid,period,lastActivityAt:serverTimestamp(),updatedAt:serverTimestamp()};for(const [k,v] of Object.entries(updates))payload[k]=increment(Number(v)||0);await setDoc(this.usageRef(period),payload,{merge:true});if(updates.pdfGenerated) await updateDoc(this.userRef(),{pdfGeneratedCount:increment(Number(updates.pdfGenerated)||0),lastSeenAt:serverTimestamp()});return this.getCurrentUsage();}
  async acceptTerms(){await setDoc(this.userRef(),{acceptedTermsAt:serverTimestamp(),acceptedPrivacyAt:serverTimestamp(),updatedAt:serverTimestamp()},{merge:true});}
  async getProfile(){const [userSnap, profileSnap]=await Promise.all([getDoc(this.userRef()),getDoc(this.profileRef())]);return normalizeProfile({...userSnap.data(),...profileSnap.data()},this.user);}
  async saveProfile(profile){const data=normalizeProfile(profile,this.user);const userSnap=await getDoc(this.userRef());const currentPlan=userSnap.exists()?(userSnap.data().plan||'free'):'free';await setDoc(this.profileRef(),{businessName:data.businessName,documentNumber:data.documentNumber,phone:data.phone,email:data.email,address:data.address,pix:data.pix,logoBase64:data.logoBase64,updatedAt:serverTimestamp()},{merge:true});await setDoc(this.userRef(),{name:data.businessName||data.name,email:this.user.email,plan:currentPlan,updatedAt:serverTimestamp()},{merge:true});}
  async listDocuments(){const q=query(this.docsCol(),orderBy('createdAt','desc'));const s=await getDocs(q);return s.docs.map(d=>({id:d.id,...d.data()}));}
  async nextNumber(type){const docs=await this.listDocuments();const max=docs.filter(d=>d.type===type).reduce((m,d)=>Math.max(m,numberValue(d.number)),0);return formatNumber(type,max+1);}
  async saveDocument(docData){const id=docData.id||uid();const ref=doc(db,'users',this.user.uid,'documents',id);const snap=await getDoc(ref);const now=new Date().toISOString();const data={...normalizeDoc(docData),id,updatedAt:now,createdAt:snap.exists()?(snap.data().createdAt||now):now};await setDoc(ref,data,{merge:true});if(!snap.exists()){const incs={documentsCreated:1}; if(data.type==='recibo') incs.receiptsCreated=1; else incs.budgetsCreated=1; await this.incrementUsage(incs); await updateDoc(this.userRef(),{documentsCount:increment(1),lastSeenAt:serverTimestamp()});}return data;}
  async getDocumentById(id){const snap=await getDoc(doc(db,'users',this.user.uid,'documents',id));return snap.exists()?{id:snap.id,...snap.data()}:null;}
  async enablePublicApproval(docData){const token=docData.publicToken||newPublicToken();const now=new Date().toISOString();const saved=await this.saveDocument({...docData,issuerProfile:docData.issuerProfile||await this.getProfile(),publicToken:token,publicEnabled:true,publicCreatedAt:docData.publicCreatedAt||now,publicLastAccessAt:docData.publicLastAccessAt||'',clientDecision:docData.clientDecision||'pendente',clientDecisionAt:docData.clientDecisionAt||null,clientDecisionNote:docData.clientDecisionNote||''});await setDoc(doc(db,'publicQuotes',token),publicQuotePayload(token,saved,await this.getProfile(),this.user.uid),{merge:true});await this.incrementUsage({publicLinksCreated:1});return {...saved,publicUrl:publicApprovalUrl(token)};}
  async disablePublicApproval(docData){if(!docData?.id)throw new Error('Documento não encontrado.');const saved=await this.saveDocument({...docData,publicEnabled:false});if(docData.publicToken){await setDoc(doc(db,'publicQuotes',docData.publicToken),{token:docData.publicToken,ownerUid:this.user.uid,documentId:docData.id,publicEnabled:false},{merge:true});}return saved;}
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
  enablePublicApproval: d => activeService.enablePublicApproval(d),
  disablePublicApproval: d => activeService.disablePublicApproval(d),
  getNextDocumentNumber: type => activeService.nextNumber(type)
};
