declare global {
  interface CredentialResponse {
    credential: string;
    select_by?: string;
    clientId?: string;
  }

  interface GoogleAccounts {
    id: {
      initialize: (config: { client_id: string; callback: (response: CredentialResponse) => void }) => void;
      renderButton: (element: HTMLElement, options: { theme?: string; size?: string; width?: number }) => void;
      prompt: () => void;
    };
  }

  interface Window {
    google?: { accounts: GoogleAccounts };
  }
}

export {};
