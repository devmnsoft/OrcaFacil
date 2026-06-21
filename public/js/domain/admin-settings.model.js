export class AdminSettingsModel {
  constructor(data = {}) { Object.assign(this, { telegramEnabled:false, telegramChatId:'', notifyOnUserRegister:true, notifyOnDocumentCreated:true, notifyOnPdfGenerated:false, notifyOnQuoteApproved:true, notifyOnCriticalError:true, notifyOnLogin:false, updatedAt:null, updatedBy:'' }, data); }
}
