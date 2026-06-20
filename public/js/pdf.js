import { money, calcDocument, valorPorExtenso } from './utils.js';

function formatDate(date){if(!date)return'';const [y,m,d]=String(date).slice(0,10).split('-');return `${d}/${m}/${y}`;}
function docTitle(type){return type==='recibo'?'RECIBO':'ORÇAMENTO';}
function pad(num){return String(num||0).padStart(4,'0');}

export function generatePdf(docData, profile={}){
  const { jsPDF } = window.jspdf;
  const pdf = new jsPDF({unit:'pt',format:'a4'});
  const pageW = pdf.internal.pageSize.getWidth();
  const margin = 42;
  const totals = calcDocument(docData);
  const primary=[30,58,95], blue=[45,125,210], green=[31,157,107];
  pdf.setFillColor(...primary);pdf.rect(0,0,pageW,92,'F');
  if(profile.logo){try{pdf.addImage(profile.logo,'PNG',margin,18,54,54);}catch{}}
  pdf.setTextColor(255);pdf.setFont('helvetica','bold');pdf.setFontSize(20);pdf.text(profile.name||'Emitente não informado',profile.logo?110:margin,38);
  pdf.setFont('helvetica','normal');pdf.setFontSize(9);
  const issuerLine=[profile.document,profile.phone,profile.email,profile.city].filter(Boolean).join(' • ');
  pdf.text(issuerLine||'Complete os dados do emitente',profile.logo?110:margin,56,{maxWidth:360});
  pdf.setFont('helvetica','bold');pdf.setFontSize(18);pdf.text(`${docTitle(docData.type)} Nº ${pad(docData.number)}`,pageW-margin,38,{align:'right'});
  pdf.setFont('helvetica','normal');pdf.setFontSize(10);pdf.text(`Data: ${formatDate(docData.date)}`,pageW-margin,58,{align:'right'});
  let y=122;
  pdf.setTextColor(28,36,48);pdf.setFont('helvetica','bold');pdf.setFontSize(12);pdf.text('Dados do cliente',margin,y);
  y+=12;pdf.setDrawColor(232,237,243);pdf.line(margin,y,pageW-margin,y);y+=22;
  pdf.setFont('helvetica','normal');pdf.setFontSize(10);
  pdf.text(`Cliente: ${docData.clientName||'-'}`,margin,y);pdf.text(`CPF/CNPJ: ${docData.clientDoc||'-'}`,310,y);y+=18;
  pdf.text(`Contato: ${docData.clientContact||'-'}`,margin,y);pdf.text(`Cidade/UF: ${docData.clientCity||'-'}`,310,y);y+=24;
  if(docData.type==='orcamento' && docData.validUntil){pdf.setTextColor(...blue);pdf.text(`Validade da proposta: ${formatDate(docData.validUntil)}`,margin,y);y+=22;pdf.setTextColor(28,36,48);}
  const body=(docData.items||[]).map((it,i)=>[i+1,it.description||'-',String(it.qty||0),money.format(it.unit||0),money.format(it.discount||0),money.format(((it.qty||0)*(it.unit||0))-(it.discount||0))]);
  pdf.autoTable({startY:y,head:[['#','Descrição','Qtd','Valor unit.','Desc.','Total']],body,theme:'grid',styles:{fontSize:9,cellPadding:6},headStyles:{fillColor:primary,textColor:255},columnStyles:{0:{cellWidth:28},2:{halign:'right'},3:{halign:'right'},4:{halign:'right'},5:{halign:'right'}}});
  y=pdf.lastAutoTable.finalY+22;
  pdf.setFont('helvetica','normal');pdf.setFontSize(10);pdf.text(`Subtotal: ${money.format(totals.subtotal)}`,pageW-margin,y,{align:'right'});y+=18;
  pdf.text(`Descontos: ${money.format(totals.discount)}`,pageW-margin,y,{align:'right'});y+=20;
  pdf.setFillColor(244,246,249);pdf.roundedRect(pageW-230,y-16,188,34,8,8,'F');pdf.setTextColor(...primary);pdf.setFont('helvetica','bold');pdf.setFontSize(14);pdf.text(`Total: ${money.format(totals.total)}`,pageW-margin,y+6,{align:'right'});pdf.setTextColor(28,36,48);y+=42;
  if(docData.type==='recibo'){
    pdf.setFont('helvetica','bold');pdf.setFontSize(11);pdf.text('Valor por extenso',margin,y);y+=16;
    pdf.setFont('helvetica','normal');pdf.text(valorPorExtenso(totals.total),margin,y,{maxWidth:pageW-margin*2});y+=36;
    pdf.text(`Declaro ter recebido de ${docData.clientName||'cliente'} a importância acima descrita.`,margin,y,{maxWidth:pageW-margin*2});y+=58;
    pdf.line(pageW/2-100,y,pageW/2+100,y);pdf.text(profile.name||'Assinatura do emitente',pageW/2,y+16,{align:'center'});y+=30;
  }
  if(docData.notes){pdf.setFont('helvetica','bold');pdf.setFontSize(11);pdf.text('Observações',margin,y);y+=16;pdf.setFont('helvetica','normal');pdf.text(docData.notes,margin,y,{maxWidth:pageW-margin*2});}
  const isFree=(profile.plan||'free')!=='pro';
  const pages=pdf.internal.getNumberOfPages();
  for(let i=1;i<=pages;i++){pdf.setPage(i);pdf.setFontSize(8);pdf.setTextColor(120);pdf.text(`Página ${i}/${pages}`,pageW-margin,820,{align:'right'});if(isFree){pdf.setTextColor(...green);pdf.setFont('helvetica','bold');pdf.text('Gerado com OrçaFácil — orçamentos e recibos em PDF',margin,820);}}
  pdf.save(`${docTitle(docData.type).toLowerCase()}-${pad(docData.number)}.pdf`);
}
