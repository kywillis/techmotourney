import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  private apiUrl: string = environment.apiUrl;
  private googleClientId: string = environment.googleClientId ?? '';

  getApiUrl(): string {
    return this.apiUrl;
  }

  /** Same Web client ID as tecmo-wager (Google Cloud Console). */
  getGoogleClientId(): string {
    return this.googleClientId;
  }
}
