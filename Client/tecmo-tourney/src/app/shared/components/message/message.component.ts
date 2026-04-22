import { Component, OnChanges, SimpleChanges } from '@angular/core';
import { NotificationLogService } from 'src/app/core/services/notification-log.service';

@Component({
    selector: 'app-message',
    templateUrl: './message.component.html',
    styleUrl: './message.component.less',
    standalone: false
})
export class MessageComponent {
  private _message: string = '';
  private _error: boolean = false;

  constructor(private notificationLog: NotificationLogService) {}

  get message(): string {
    return this._message;
  }

  get error(): boolean {
    return this._error;
  }

  ngOnChanges(changes: SimpleChanges): void {
  }

  setMessage(message: string, error: boolean = false) {
    this._message = message;
    this._error = error;
    this.notificationLog.add({
      level: error ? 'error' : 'success',
      text: message
    });
    setTimeout(() => {
      this._message = '';
    }, 7000);
  }
}
