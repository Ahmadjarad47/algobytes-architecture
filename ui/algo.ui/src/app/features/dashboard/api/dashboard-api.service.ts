import { inject, Injectable } from '@angular/core';
import { catchError, forkJoin, Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';

import { Permissions } from '../../../core/permissions/permission.catalog';
import { PermissionService } from '../../../core/permissions/permission.service';
import { AccessPoliciesApiService } from '../../access-policies/api/access-policies-api.service';
import { ApiService } from '../../../core/api/api.service';
import { ErrorLogsApiService } from '../../error-logs/api/error-logs-api.service';
import { LogsApiService } from '../../logs/api/logs-api.service';
import {
  AdminDashboardOverview,
  AdminDashboardOverviewQuery,
  UserDashboardStats
} from '../models/dashboard.models';

const EMPTY_USER_STATS: UserDashboardStats = {
  totalUsers: 0,
  activeUsers: 0,
  inactiveUsers: 0,
  lockedUsers: 0,
  emailConfirmedUsers: 0,
  emailNotConfirmedUsers: 0,
  phoneConfirmedUsers: 0,
  newUsersToday: 0,
  newUsersThisWeek: 0,
  newUsersThisMonth: 0,
  usersByRole: {},
  recentUsers: [],
  recentlyLockedUsers: []
};

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly api = inject(ApiService);
  private readonly logsApi = inject(LogsApiService);
  private readonly errorLogsApi = inject(ErrorLogsApiService);
  private readonly accessPoliciesApi = inject(AccessPoliciesApiService);
  private readonly permissions = inject(PermissionService);

  getStats(): Observable<UserDashboardStats> {
    return this.api.get<UserDashboardStats>('/Users/dashboard');
  }

  getOverview(query: AdminDashboardOverviewQuery = {}): Observable<AdminDashboardOverview> {
    const canReadUsers = this.permissions.can({ any: [Permissions.users.read] });
    const canReadLogs = this.permissions.can({ any: [Permissions.logs.read] });
    const canReadErrorLogs = this.permissions.can({ any: [Permissions.errorLogs.read] });
    const canReadPolicies = this.permissions.can({ any: [Permissions.accessPolicies.read] });

    return forkJoin({
      stats: (canReadUsers ? this.getStats() : of(EMPTY_USER_STATS)).pipe(catchError(() => of(EMPTY_USER_STATS))),
      logs: (canReadLogs ? this.logsApi
        .getLogs({
          PageNumber: 1,
          PageSize: 50,
          FromTimestamp: query.fromTimestamp,
          ToTimestamp: query.toTimestamp,
          SortField: 'timestamp',
          SortDirection: 'Descending'
        })
        .pipe(
          map((page) => page.items),
          catchError(() => of([]))
        ) : of([])),
      errorLogs: (canReadErrorLogs ? this.errorLogsApi
        .getErrorLogs({
          PageNumber: 1,
          PageSize: 25,
          FromTimestamp: query.fromTimestamp,
          ToTimestamp: query.toTimestamp,
          SortField: 'timestamp',
          SortDirection: 'Descending'
        })
        .pipe(
          map((page) => page.items),
          catchError(() => of([]))
        ) : of([])),
      accessPolicies: (canReadPolicies ? this.accessPoliciesApi.getPolicies() : of([])).pipe(catchError(() => of([])))
    });
  }
}
