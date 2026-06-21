export class AuditLogModel {
  constructor(data = {}) { Object.assign(this, { id:'', action:'', entityType:'', entityId:'', uid:'', userEmail:'', before:null, after:null, metadata:{}, createdAt:null }, data); }
}
