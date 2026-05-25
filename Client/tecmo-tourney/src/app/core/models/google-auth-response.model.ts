/** Response from POST /api/wager/auth/google (same contract as tecmo-wager). */
export interface GoogleAuthResponse {
  isAuthenticated: boolean;
  isPending: boolean;
  message?: string;
  playerId?: number;
  fullName?: string;
  isAdmin: boolean;
  balance: number;
  pendingActivationId?: number;
  email?: string;
  requestedProfilePic?: number;
  profilePic?: number | null;
}
