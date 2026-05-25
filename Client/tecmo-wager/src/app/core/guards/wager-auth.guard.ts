import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { WagerAuthService } from '../services/wager-auth.service';

export function wagerAuthGuard(require: 'active' | 'pending'): CanActivateFn {
  return () => {
    const router = inject(Router);
    const auth = inject(WagerAuthService);
    const token = auth.getToken();
    const authState = auth.currentAuth();
    if (!token || !authState) {
      router.navigate(['/login']);
      return false;
    }
    if (require === 'pending') {
      if (authState.isPending) return true;
      router.navigate(['/']);
      return false;
    }
    if (require === 'active') {
      if (!authState.isPending && authState.isAuthenticated) return true;
      if (authState.isPending) {
        router.navigate(['/pending']);
        return false;
      }
    }
    router.navigate(['/login']);
    return false;
  };
}

/** Keep admins on operator flows only (no betting boards or player home). */
export const playerRoutesExcludeAdminGuard: CanActivateFn = () => {
  const router = inject(Router);
  const auth = inject(WagerAuthService);
  if (auth.currentAuth()?.isAdmin === true) {
    void router.navigate(['/admin']);
    return false;
  }
  return true;
};

/** Active, non-pending user with IsAdmin in the database. */
export const wagerAdminGuard: CanActivateFn = () => {
  const router = inject(Router);
  const auth = inject(WagerAuthService);
  const token = auth.getToken();
  const authState = auth.currentAuth();
  if (!token || !authState) {
    router.navigate(['/login']);
    return false;
  }
  if (authState.isPending) {
    router.navigate(['/pending']);
    return false;
  }
  if (!authState.isAuthenticated) {
    router.navigate(['/login']);
    return false;
  }
  if (!authState.isAdmin) {
    router.navigate(['/']);
    return false;
  }
  return true;
};

export const guestOnlyGuard: CanActivateFn = () => {
  const router = inject(Router);
  const auth = inject(WagerAuthService);
  const authState = auth.currentAuth();
  if (authState?.isPending) {
    router.navigate(['/pending']);
    return false;
  }
  if (authState?.isAuthenticated) {
    router.navigate(['/']);
    return false;
  }
  return true;
};
