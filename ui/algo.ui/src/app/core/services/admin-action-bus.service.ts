import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export type AdminGlobalAction =
  | 'create-user'
  | 'create-role'
  | 'create-access-policy'
  | 'create-product'
  | 'create-order'
  | 'open-wallet'
  | 'create-category'
  | 'create-api-key'
  | 'create-workspace';

@Injectable({ providedIn: 'root' })
export class AdminActionBusService {
  private readonly actionSubject = new Subject<AdminGlobalAction>();

  readonly actions$ = this.actionSubject.asObservable();

  dispatch(action: AdminGlobalAction): void {
    this.actionSubject.next(action);
  }
}
