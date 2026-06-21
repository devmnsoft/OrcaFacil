export class SystemErrorModel {
  constructor(data = {}) { Object.assign(this, { id:'', message:'', stack:'', code:'', severity:'error', uid:'', userEmail:'', context:{}, resolved:false, adminNote:'', createdAt:null }, data); }
}
