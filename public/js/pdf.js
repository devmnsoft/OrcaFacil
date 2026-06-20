import { money, calcDocument, valorPorExtenso } from './utils.js';

function formatDate(date){if(!date)return'';const [y,m,d]=String(date).slice(0,10).split('-');return `${d}/${m}/${y}`;}
function docTitle(type){return type==='recibo'?'RECIBO':'ORÇAMENTO';}
function pad(num){return String(num||0).padStart(4,'0');}
function ensureSpace(pdf,y,need=90){const h=pdf.internal.pageSize.getHeight(); if(y+need>h-58){pdf.addPage(); return 54;} return y;}

export function generatePdf(docData, profile={}){
  const { jsPDF } = window.jspdf;
  const pdf = new jsPDF({unit:'pt',format:'a4'});
  const pageW = pdf.internal.pageSize.getWidth();
  const pageH = pdf.internal.pageSize.getHeight();
  const margin = 42;
  const totals = calcDocument(docData);
  const primary=[30,58,95], blue=[45,125,210], green=[31,157,107], soft=[244,246,249];

  pdf.setFillColor(...primary);pdf.rect(0,0,pageW,98,'F');
  if(profile.logo){try{pdf.addImage(profile.logo,'PNG',margin,18,58,58);}catch{} }
  const left=profile.logo?116:margin;
  pdf.setTextColor(255);pdf.setFont('helvetica','bold');pdf.setFontSize(19);pdf.text(profile.name||'Emitente não informado',left,38,{maxWidth:270});
  pdf.setFont('helvetica','normal');pdf.setFontSize(9);
  pdf.text([profile.document,profile.phone,profile.email,profile.city].filter(Boolean).join(' • ')||'Complete os dados do emitente',left,58,{maxWidth:320});
  if(profile.pix) pdf.text(`Pix: ${profile.pix}`,left,74,{maxWidth:320});
  pdf.setFont('helvetica','bold');pdf.setFontSize(18);pdf.text(`${docTitle(docData.type)} Nº ${pad(docData.number)}`,pageW-margin,38,{align:'right'});
  pdf.setFont('helvetica','normal');pdf.setFontSize(10);pdf.text(`Data: ${formatDate(docData.date)}`,pageW-margin,58,{align:'right'});
  if(docData.type==='orcamento'&&docData.validUntil) pdf.text(`Validade: ${formatDate(docData.validUntil)}`,pageW-margin,74,{align:'right'});

  let y=124;
  pdf.setTextColor(28,36,48);pdf.setFont('helvetica','bold');pdf.setFontSize(12);pdf.text('Dados do cliente',margin,y);
  y+=12;pdf.setFillColor(...soft);pdf.roundedRect(margin,y,pageW-margin*2,58,8,8,'F');y+=20;
  pdf.setFont('helvetica','normal');pdf.setFontSize(10);
  pdf.text(`Cliente: ${docData.clientName||'-'}`,margin+14,y);pdf.text(`CPF/CNPJ: ${docData.clientDoc||'-'}`,310,y);y+=18;
  pdf.text(`Contato: ${docData.clientContact||'-'}`,margin+14,y);pdf.text(`Cidade/UF: ${docData.clientCity||'-'}`,310,y);y+=42;

  const body=(docData.items||[]).map((it,i)=>[i+1,it.description||'-',String(it.qty||0),money.format(it.unit||0),money.format(it.discount||0),money.format(((it.qty||0)*(it.unit||0))-(it.discount||0))]);
  pdf.autoTable({startY:y,head:[['#','Descrição','Qtd','Valor unit.','Desc.','Total']],body,theme:'striped',margin:{left:margin,right:margin,bottom:70},styles:{fontSize:9,cellPadding:7,lineColor:[232,237,243],lineWidth:.4,overflow:'linebreak'},headStyles:{fillColor:primary,textColor:255,fontStyle:'bold'},alternateRowStyles:{fillColor:[248,250,252]},columnStyles:{0:{cellWidth:28},1:{cellWidth:220},2:{halign:'right'},3:{halign:'right'},4:{halign:'right'},5:{halign:'right',fontStyle:'bold'}}});
  y=pdf.lastAutoTable.finalY+22; y=ensureSpace(pdf,y,150);
  pdf.setFont('helvetica','normal');pdf.setFontSize(10);pdf.text(`Subtotal: ${money.format(totals.subtotal)}`,pageW-margin,y,{align:'right'});y+=18;
  pdf.text(`Descontos: ${money.format(totals.discount)}`,pageW-margin,y,{align:'right'});y+=22;
  pdf.setFillColor(...primary);pdf.roundedRect(pageW-238,y-18,196,40,8,8,'F');pdf.setTextColor(255);pdf.setFont('helvetica','bold');pdf.setFontSize(15);pdf.text(`Total: ${money.format(totals.total)}`,pageW-margin,y+7,{align:'right'});pdf.setTextColor(28,36,48);y+=52;

  if(docData.type==='recibo'){
    y=ensureSpace(pdf,y,135);pdf.setFillColor(232,248,241);pdf.roundedRect(margin,y,pageW-margin*2,66,8,8,'F');y+=22;
    pdf.setFont('helvetica','bold');pdf.setFontSize(11);pdf.text('Valor por extenso',margin+14,y);y+=17;
    pdf.setFont('helvetica','normal');pdf.text(valorPorExtenso(totals.total),margin+14,y,{maxWidth:pageW-margin*2-28});y+=42;
    pdf.text(`Declaro ter recebido de ${docData.clientName||'cliente'} a importância acima descrita.`,margin,y,{maxWidth:pageW-margin*2});y+=58;
    pdf.line(pageW/2-105,y,pageW/2+105,y);pdf.text(profile.name||'Assinatura do emitente',pageW/2,y+16,{align:'center'});y+=36;
  }
  if(docData.notes){y=ensureSpace(pdf,y,80);pdf.setFillColor(...soft);pdf.roundedRect(margin,y,pageW-margin*2,70,8,8,'F');y+=20;pdf.setFont('helvetica','bold');pdf.setFontSize(11);pdf.text('Observações',margin+14,y);y+=16;pdf.setFont('helvetica','normal');pdf.text(docData.notes,margin+14,y,{maxWidth:pageW-margin*2-28});}
  const isFree=(profile.plan||'free')!=='pro';
  const pages=pdf.internal.getNumberOfPages();
  for(let i=1;i<=pages;i++){pdf.setPage(i);pdf.setDrawColor(232,237,243);pdf.line(margin,pageH-46,pageW-margin,pageH-46);pdf.setFontSize(8);pdf.setTextColor(120);pdf.text(`Página ${i}/${pages}`,pageW-margin,pageH-28,{align:'right'});if(isFree){pdf.setTextColor(...green);pdf.setFont('helvetica','bold');pdf.text('Gerado com OrçaFácil — orçamentos e recibos profissionais em PDF',margin,pageH-28);}else{pdf.setTextColor(120);pdf.text(profile.name||'',margin,pageH-28);}}
  pdf.save(`${docTitle(docData.type).toLowerCase()}-${pad(docData.number)}.pdf`);
}
