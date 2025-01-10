import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  private apiUrl: string = environment.apiUrl;

  getApiUrl(): string {
    return this.apiUrl;
  }
}
