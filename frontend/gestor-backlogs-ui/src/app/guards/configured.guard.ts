import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { ApiService } from '../services/api.service';

export const configuredGuard: CanActivateFn = () => {
  const api = inject(ApiService);
  const router = inject(Router);

  return api.getConnectionSettings().pipe(
    map((settings) => settings.hasToken || router.createUrlTree(['/config'])),
    catchError(() => of(router.createUrlTree(['/config']))),
  );
};
