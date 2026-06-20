import { firebaseConfig } from './firebase-config.js';
import { uid } from './utils.js';

const isConfigured = Boolean(firebaseConfig.apiKey && firebaseConfig.projectId);
let fb = null;

async function loadFirebase(){
  if(!isConfigured) return null;
  const appMod = await import('https://www.gstatic.com/firebasejs/10.12.4/firebase-app.js');
  const authMod = await import('https://www.gstatic.com/firebasejs/10.12.4/firebase-auth.js');
  const fsMod = await import('https://www.gstatic.com/firebasejs/10.12.4/firebase-firestore.js');
  const app = appMod.initializeApp(firebaseConfig);
  return { app, ...authMod, ...fsMod, auth: authMod.getAuth(app), db: fsMod.getFirestore(app) };
}

class LocalService{
  constructor(){this.mode='demo';this.user=null;}
  key(k){return `orcafacil:${this.user?.uid||'demo'}:${k}`;}
  async init(){this.user=JSON.parse(localStorage.getItem('orcafacil:user')||'null');return {configured:false,mode:'demo'};}
  onAuth(cb){setTimeout(()=>cb(this.user),0);return()=>{};}
  async login(email){this.user={uid:'demo-user',email:email||'demo@orcafacil.app'};localStorage.setItem('orcafacil:user',JSON.stringify(this.user));return this.user;}
  async register(email){return this.login(email);}
  async demo(){return this.login('demo@orcafacil.app');}
  async logout(){localStorage.removeItem('orcafacil:user');this.user=null;}
  async getProfile(){return JSON.parse(localStorage.getItem(this.key('profile'))||'{}');}
  async saveProfile(profile){localStorage.setItem(this.key('profile'),JSON.stringify({...profile,updatedAt:new Date().toISOString()}));}
  async nextNumber(type){const k=this.key(`counter:${type}`);const n=Number(localStorage.getItem(k)||0)+1;localStorage.setItem(k,String(n));return n;}
  async listDocuments(){return JSON.parse(localStorage.getItem(this.key('docs'))||'[]').sort((a,b)=>String(b.createdAt).localeCompare(String(a.createdAt)));}
  async saveDocument(doc){const docs=await this.listDocuments();if(!doc.id) doc.id=uid();const idx=docs.findIndex(d=>d.id===doc.id);const now=new Date().toISOString();const data={...doc,updatedAt:now,createdAt:doc.createdAt||now};if(idx>=0)docs[idx]=data;else docs.unshift(data);localStorage.setItem(this.key('docs'),JSON.stringify(docs));return data;}
  async deleteDocument(id){const docs=(await this.listDocuments()).filter(d=>d.id!==id);localStorage.setItem(this.key('docs'),JSON.stringify(docs));}
}

class FirebaseService{
  constructor(f){this.mode='firebase';this.f=f;this.user=null;}
  async init(){return {configured:true,mode:'firebase'};}
  onAuth(cb){return this.f.onAuthStateChanged(this.f.auth,u=>{this.user=u;cb(u);});}
  async login(email,password){const res=await this.f.signInWithEmailAndPassword(this.f.auth,email,password);this.user=res.user;return res.user;}
  async register(email,password){const res=await this.f.createUserWithEmailAndPassword(this.f.auth,email,password);this.user=res.user;return res.user;}
  async demo(){throw new Error('Demonstração local indisponível em modo Firebase.');}
  async logout(){await this.f.signOut(this.f.auth);this.user=null;}
  userRef(){return this.f.doc(this.f.db,'users',this.user.uid);}
  docsCol(){return this.f.collection(this.f.db,'users',this.user.uid,'documents');}
  async getProfile(){const snap=await this.f.getDoc(this.userRef());return snap.exists()?snap.data():{};}
  async saveProfile(profile){await this.f.setDoc(this.userRef(),{...profile,email:this.user.email,updatedAt:this.f.serverTimestamp()},{merge:true});}
  async nextNumber(type){const profile=await this.getProfile();const counters=profile.counters||{};const n=Number(counters[type]||0)+1;await this.f.setDoc(this.userRef(),{counters:{...counters,[type]:n}},{merge:true});return n;}
  async listDocuments(){const q=this.f.query(this.docsCol(),this.f.orderBy('createdAt','desc'));const s=await this.f.getDocs(q);return s.docs.map(d=>({id:d.id,...d.data()}));}
  async saveDocument(doc){const now=new Date().toISOString();const id=doc.id||uid();await this.f.setDoc(this.f.doc(this.f.db,'users',this.user.uid,'documents',id),{...doc,id,updatedAt:now,createdAt:doc.createdAt||now},{merge:true});return {...doc,id};}
  async deleteDocument(id){await this.f.deleteDoc(this.f.doc(this.f.db,'users',this.user.uid,'documents',id));}
}

export async function createService(){
  fb = await loadFirebase();
  return fb ? new FirebaseService(fb) : new LocalService();
}
