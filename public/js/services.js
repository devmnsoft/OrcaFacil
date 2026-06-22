import { auth, db } from './firebase-config.js';
import { BillingService } from './services/billing.service.js';
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
    timeline: Array.isArray(doc.timeline) ? doc.timeline : [],
    convertedReceiptId: doc.convertedReceiptId || '',
    convertedReceiptNumber: doc.convertedReceiptNumber || '',
    originBudgetId: doc.originBudgetId || '',
    originBudgetNumber: doc.originBudgetNumber || '',
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
function newPublicToken(){
  if (crypto.randomUUID) return `oqf_${crypto.randomUUID().replace(/-/g,'')}`;
  const bytes = new Uint8Array(16); crypto.getRandomValues(bytes);
  return `oqf_${Array.from(bytes, b => b.toString(16).padStart(2,'0')).join('')}`;
}

function publicQuotePayload(token, saved, profile, ownerUid){
  const totals = calcDocument(saved);
  const now = new Date().toISOString();
  const decision = saved.decision || { status: saved.clientDecision || 'pendente', note: saved.clientDecisionNote || '', decidedAt: saved.clientDecisionAt || null, decidedByName: '', decidedByDocument: '', decidedByEmail: '', acceptedTerms: false, ipInfo: null, userAgent: '', evidenceHash: '' };
  return {
    token,
    ownerUid,
    documentId: saved.id,
    type: 'orcamento',
    publicEnabled: true,
    issuer: { name: profile?.name || profile?.businessName || '', documentNumber: profile?.documentNumber || profile?.document || '', phone: profile?.phone || '', email: profile?.email || '', city: profile?.city || profile?.address || '', logoBase64: profile?.logoBase64 || profile?.logo || '' },
    client: { name: saved.clientName || '', document: saved.clientDoc || saved.clientDocument || '', phone: saved.clientContact || saved.clientPhone || '', email: saved.clientEmail || '', city: saved.clientCity || '' },
    document: { number: saved.number || '', issueDate: saved.issueDate || saved.date || '', validUntil: saved.validUntil || saved.dueDate || '', items: (saved.items || []).map((item) => ({ description: item.description || '', qty: Number(item.qty) || 0, unit: Number(item.unit) || 0, discount: Number(item.discount) || 0 })), subtotal: totals.subtotal, discount: totals.discount, total: totals.total, notes: saved.notes || '', status: saved.status || 'emitido' },
    decision,
    timeline: saved.timeline || [],
    expiresAt: saved.validUntil || saved.dueDate || '',
    lastAccessAt: saved.publicLastAccessAt || '',
    viewCount: Number(saved.publicViewCount || 0),
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
    updatedAt: now,
    createdAt: saved.publicCreatedAt || now
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
  async listDocuments(){let docs=JSON.parse(localStorage.getItem(this.key('docs'))||'[]').sort((a,b)=>String(b.createdAt).localeCompare(String(a.createdAt)));const pub=JSON.parse(localStorage.getItem('orcafacil:publicQuotes')||'{}');let changed=false;docs=docs.map(d=>{const q=d.publicToken&&pub[d.publicToken];if(q?.decision?.status&&q.decision.status!=='pendente'&&d.clientDecision!==q.decision.status){changed=true;return {...d,status:q.document?.status||q.decision.status,clientDecision:q.decision.status,clientDecisionAt:q.decision.decidedAt,clientDecisionNote:q.decision.note,evidenceHash:q.decision.evidenceHash,publicViewCount:q.viewCount||d.publicViewCount,timeline:q.timeline||d.timeline};}return q?{...d,publicViewCount:q.viewCount||d.publicViewCount,publicLastAccessAt:q.lastAccessAt||d.publicLastAccessAt,status:q.document?.status||d.status,timeline:q.timeline||d.timeline}:d;});if(changed)localStorage.setItem(this.key('docs'),JSON.stringify(docs));return docs;}
  async nextNumber(type){const docs=await this.listDocuments();const max=docs.filter(d=>d.type===type).reduce((m,d)=>Math.max(m,numberValue(d.number)),0);return formatNumber(type,max+1);}
  async saveDocument(docData){const docs=await this.listDocuments();const id=docData.id||uid();const idx=docs.findIndex(d=>d.id===id);const now=new Date().toISOString();const timeline=[...(docData.timeline||[])]; if(!docData.id&&!timeline.length) timeline.push({id:uid(),type:'created',title:'Documento criado',message:'Documento criado no OrçaFácil.',createdAt:now,source:'user',metadata:{}}); const data={...normalizeDoc(docData),id,timeline,updatedAt:now,createdAt:docData.createdAt||now};if(idx>=0)docs[idx]=data;else {docs.unshift(data); const incs={documentsCreated:1}; if(data.type==='recibo') incs.receiptsCreated=1; else incs.budgetsCreated=1; await this.incrementUsage(incs);}localStorage.setItem(this.key('docs'),JSON.stringify(docs));return data;}
  async getDocumentById(id){return (await this.listDocuments()).find(d=>d.id===id)||null;}
  async enablePublicApproval(docData){const token=docData.publicToken||newPublicToken();const now=new Date().toISOString();const saved=await this.saveDocument({...docData,issuerProfile:docData.issuerProfile||await this.getProfile(),publicToken:token,publicEnabled:true,status: docData.status==='rascunho'?'emitido':docData.status,publicCreatedAt:docData.publicCreatedAt||now,publicLastAccessAt:docData.publicLastAccessAt||'',clientDecision:docData.clientDecision||'pendente',clientDecisionAt:docData.clientDecisionAt||null,clientDecisionNote:docData.clientDecisionNote||''});const index=JSON.parse(localStorage.getItem('orcafacil:publicQuotes')||'{}');index[token]=publicQuotePayload(token,saved,await this.getProfile(),this.user?.uid||'demo-user');localStorage.setItem('orcafacil:publicQuotes',JSON.stringify(index));return {...saved,publicUrl:publicApprovalUrl(token)};}
  async disablePublicApproval(docData){if(!docData?.id)throw new Error('Documento não encontrado.');const saved=await this.saveDocument({...docData,publicEnabled:false});if(docData.publicToken){const index=JSON.parse(localStorage.getItem('orcafacil:publicQuotes')||'{}');index[docData.publicToken]={...(index[docData.publicToken]||{}),token:docData.publicToken,ownerUid:this.user?.uid||'demo-user',documentId:docData.id,publicEnabled:false};localStorage.setItem('orcafacil:publicQuotes',JSON.stringify(index));}return saved;}
  async convertBudgetToReceipt(budget){if(!budget?.id||budget.type!=='orcamento'||budget.status!=='aprovado')throw new Error('Somente orçamento aprovado pode virar recibo.');const number=await this.nextNumber('recibo');const now=new Date().toISOString();const receipt=await this.saveDocument({...budget,id:'',type:'recibo',number,status:'emitido',publicToken:'',publicEnabled:false,clientDecision:'pendente',notes:`Recibo gerado a partir do orçamento ${budget.number}.`,originBudgetId:budget.id,originBudgetNumber:budget.number,timeline:[{id:uid(),type:'converted_from_budget',title:'Recibo criado',message:`Recibo gerado a partir do orçamento ${budget.number}.`,createdAt:now,source:'user',metadata:{budgetId:budget.id}}]});await this.saveDocument({...budget,status:'convertido',convertedReceiptId:receipt.id,convertedReceiptNumber:receipt.number,timeline:[...(budget.timeline||[]),{id:uid(),type:'converted',title:'Convertido em recibo',message:`Recibo ${receipt.number} criado a partir deste orçamento.`,createdAt:now,source:'user',metadata:{receiptId:receipt.id}}]});return receipt;}
  async deleteDocument(id){localStorage.setItem(this.key('docs'),JSON.stringify((await this.listDocuments()).filter(d=>d.id!==id)));}
  async getSubscription(){return {status:'none',plan:(await this.getProfile()).plan||'free',billingCycle:'monthly'};}
  async listPayments(){return [];}
  async createCheckoutPreference(){throw new Error('Checkout indisponível no modo demonstração. Faça login com Firebase.');}
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
  async listDocuments(){const q=query(this.docsCol(),orderBy('createdAt','desc'));const s=await getDocs(q);const docs=s.docs.map(d=>({id:d.id,...d.data()}));return Promise.all(docs.map(async d=>{if(!d.publicToken)return d;try{const ps=await getDoc(doc(db,'publicQuotes',d.publicToken));if(!ps.exists())return d;const pub=ps.data();const merged={...d,publicViewCount:pub.viewCount||d.publicViewCount,publicLastAccessAt:pub.lastAccessAt||d.publicLastAccessAt,status:pub.document?.status||d.status,timeline:pub.timeline||d.timeline};if(pub.decision?.status&&pub.decision.status!=='pendente'&&d.clientDecision!==pub.decision.status){const next={...merged,status:pub.document?.status||pub.decision.status,clientDecision:pub.decision.status,clientDecisionAt:pub.decision.decidedAt,clientDecisionNote:pub.decision.note,evidenceHash:pub.decision.evidenceHash};await this.saveDocument(next);return next;}return merged;}catch{return d;}}));}
  async nextNumber(type){const docs=await this.listDocuments();const max=docs.filter(d=>d.type===type).reduce((m,d)=>Math.max(m,numberValue(d.number)),0);return formatNumber(type,max+1);}
  async saveDocument(docData){const id=docData.id||uid();const ref=doc(db,'users',this.user.uid,'documents',id);const snap=await getDoc(ref);const now=new Date().toISOString();const timeline=[...(docData.timeline||[])]; if(!snap.exists()&&!timeline.length) timeline.push({id:uid(),type:'created',title:'Documento criado',message:'Documento criado no OrçaFácil.',createdAt:now,source:'user',metadata:{}}); const data={...normalizeDoc(docData),id,timeline,updatedAt:now,createdAt:snap.exists()?(snap.data().createdAt||now):now};await setDoc(ref,data,{merge:true});if(!snap.exists()){const incs={documentsCreated:1}; if(data.type==='recibo') incs.receiptsCreated=1; else incs.budgetsCreated=1; await this.incrementUsage(incs); await updateDoc(this.userRef(),{documentsCount:increment(1),lastSeenAt:serverTimestamp()});}return data;}
  async getDocumentById(id){const snap=await getDoc(doc(db,'users',this.user.uid,'documents',id));return snap.exists()?{id:snap.id,...snap.data()}:null;}
  async enablePublicApproval(docData){const token=docData.publicToken||newPublicToken();const now=new Date().toISOString();const saved=await this.saveDocument({...docData,issuerProfile:docData.issuerProfile||await this.getProfile(),publicToken:token,publicEnabled:true,status: docData.status==='rascunho'?'emitido':docData.status,publicCreatedAt:docData.publicCreatedAt||now,publicLastAccessAt:docData.publicLastAccessAt||'',clientDecision:docData.clientDecision||'pendente',clientDecisionAt:docData.clientDecisionAt||null,clientDecisionNote:docData.clientDecisionNote||''});await setDoc(doc(db,'publicQuotes',token),publicQuotePayload(token,saved,await this.getProfile(),this.user.uid),{merge:true});await this.incrementUsage({publicLinksCreated:1});return {...saved,publicUrl:publicApprovalUrl(token)};}
  async disablePublicApproval(docData){if(!docData?.id)throw new Error('Documento não encontrado.');const saved=await this.saveDocument({...docData,publicEnabled:false});if(docData.publicToken){await setDoc(doc(db,'publicQuotes',docData.publicToken),{token:docData.publicToken,ownerUid:this.user.uid,documentId:docData.id,publicEnabled:false},{merge:true});}return saved;}
  async convertBudgetToReceipt(budget){if(!budget?.id||budget.type!=='orcamento'||budget.status!=='aprovado')throw new Error('Somente orçamento aprovado pode virar recibo.');const number=await this.nextNumber('recibo');const now=new Date().toISOString();const receipt=await this.saveDocument({...budget,id:'',type:'recibo',number,status:'emitido',publicToken:'',publicEnabled:false,clientDecision:'pendente',notes:`Recibo gerado a partir do orçamento ${budget.number}.`,originBudgetId:budget.id,originBudgetNumber:budget.number,timeline:[{id:uid(),type:'converted_from_budget',title:'Recibo criado',message:`Recibo gerado a partir do orçamento ${budget.number}.`,createdAt:now,source:'user',metadata:{budgetId:budget.id}}]});await this.saveDocument({...budget,status:'convertido',convertedReceiptId:receipt.id,convertedReceiptNumber:receipt.number,timeline:[...(budget.timeline||[]),{id:uid(),type:'converted',title:'Convertido em recibo',message:`Recibo ${receipt.number} criado a partir deste orçamento.`,createdAt:now,source:'user',metadata:{receiptId:receipt.id}}]});return receipt;}
  async deleteDocument(id){await deleteDoc(doc(db,'users',this.user.uid,'documents',id));}

  async getSubscription(){return new BillingService(this.user).getSubscription();}
  async listPayments(max=10){return new BillingService(this.user).listPayments(max);}
  async createCheckoutPreference(billingCycle='monthly'){return new BillingService(this.user).createCheckoutPreference(billingCycle);}
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
  getNextDocumentNumber: type => activeService.nextNumber(type),
  convertBudgetToReceipt: d => activeService.convertBudgetToReceipt(d)
};
