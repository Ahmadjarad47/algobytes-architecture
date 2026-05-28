import { environment } from '../../../environments/environment';

export type AdminEnvironment = 'Dev' | 'Staging' | 'Prod';
export type AdminDirection = 'ltr' | 'rtl';
export type AdminThemeMode = 'light' | 'dark';
export type AdminShapeMode = 'rounded' | 'sharp';

export interface AdminFeatureFlags {
  readonly users: boolean;
  readonly roles: boolean;
  readonly accessPolicies: boolean;
  readonly activeSessions: boolean;
  readonly logs: boolean;
  readonly errorLogs: boolean;
  readonly settings: boolean;
}

export interface AdminPasswordPolicy {
  readonly minLength: number;
  readonly requireUppercase: boolean;
  readonly requireNumber: boolean;
  readonly requireSymbol: boolean;
}

export interface AdminApiKeyConfig {
  readonly id: string;
  readonly name: string;
  readonly createdAt: string;
}

export interface AdminWebhookConfig {
  readonly id: string;
  readonly name: string;
  readonly url: string;
  readonly enabled: boolean;
}

export interface AdminAuthPageConfig {
  readonly brandLabel: string;
  readonly loginTitle: string;
  readonly loginSubtitle: string;
  readonly loginSubmitLabel: string;
  readonly registerPrompt: string;
  readonly registerLinkLabel: string;
  readonly registerTitle: string;
  readonly registerSubtitle: string;
  readonly registerSubmitLabel: string;
  readonly registerBackLinkLabel: string;
}

export interface AdminAuthPageDesignConfig {
  readonly backgroundStart: string;
  readonly backgroundEnd: string;
  readonly accentColor: string;
  readonly accentOpacity: number;
  readonly accentSizePercent: number;
  readonly cardBackground: string;
  readonly cardBorderColor: string;
  readonly cardRadiusPx: number;
  readonly cardShadow: string;
  readonly loginCardWidthRem: number;
  readonly registerCardWidthRem: number;
  readonly buttonBackground: string;
  readonly buttonTextColor: string;
}

export interface AdminTemplateConfig {
  readonly appName: string;
  readonly workspaceName: string;
  readonly environment: AdminEnvironment;
  readonly apiBaseUrl: string;
  readonly defaultLanguage: string;
  readonly timezone: string;
  readonly direction: AdminDirection;
  readonly theme: AdminThemeMode;
  readonly compactMode: boolean;
  readonly shape: AdminShapeMode;
  readonly sidebarCollapsed: boolean;
  readonly sidebarTitle: string;
  readonly primaryColor: string;
  readonly logoUrl: string | null;
  readonly faviconUrl: string | null;
  readonly sessionTimeoutMinutes: number;
  readonly passwordPolicy: AdminPasswordPolicy;
  readonly twoFactorEnabled: boolean;
  readonly allowedEmailDomains: readonly string[];
  readonly emailNotifications: boolean;
  readonly systemAlerts: boolean;
  readonly errorAlerts: boolean;
  readonly apiKeys: readonly AdminApiKeyConfig[];
  readonly webhooks: readonly AdminWebhookConfig[];
  readonly authPage: AdminAuthPageConfig;
  readonly authPageDesign: AdminAuthPageDesignConfig;
  readonly features: AdminFeatureFlags;
}

export const DEFAULT_ADMIN_TEMPLATE_CONFIG: AdminTemplateConfig = {
  appName: 'ALGO.UI',
  workspaceName: 'Workspace Admin Console',
  environment: environment.appEnvironment,
  apiBaseUrl: environment.apiBaseUrl,
  defaultLanguage: 'en',
  timezone: Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC',
  direction: 'ltr',
  theme: 'light',
  compactMode: false,
  shape: 'rounded',
  sidebarCollapsed: false,
  sidebarTitle: 'Admin Console',
  primaryColor: '#2563eb',
  logoUrl: null,
  faviconUrl: null,
  sessionTimeoutMinutes: 60,
  passwordPolicy: {
    minLength: 8,
    requireUppercase: true,
    requireNumber: true,
    requireSymbol: true
  },
  twoFactorEnabled: false,
  allowedEmailDomains: [],
  emailNotifications: true,
  systemAlerts: true,
  errorAlerts: true,
  authPage: {
    brandLabel: 'ALGO.UI',
    loginTitle: 'Please login',
    loginSubtitle: 'Sign in to manage users, roles, policies, and operational logs.',
    loginSubmitLabel: 'Sign in',
    registerPrompt: 'New here?',
    registerLinkLabel: 'Create an account',
    registerTitle: 'Create an account',
    registerSubtitle: 'Register a new operator account for the dashboard workspace.',
    registerSubmitLabel: 'Create account',
    registerBackLinkLabel: 'Back to sign in'
  },
  authPageDesign: {
    backgroundStart: '#f8fafc',
    backgroundEnd: '#eef2f7',
    accentColor: '#0ea5e9',
    accentOpacity: 0.16,
    accentSizePercent: 30,
    cardBackground: '#ffffff',
    cardBorderColor: '#ffffff',
    cardRadiusPx: 24,
    cardShadow: '0 24px 70px rgba(15, 23, 42, 0.16)',
    loginCardWidthRem: 28,
    registerCardWidthRem: 42,
    buttonBackground: '#6ee7b7',
    buttonTextColor: '#ffffff'
  },
  apiKeys: [
    {
      id: 'template-key',
      name: 'Template development key',
      createdAt: new Date().toISOString()
    }
  ],
  webhooks: [],
  features: {
    users: true,
    roles: true,
    accessPolicies: true,
    activeSessions: true,
    logs: true,
    errorLogs: true,
    settings: true
  }
};
