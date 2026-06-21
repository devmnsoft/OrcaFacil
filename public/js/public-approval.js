import { db } from './firebase-config.js';
import { doc, getDoc, updateDoc } from 'https://www.gstatic.com/firebasejs/10.12.5/firebase-firestore.js';
import { generatePdf } from './pdf.js';
import { calcDocument, escapeHtml, formatCurrency, formatDateBR, handleError } from './utils.js';

const $ = sel => document.querySelector(sel);
const token = new URLSearchParams(location.search).get('t') || '';
let currentDoc = null, docRef = null;

function unavailable(msg='Este link não está mais disponível.'){$('#publicApp').innerHTML=`<div class="card border-0 shadow-sm"><div class="card-body p-5 text-center"><i class="bi bi-link-45deg fs-1 text-secondary"></i><h1 class="h4 mt-3">${escapeHtml(msg)}</h1><p class="text-secondary mb-0">Solicite um novo link ao prestador.</p></div></div>`;}
function decisionBadge(d){const map={aprovado:'success',recusado:'danger',pendente:'secondary'};return `<span class="badge text-bg-${map[d]||'secondary'}">${d==='aprovado'?'Aprovado pelo cliente':d==='recusado'?'Recusado pelo cliente':'Pendente'}</span>`;}
function demoIndex(){return JSON.parse(localStorage.getItem('orcafacil:publicQuotes')||'{}');}
async function loadPublic(){
  if(!token) return unavailable('Token público não informado.');
  const demo = demoIndex()[token];
  if(demo){
    if(!demo.publicEnabled) return unavailable();
    const docs=JSON.parse(localStorage.getItem(`orcafacil:${demo.ownerUid}:docs`)||'[]');
    const found=docs.find(d=>d.id===demo.documentId);
    if(!found || found.type!=='orcamento' || !found.publicEnabled || found.publicToken!==token) return unavailable();
    currentDoc=found; docRef={demo:true,ownerUid:demo.ownerUid,documentId:demo.documentId}; return render();
  }
  const idxSnap=await getDoc(doc(db,'publicQuotes',token));
  if(!idxSnap.exists() || idxSnap.data().publicEnabled!==true) return unavailable();
  const idx=idxSnap.data();
  const ref=doc(db,'users',idx.ownerUid,'documents',idx.documentId);
  const snap=await getDoc(ref);
  if(!snap.exists()) return unavailable();
  const data={id:snap.id,...snap.data()};
  if(data.type!=='orcamento'||data.publicEnabled!==true||data.publicToken!==token) return unavailable();
  currentDoc=data; docRef=ref; render();
}
function render(){
  const d=currentDoc, totals=calcDocument(d), p=d.issuerProfile||{};
  $('#publicApp').innerHTML=`<div class="row justify-content-center"><div class="col-lg-10"><div class="card border-0 shadow-sm"><div class="card-body p-4 p-lg-5">
    <div class="d-flex flex-column flex-md-row justify-content-between gap-3 mb-4"><div><p class="text-uppercase text-primary fw-bold small mb-1">Confira seu orçamento</p><h1 class="h3 fw-bold mb-1">Orçamento Nº ${escapeHtml(d.number||'-')}</h1><p class="text-secondary mb-0">Data: ${formatDateBR(d.date)}${d.validUntil?` • Validade: ${formatDateBR(d.validUntil)}`:''}</p></div><div class="text-md-end"><div>Status da decisão</div>${decisionBadge(d.clientDecision||'pendente')}</div></div>
    <div class="row g-3 mb-4"><div class="col-md-6"><div class="p-3 bg-light rounded h-100"><h2 class="h6 fw-bold">Emitente</h2><div>${escapeHtml(p.name||p.businessName||'Emitente')}</div><small class="text-secondary">${escapeHtml([p.document||p.documentNumber,p.phone,p.email,p.city||p.address].filter(Boolean).join(' • '))}</small></div></div><div class="col-md-6"><div class="p-3 bg-light rounded h-100"><h2 class="h6 fw-bold">Cliente</h2><div>${escapeHtml(d.clientName||'-')}</div><small class="text-secondary">${escapeHtml([d.clientDoc,d.clientContact,d.clientCity].filter(Boolean).join(' • '))}</small></div></div></div>
    <div class="table-responsive"><table class="table align-middle"><thead><tr><th>Item</th><th class="text-end">Qtd.</th><th class="text-end">Valor</th><th class="text-end">Desc.</th><th class="text-end">Total</th></tr></thead><tbody>${(d.items||[]).map(i=>`<tr><td>${escapeHtml(i.description||'-')}</td><td class="text-end">${i.qty||0}</td><td class="text-end">${formatCurrency(i.unit)}</td><td class="text-end">${formatCurrency(i.discount)}</td><td class="text-end fw-semibold">${formatCurrency((Number(i.qty)||0)*(Number(i.unit)||0)-(Number(i.discount)||0))}</td></tr>`).join('')}</tbody></table></div>
    <div class="row justify-content-end"><div class="col-md-5"><div class="p-3 bg-light rounded"><div class="d-flex justify-content-between"><span>Subtotal</span><strong>${formatCurrency(totals.subtotal)}</strong></div><div class="d-flex justify-content-between"><span>Desconto</span><strong>${formatCurrency(totals.discount)}</strong></div><hr><div class="d-flex justify-content-between fs-4"><span>Total</span><strong class="text-primary">${formatCurrency(totals.total)}</strong></div></div></div></div>
    ${d.notes?`<div class="alert alert-light border mt-4"><strong>Observações/condições comerciais</strong><br>${escapeHtml(d.notes)}</div>`:''}
    <div class="mt-4"><label class="form-label">Mensagem para o prestador (opcional)</label><textarea id="decisionNote" class="form-control" rows="3" placeholder="Escreva uma observação sobre sua decisão...">${escapeHtml(d.clientDecisionNote||'')}</textarea></div>
    <div id="decisionAlert" class="mt-3"></div><div class="d-grid d-md-flex gap-2 mt-4"><button class="btn btn-success btn-lg" id="btnApprove"><i class="bi bi-check2-circle"></i> Aprovar orçamento</button><button class="btn btn-outline-danger btn-lg" id="btnReject"><i class="bi bi-x-circle"></i> Recusar orçamento</button><button class="btn btn-outline-primary btn-lg" id="btnPdfPublic"><i class="bi bi-file-earmark-pdf"></i> Baixar PDF</button></div>
  </div></div></div></div>`;
  $('#btnApprove').onclick=()=>decide('aprovado'); $('#btnReject').onclick=()=>decide('recusado'); $('#btnPdfPublic').onclick=()=>generatePdf(currentDoc,p);
}
async function decide(decision){
  if(!confirm(decision==='aprovado'?'Confirmar aprovação deste orçamento?':'Confirmar recusa deste orçamento?')) return;
  const payload={clientDecision:decision,clientDecisionAt:new Date().toISOString(),clientDecisionNote:$('#decisionNote').value.trim(),status:decision==='aprovado'?'aprovado':'cancelado'};
  if(docRef.demo){const key=`orcafacil:${docRef.ownerUid}:docs`, docs=JSON.parse(localStorage.getItem(key)||'[]'), i=docs.findIndex(d=>d.id===docRef.documentId);docs[i]={...docs[i],...payload};localStorage.setItem(key,JSON.stringify(docs));currentDoc=docs[i];render();}
  else {await updateDoc(docRef,payload);currentDoc={...currentDoc,...payload};render();}
  $('#decisionAlert').innerHTML=`<div class="alert alert-success">${decision==='aprovado'?'Orçamento aprovado com sucesso. O prestador será notificado pelo histórico do sistema.':'Resposta registrada. O prestador poderá visualizar sua mensagem no sistema.'}</div>`;
}
loadPublic().catch(err=>{console.error(err);unavailable(handleError(err,'Não foi possível carregar este orçamento.'));});
