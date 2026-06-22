import { db } from './firebase-config.js';
import { doc, getDoc, updateDoc, increment, arrayUnion } from 'https://www.gstatic.com/firebasejs/10.12.5/firebase-firestore.js';
import { generatePdf } from './pdf.js';
import { calcDocument, escapeHtml, formatCurrency, formatDateBR, handleError, uid } from './utils.js';
import { getDocumentStatusMeta } from './domain/document-status.model.js';

const $ = sel => document.querySelector(sel);
const token = new URLSearchParams(location.search).get('t') || '';
let currentDoc = null, docRef = null;

function nowIso(){return new Date().toISOString();}
function demoIndex(){return JSON.parse(localStorage.getItem('orcafacil:publicQuotes')||'{}');}
function sanitize(v,max=1000){return String(v||'').replace(/[<>]/g,'').slice(0,max).trim();}
async function sha256(text){const bytes=new TextEncoder().encode(text);const hash=await crypto.subtle.digest('SHA-256',bytes);return Array.from(new Uint8Array(hash),b=>b.toString(16).padStart(2,'0')).join('');}
function unavailable(title='Este link não está mais disponível.', msg='Solicite um novo link ao prestador.'){$('#publicApp').innerHTML=`<div class="card border-0 shadow-sm"><div class="card-body p-5 text-center"><i class="bi bi-link-45deg fs-1 text-secondary"></i><h1 class="h4 mt-3">${escapeHtml(title)}</h1><p class="text-secondary mb-0">${escapeHtml(msg)}</p></div></div>`;}
function normalize(data){
  const document=data.document||{}; const issuer=data.issuer||{name:data.issuerPublicName,phone:data.issuerPublicContact}; const client=data.client||{name:data.clientName};
  return {...data,issuer,client,document:{...document,number:document.number||data.documentNumber,issueDate:document.issueDate||data.issueDate,validUntil:document.validUntil||data.validUntil,items:document.items||data.items||[],subtotal:document.subtotal??data.subtotal,discount:document.discount??data.discount,total:document.total??data.total,notes:document.notes??data.notes,status:document.status||data.status||'emitido'},decision:data.decision||{status:data.clientDecision||'pendente',note:data.clientDecisionNote||'',decidedAt:data.clientDecisionAt||null,decidedByName:'',evidenceHash:data.evidenceHash||''},timeline:data.timeline||[],viewCount:Number(data.viewCount||0)};
}
function isExpired(d){return d.expiresAt && new Date(d.expiresAt) < new Date(new Date().toISOString().slice(0,10));}
async function registerView(){
  const event={id:uid(),type:'viewed',title:'Orçamento visualizado',message:'O cliente abriu o link público.',createdAt:nowIso(),source:'public_page',userAgent:navigator.userAgent||''};
  currentDoc.viewCount=(currentDoc.viewCount||0)+1; currentDoc.lastAccessAt=event.createdAt; currentDoc.timeline=[...(currentDoc.timeline||[]),event];
  if(['emitido','enviado'].includes(currentDoc.document.status)) currentDoc.document.status='visualizado';
  if(docRef?.demo){const idx=demoIndex();idx[docRef.token]={...idx[docRef.token],...currentDoc};localStorage.setItem('orcafacil:publicQuotes',JSON.stringify(idx));return;}
  await updateDoc(docRef,{viewCount:increment(1),lastAccessAt:event.createdAt,'document.status':currentDoc.document.status,timeline:arrayUnion(event),updatedAt:event.createdAt});
}
async function loadPublic(){
  if(!token) return unavailable('Token público não informado.');
  const demo = demoIndex()[token];
  if(demo){currentDoc=normalize(demo); if(!currentDoc.publicEnabled)return unavailable('Link desativado.'); if(isExpired(currentDoc))return unavailable('Link expirado.'); docRef={demo:true,token}; await registerView(); return render();}
  const snap=await getDoc(doc(db,'publicQuotes',token));
  if(!snap.exists()) return unavailable('Link inválido.');
  currentDoc=normalize(snap.data()); docRef=doc(db,'publicQuotes',token);
  if(currentDoc.publicEnabled!==true) return unavailable('Link desativado.');
  if(isExpired(currentDoc)) return unavailable('Link expirado.');
  await registerView(); render();
}
function decisionBadge(){const meta=getDocumentStatusMeta(currentDoc.document.status);return `<span class="badge text-bg-${meta.badge}">${escapeHtml(meta.label)}</span>`;}
function render(){
  const q=currentDoc, d=q.document, p=q.issuer||{}, c=q.client||{}, totals=calcDocument(d); const decided=q.decision?.status&&q.decision.status!=='pendente';
  $('#publicApp').innerHTML=`<div class="row justify-content-center"><div class="col-lg-10"><div class="card border-0 shadow-sm"><div class="card-body p-4 p-lg-5">
    <div class="d-flex flex-column flex-md-row justify-content-between gap-3 mb-4"><div>${p.logoBase64?`<img src="${escapeHtml(p.logoBase64)}" alt="Logo" class="public-logo mb-2">`:''}<p class="text-uppercase text-primary fw-bold small mb-1">Orçamento para aprovação</p><h1 class="h3 fw-bold mb-1">Orçamento Nº ${escapeHtml(d.number||'-')}</h1><p class="text-secondary mb-0">Emissão: ${formatDateBR(d.issueDate)}${d.validUntil?` • Validade: ${formatDateBR(d.validUntil)}`:''}</p></div><div class="text-md-end"><div>Status comercial</div>${decisionBadge()}<div class="small text-secondary mt-1">${q.viewCount||0} visualização(ões)</div></div></div>
    ${decided?`<div class="alert alert-${q.decision.status==='aprovado'?'success':'danger'}"><strong>${q.decision.status==='aprovado'?'Orçamento já aprovado':'Orçamento recusado'}</strong><br>${escapeHtml(q.decision.note||'')} ${q.decision.evidenceHash?`<br><small>Código de evidência: ${escapeHtml(q.decision.evidenceHash)}</small>`:''}</div>`:''}
    <div class="row g-3 mb-4"><div class="col-md-6"><div class="p-3 bg-light rounded h-100"><h2 class="h6 fw-bold">Prestador</h2><div>${escapeHtml(p.name||'Emitente')}</div><small class="text-secondary">${escapeHtml([p.documentNumber,p.phone,p.email,p.city].filter(Boolean).join(' • '))}</small></div></div><div class="col-md-6"><div class="p-3 bg-light rounded h-100"><h2 class="h6 fw-bold">Cliente</h2><div>${escapeHtml(c.name||'-')}</div><small class="text-secondary">${escapeHtml([c.document,c.phone,c.email,c.city].filter(Boolean).join(' • '))}</small></div></div></div>
    <div class="table-responsive"><table class="table align-middle"><thead><tr><th>Item</th><th class="text-end">Qtd.</th><th class="text-end">Valor</th><th class="text-end">Desc.</th><th class="text-end">Total</th></tr></thead><tbody>${(d.items||[]).map(i=>`<tr><td>${escapeHtml(i.description||'-')}</td><td class="text-end">${i.qty||0}</td><td class="text-end">${formatCurrency(i.unit)}</td><td class="text-end">${formatCurrency(i.discount)}</td><td class="text-end fw-semibold">${formatCurrency((Number(i.qty)||0)*(Number(i.unit)||0)-(Number(i.discount)||0))}</td></tr>`).join('')}</tbody></table></div>
    <div class="row justify-content-end"><div class="col-md-5"><div class="p-3 bg-light rounded"><div class="d-flex justify-content-between"><span>Subtotal</span><strong>${formatCurrency(totals.subtotal)}</strong></div><div class="d-flex justify-content-between"><span>Desconto</span><strong>${formatCurrency(totals.discount)}</strong></div><hr><div class="d-flex justify-content-between fs-4"><span>Total</span><strong class="text-primary">${formatCurrency(totals.total)}</strong></div></div></div></div>
    ${d.notes?`<div class="alert alert-light border mt-4"><strong>Observações/condições comerciais</strong><br>${escapeHtml(d.notes)}</div>`:''}
    <div id="decisionAlert" class="mt-3"></div><div class="d-grid d-md-flex gap-2 mt-4"><button class="btn btn-success btn-lg" id="btnApprove" ${decided?'disabled':''}><i class="bi bi-check2-circle"></i> Aprovar orçamento</button><button class="btn btn-outline-danger btn-lg" id="btnReject" ${decided?'disabled':''}><i class="bi bi-x-circle"></i> Recusar orçamento</button><button class="btn btn-outline-primary btn-lg" id="btnPdfPublic"><i class="bi bi-file-earmark-pdf"></i> Baixar PDF</button></div>
    <p class="small text-secondary mt-4 mb-0">Aceite eletrônico simples para controle comercial. Não substitui assinatura digital certificada ICP-Brasil.</p><p class="small text-secondary mb-0">Gerado com OrçaFácil.</p>
  </div></div></div></div>`;
  $('#btnApprove').onclick=()=>showDecisionModal('aprovado'); $('#btnReject').onclick=()=>showDecisionModal('recusado'); $('#btnPdfPublic').onclick=()=>generatePdf({...d,type:'orcamento',number:d.number,date:d.issueDate,validUntil:d.validUntil,clientName:c.name,clientDoc:c.document,clientContact:c.phone,clientCity:c.city,status:d.status,decision:q.decision,clientDecision:q.decision.status,clientDecisionAt:q.decision.decidedAt,clientDecisionNote:q.decision.note,evidenceHash:q.decision.evidenceHash},p);
}
function showDecisionModal(decision){
  let el=$('#decisionModal'); if(!el){document.body.insertAdjacentHTML('beforeend',`<div class="modal fade" id="decisionModal" tabindex="-1"><div class="modal-dialog modal-dialog-centered"><div class="modal-content"><div class="modal-header"><h5 class="modal-title" id="decisionTitle"></h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div><div class="modal-body"><div class="mb-2"><label class="form-label">Nome</label><input id="decisionName" class="form-control" maxlength="120"></div><div class="mb-2"><label class="form-label">CPF/CNPJ (opcional)</label><input id="decisionDocument" class="form-control" maxlength="32"></div><div class="mb-2"><label class="form-label">E-mail (opcional)</label><input id="decisionEmail" class="form-control" maxlength="160"></div><div class="mb-2"><label class="form-label" id="decisionNoteLabel">Observação</label><textarea id="decisionNote" class="form-control" rows="3" maxlength="1000"></textarea></div><div class="form-check" id="termsBox"><input class="form-check-input" type="checkbox" id="decisionTerms"><label class="form-check-label" for="decisionTerms">Declaro que li e aprovo este orçamento nas condições apresentadas.</label></div></div><div class="modal-footer"><button class="btn btn-outline-secondary" data-bs-dismiss="modal">Cancelar</button><button class="btn btn-primary" id="btnConfirmDecision">Confirmar</button></div></div></div></div>`);el=$('#decisionModal');}
  $('#decisionTitle').textContent=decision==='aprovado'?'Aprovar orçamento':'Recusar orçamento'; $('#decisionNoteLabel').textContent=decision==='aprovado'?'Observação (opcional)':'Motivo/observação'; $('#termsBox').classList.toggle('d-none',decision!=='aprovado'); $('#decisionTerms').checked=false; $('#decisionName').value=''; $('#decisionDocument').value=''; $('#decisionEmail').value=''; $('#decisionNote').value=''; $('#btnConfirmDecision').onclick=()=>decide(decision); bootstrap.Modal.getOrCreateInstance(el).show();
}
async function decide(status){
  const name=sanitize($('#decisionName').value,120), note=sanitize($('#decisionNote').value,1000); if(!name) return alert('Informe o nome.'); if(status==='recusado'&&!note) return alert('Informe o motivo da recusa.'); if(status==='aprovado'&&!$('#decisionTerms').checked) return alert('Marque a declaração de aceite.');
  const decidedAt=nowIso(); const userAgent=navigator.userAgent||''; const evidenceHash=await sha256([token,currentDoc.documentId,currentDoc.document.number,currentDoc.document.total,decidedAt,name,userAgent,status].join('|'));
  const decision={status,note,decidedAt,decidedByName:name,decidedByDocument:sanitize($('#decisionDocument').value,32),decidedByEmail:sanitize($('#decisionEmail').value,160),acceptedTerms:status==='aprovado',ipInfo:null,userAgent,evidenceHash};
  const event={id:uid(),type:status==='aprovado'?'approved':'rejected',title:status==='aprovado'?'Orçamento aprovado pelo cliente':'Orçamento recusado pelo cliente',message:status==='aprovado'?`Aprovado por ${name}.`:`Recusado por ${name}.`,createdAt:decidedAt,source:'public_page',metadata:{evidenceHash}};
  currentDoc.decision=decision; currentDoc.document.status=status; currentDoc.timeline=[...(currentDoc.timeline||[]),event];
  if(docRef.demo){const idx=demoIndex();idx[docRef.token]={...idx[docRef.token],...currentDoc,clientDecision:status,clientDecisionAt:decidedAt,clientDecisionNote:note,evidenceHash,status};localStorage.setItem('orcafacil:publicQuotes',JSON.stringify(idx));}
  else await updateDoc(docRef,{decision,'document.status':status,status,clientDecision:status,clientDecisionAt:decidedAt,clientDecisionNote:note,evidenceHash,timeline:arrayUnion(event),updatedAt:decidedAt});
  bootstrap.Modal.getInstance($('#decisionModal'))?.hide(); render(); $('#decisionAlert').innerHTML=`<div class="alert alert-success">${status==='aprovado'?'Aprovação registrada com sucesso.':'Recusa registrada com sucesso.'}</div>`;
}
loadPublic().catch(err=>{console.error(err);unavailable(handleError(err,'Não foi possível carregar este orçamento.'));});
