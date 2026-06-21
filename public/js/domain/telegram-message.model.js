export class TelegramMessageModel {
  constructor(data = {}) { Object.assign(this, { id:'', type:'', title:'', message:'', severity:'info', payload:{}, status:'pending', createdAt:null, sentAt:null, error:'' }, data); }
}
