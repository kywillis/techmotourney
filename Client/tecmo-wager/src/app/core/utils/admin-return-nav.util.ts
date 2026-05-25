import { ParamMap, Params } from '@angular/router';

export const ADMIN_RETURN_QUERY_RETURN_TO = 'returnTo';
export const ADMIN_RETURN_QUERY_RETURN_LABEL = 'returnLabel';

/** Parsed return navigation for admin child pages (balance, wagers list, etc.). */
export interface AdminReturnNav {
  label: string;
  /** Route path, e.g. `/admin/players/1019`. */
  path: string;
  queryParams: Params;
}

/** Build query params to pass when navigating from a page that should offer a return link. */
export function buildAdminReturnQuery(returnTo: string, returnLabel: string): Params {
  return {
    [ADMIN_RETURN_QUERY_RETURN_TO]: returnTo,
    [ADMIN_RETURN_QUERY_RETURN_LABEL]: returnLabel
  };
}

/** Merge return query into an existing queryParams object. */
export function withAdminReturnQuery(base: Params, returnTo: string, returnLabel: string): Params {
  return { ...base, ...buildAdminReturnQuery(returnTo, returnLabel) };
}

/**
 * Read returnTo + returnLabel from the route. Returns null if missing, invalid, or disallowed (open-redirect guard).
 */
export function parseAdminReturnNav(paramMap: ParamMap): AdminReturnNav | null {
  const returnTo = (paramMap.get(ADMIN_RETURN_QUERY_RETURN_TO) ?? '').trim();
  const label = (paramMap.get(ADMIN_RETURN_QUERY_RETURN_LABEL) ?? '').trim();
  if (!returnTo || !label) {
    return null;
  }
  const parsed = parseAllowedReturnTo(returnTo);
  if (!parsed) {
    return null;
  }
  return { label, path: parsed.path, queryParams: parsed.queryParams };
}

function parseAllowedReturnTo(returnTo: string): { path: string; queryParams: Params } | null {
  if (!returnTo.startsWith('/') || returnTo.startsWith('//')) {
    return null;
  }
  if (/^[a-z][a-z0-9+.-]*:/i.test(returnTo)) {
    return null;
  }

  const qIndex = returnTo.indexOf('?');
  const path = (qIndex >= 0 ? returnTo.slice(0, qIndex) : returnTo).trim();
  if (!isAllowedAdminReturnPath(path)) {
    return null;
  }

  const queryParams: Params = {};
  if (qIndex >= 0) {
    const search = returnTo.slice(qIndex + 1);
    if (search) {
      const params = new URLSearchParams(search);
      params.forEach((value, key) => {
        queryParams[key] = value;
      });
    }
  }

  return { path, queryParams };
}

function isAllowedAdminReturnPath(path: string): boolean {
  if (path === '/admin' || path.startsWith('/admin/')) {
    return true;
  }
  return false;
}
