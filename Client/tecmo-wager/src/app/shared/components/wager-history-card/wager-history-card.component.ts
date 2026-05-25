import { Component, Input } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
import { WagerHistoryCardView } from '../../../core/utils/wager-audit-display.util';
import { wagerAuditActionLabel } from '../../../core/utils/wager-audit-display.util';

@Component({
  selector: 'app-wager-history-card',
  standalone: true,
  imports: [DatePipe, NgClass, RouterLink],
  templateUrl: './wager-history-card.component.html',
  styleUrl: './wager-history-card.component.less'
})
export class WagerHistoryCardComponent {
  @Input({ required: true }) view!: WagerHistoryCardView;

  actionLabel = wagerAuditActionLabel;

  formattedBalanceAfter(): string {
    const b = this.view.balanceAfter;
    return b ?? '';
  }

  hasBalanceAfter(): boolean {
    return this.view.balanceAfter != null && this.view.balanceAfter.length > 0;
  }

  isSettleOutcome(action: string): boolean {
    return (
      action === 'SettleWagerWin' ||
      action === 'SettleWagerLose' ||
      action === 'VoidWager' ||
      action === 'CancelWager' ||
      action === 'AdminCancelWager'
    );
  }

  outcomeBadgeClass(): Record<string, boolean> {
    const t = this.view.outcomeTone;
    return {
      'wager-outcome-badge--won': t === 'won',
      'wager-outcome-badge--lost': t === 'lost',
      'wager-outcome-badge--void': t === 'void',
      'wager-outcome-badge--cancelled': t === 'cancelled'
    };
  }
}
