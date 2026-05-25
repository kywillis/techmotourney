/**
 * Read JWT exp claim (seconds since epoch). No signature verification — UX only; API still validates.
 */
export function getJwtExpirySeconds(token: string): number | null {
  try {
    const parts = token.split('.');
    if (parts.length < 2) return null;
    let base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const pad = base64.length % 4;
    if (pad) base64 += '='.repeat(4 - pad);
    const payload = JSON.parse(atob(base64)) as { exp?: number };
    return typeof payload.exp === 'number' ? payload.exp : null;
  } catch {
    return null;
  }
}

/** True if token is missing or expired (with small clock skew). */
export function isJwtExpired(token: string, skewSeconds = 60): boolean {
  const exp = getJwtExpirySeconds(token);
  if (exp == null) return true;
  const nowSec = Math.floor(Date.now() / 1000);
  return exp <= nowSec + skewSeconds;
}
