import { HttpErrorResponse } from '@angular/common/http';

/**
 * Extracts a human-readable message from Angular HttpClient errors and API bodies.
 * Handles TecmoTourney ErrorContent ({ errorMessage }), Problem Details, and network failures.
 */
export function getHttpErrorMessage(err: unknown, fallback = 'Request failed.'): string {
  if (err instanceof HttpErrorResponse) {
    const body = err.error;

    if (typeof body === 'string') {
      const t = body.trim();
      if (!t) {
        return formatHttpStatus(err);
      }
      if (t.startsWith('<')) {
        return formatHttpStatus(err);
      }
      return t.length > 500 ? `${t.slice(0, 500)}…` : t;
    }

    if (body && typeof body === 'object') {
      const o = body as Record<string, unknown>;
      const fromKeys =
        pickString(o, 'errorMessage') ??
        pickString(o, 'ErrorMessage') ??
        pickString(o, 'message') ??
        pickString(o, 'Message') ??
        pickString(o, 'title') ??
        pickString(o, 'Title') ??
        pickString(o, 'detail') ??
        pickString(o, 'Detail');
      if (fromKeys) {
        return fromKeys;
      }
      if (typeof o['errors'] === 'object' && o['errors'] !== null) {
        const nested = stringifyValidationErrors(o['errors'] as Record<string, unknown>);
        if (nested) {
          return nested;
        }
      }
    }

    return formatHttpStatus(err);
  }

  if (err instanceof Error && err.message) {
    return err.message;
  }

  return fallback;
}

function pickString(o: Record<string, unknown>, key: string): string | null {
  const v = o[key];
  return typeof v === 'string' && v.trim() ? v.trim() : null;
}

function stringifyValidationErrors(errors: Record<string, unknown>): string | null {
  const parts: string[] = [];
  for (const [k, v] of Object.entries(errors)) {
    if (Array.isArray(v)) {
      const msgs = v.filter((x) => typeof x === 'string') as string[];
      if (msgs.length) {
        parts.push(`${k}: ${msgs.join('; ')}`);
      }
    } else if (typeof v === 'string') {
      parts.push(`${k}: ${v}`);
    }
  }
  return parts.length ? parts.join(' ') : null;
}

function formatHttpStatus(err: HttpErrorResponse): string {
  if (err.status === 0) {
    return 'Network error (offline, CORS, or server unreachable).';
  }
  const code = err.status > 0 ? `${err.status}` : '';
  const text = (err.statusText && err.statusText !== 'Unknown Error' ? err.statusText : '') || '';
  const base = [code, text].filter(Boolean).join(' ').trim();
  return base ? `HTTP ${base}` : err.message || 'Request failed.';
}
