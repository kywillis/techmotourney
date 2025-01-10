import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';

@Component({
  selector: 'app-message',
  templateUrl: './message.component.html',
  styleUrl: './message.component.less'
})
export class MessageComponent {
  private _message: string = '';
  private _error: boolean = false;

  get message(): string {
    return this._message;
  }

  get error(): boolean {
    return this._error;
  }

  ngOnChanges(changes: SimpleChanges): void {
  }

  setMessage(message:string, error: boolean = false){
    this._message = message;
    this._error = error;
    setTimeout(() => {
      this._message = '';
    }, 7000);
  }
}
