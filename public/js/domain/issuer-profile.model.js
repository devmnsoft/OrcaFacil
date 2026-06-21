export class IssuerProfile {
  constructor({ businessName = '', documentNumber = '', phone = '', email = '', address = '', city = '', pix = '', logoBase64 = '', plan = 'free', updatedAt = '' } = {}) {
    this.businessName = businessName;
    this.documentNumber = documentNumber;
    this.phone = phone;
    this.email = email;
    this.address = address;
    this.city = city;
    this.pix = pix;
    this.logoBase64 = logoBase64;
    this.plan = plan;
    this.updatedAt = updatedAt;
  }
}
