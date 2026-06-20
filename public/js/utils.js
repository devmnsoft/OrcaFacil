export const money = new Intl.NumberFormat('pt-BR',{style:'currency',currency:'BRL'});
export const todayISO = () => new Date().toISOString().slice(0,10);
export const today = todayISO;
export const uid = () => (crypto.randomUUID ? crypto.randomUUID() : `${Date.now()}-${Math.random()}`);
export function parseCurrency(v){
  if(typeof v === 'number') return Number.isFinite(v) ? v : 0;
  const raw = String(v||'').trim().replace(/[R$\s]/g,'');
  if(!raw) return 0;
  if(raw.includes(',')) return Number(raw.replace(/\./g,'').replace(',','.')) || 0;
  return Number(raw) || 0;
}
export const formatCurrency = value => money.format(Number(value)||0);
export const formatNumber = n => (Number(n)||0).toFixed(2).replace('.',',');
export function formatDateBR(date){if(!date)return'';const s=String(date);const [y,m,d]=s.slice(0,10).split('-');return y&&m&&d?`${d}/${m}/${y}`:s;}
export const onlyDigits = v => String(v||'').replace(/\D/g,'');
export function isValidEmail(v){return !v || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(v).trim());}
export function hasMinDigits(v,min){return onlyDigits(v).length===0 || onlyDigits(v).length>=min;}
export function readFileAsDataUrl(file){return new Promise((resolve,reject)=>{if(!file)return resolve('');const r=new FileReader();r.onload=()=>resolve(r.result);r.onerror=reject;r.readAsDataURL(file);});}
export function calcDocument(doc){const items=doc.items||[];const subtotal=items.reduce((s,i)=>s+(Number(i.qty)||0)*(Number(i.unit)||0),0);const discount=items.reduce((s,i)=>s+(Number(i.discount)||0),0);return{subtotal,discount,total:Math.max(0,subtotal-discount)};}
const unidades=['','um','dois','três','quatro','cinco','seis','sete','oito','nove'];
const especiais=['dez','onze','doze','treze','quatorze','quinze','dezesseis','dezessete','dezoito','dezenove'];
const dezenas=['','','vinte','trinta','quarenta','cinquenta','sessenta','setenta','oitenta','noventa'];
const centenas=['','cento','duzentos','trezentos','quatrocentos','quinhentos','seiscentos','setecentos','oitocentos','novecentos'];
function trio(n){n=Number(n);if(n===0)return'';if(n===100)return'cem';let c=Math.floor(n/100),d=Math.floor((n%100)/10),u=n%10;let p=[];if(c)p.push(centenas[c]);if(d===1)p.push(especiais[u]);else{if(d)p.push(dezenas[d]);if(u)p.push(unidades[u]);}return p.join(' e ');} 
function inteiroPorExtenso(n){n=Math.floor(n);if(n===0)return'zero';let mil=Math.floor(n/1000),rest=n%1000;let p=[];if(mil){p.push(mil===1?'mil':`${trio(mil)} mil`);}if(rest){p.push(trio(rest));}return p.join(rest<100&&mil?' e ':' ');} 
export function valorPorExtenso(valor){const inteiro=Math.floor(Number(valor)||0);const cent=Math.round(((Number(valor)||0)-inteiro)*100);let txt=`${inteiroPorExtenso(inteiro)} ${inteiro===1?'real':'reais'}`;if(cent>0)txt+=` e ${inteiroPorExtenso(cent)} ${cent===1?'centavo':'centavos'}`;return txt;}
export function escapeHtml(s){return String(s||'').replace(/[&<>"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));}
export function handleError(error, friendlyMessage='Não foi possível concluir agora. Verifique sua conexão e tente novamente.'){console.error(error);return friendlyMessage;}
