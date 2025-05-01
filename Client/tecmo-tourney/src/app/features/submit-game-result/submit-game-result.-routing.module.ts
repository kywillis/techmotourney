import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SubmitGameResultComponent } from './components/submit-game-result/submit-game-result.component';

const routes: Routes = [
  {
    path: '',
    component: SubmitGameResultComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class SubmitGameResultRoutingModule { }
