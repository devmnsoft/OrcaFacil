import { APP_NAME, APP_VERSION, PUBLIC_BASE_PATH } from './config.js';

export function describeRuntime() {
  return { appName: APP_NAME, appVersion: APP_VERSION, publicBasePath: PUBLIC_BASE_PATH };
}
