import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SubmitGameResultComponent } from './components/submit-game-result/submit-game-result.component';
import { SubmitGameResultRoutingModule } from './submit-game-result.-routing.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { FormsModule } from '@angular/forms'; 
import { MatTabsModule } from '@angular/material/tabs';


@NgModule({
  declarations: [
    SubmitGameResultComponent    
  ],
  imports: [
    CommonModule,
    SharedModule,
    FormsModule,
    MatTabsModule,
    SubmitGameResultRoutingModule
  ]
})
export class SubmitGameResultModule { }

