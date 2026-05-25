export interface PendingActivation {
  pendingActivationId: number;
  googleSubjectId: string;
  email: string;
  fullName: string;
  requestedProfilePic: number;
  status: string;
  requestedAt: string;
  activatedAt?: string | null;
  activatedByPlayerId?: number | null;
}

/** Non-deleted player with no Google id (admin link dropdown). */
export interface AdminPlayerLinkListItem {
  playerId: number;
  fullName: string;
  emailAddress: string;
}
