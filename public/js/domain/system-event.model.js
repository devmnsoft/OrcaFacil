export class SystemEventModel {
  constructor(data = {}) { Object.assign(this, { id:'', type:'', severity:'info', title:'', message:'', uid:'', userEmail:'', metadata:{}, source:'frontend', createdAt:null }, data); }
}
