export interface WagerAuthResponse {
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
  /** Active player portrait index; omit or 0 = none */
  profilePic?: number | null;
}
