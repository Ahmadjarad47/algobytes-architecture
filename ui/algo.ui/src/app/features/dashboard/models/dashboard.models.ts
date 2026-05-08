import { AccessPolicyAdminDto } from '../../access-policies/models/access-policies.models';
import { ErrorLogDto } from '../../error-logs/models/error-logs.models';
import { ApplicationLogDto } from '../../logs/models/logs.models';

export interface DashboardRecentUser {
  readonly userId: string;
  readonly email: string | null;
  readonly displayName: string;
  readonly occurredAt: string;
  readonly activityKind: string;
}

export interface UserDashboardStats {
  readonly totalUsers: number;
  readonly activeUsers: number;
  readonly inactiveUsers: number;
  readonly lockedUsers: number;
  readonly emailConfirmedUsers: number;
  readonly emailNotConfirmedUsers: number;
  readonly phoneConfirmedUsers: number;
  readonly newUsersToday: number;
  readonly newUsersThisWeek: number;
  readonly newUsersThisMonth: number;
  readonly usersByRole: Record<string, number>;
  readonly recentUsers: readonly DashboardRecentUser[];
  readonly recentlyLockedUsers: readonly DashboardRecentUser[];
}

export interface AdminDashboardOverview {
  readonly stats: UserDashboardStats;
  readonly logs: ApplicationLogDto[];
  readonly errorLogs: ErrorLogDto[];
  readonly accessPolicies: AccessPolicyAdminDto[];
}

export interface AdminDashboardOverviewQuery {
  readonly fromTimestamp?: string;
  readonly toTimestamp?: string;
}
