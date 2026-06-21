import { generatePdf } from '../pdf.js';
export class PdfService { generate(document, profile) { return generatePdf(document, profile); } }
