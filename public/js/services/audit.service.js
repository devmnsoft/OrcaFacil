import { logger } from './logger.service.js';
export const AuditService = { record: (...args) => logger.audit(...args) };
