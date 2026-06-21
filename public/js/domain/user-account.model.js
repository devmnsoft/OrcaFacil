export class UserAccount {
  constructor({ uid = '', name = '', email = '', plan = 'free', createdAt = '', updatedAt = '' } = {}) {
    this.uid = uid;
    this.name = name;
    this.email = email;
    this.plan = plan;
    this.createdAt = createdAt;
    this.updatedAt = updatedAt;
  }
}
