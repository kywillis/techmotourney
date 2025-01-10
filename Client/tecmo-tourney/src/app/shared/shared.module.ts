import { NgModule, Optional, SkipSelf } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';

// Import your core services here
import { HeaderComponent } from '../shared/components/header/header.component';
import { FooterComponent } from '../shared/components/footer/footer.component';
import { NavComponent } from './components/nav/nav.component';
import { ModalComponent } from './components/modal/modal.component';
import { MessageComponent } from './components/message/message.component';
import { ViewGameResultComponent } from './components/view-game-result/view-game-result.component';
import { EditGameResultComponent } from './components/edit-game-result/edit-game-result.component';
import { DeleteGameResultComponent } from './components/delete-game-result/delete-game-result.component';
import { EnumFriendlyNamePipe } from './pipes/enum-friendly-name.pipe';

@NgModule({
  imports: [
    CommonModule,
    ReactiveFormsModule,
    HttpClientModule  // Import HttpClientModule here if your services need HTTP
  ],
  providers: [
  ],
  declarations: [
    HeaderComponent,
    FooterComponent,
    NavComponent,
    MessageComponent,
    ModalComponent,
    ViewGameResultComponent,
    EditGameResultComponent,
    DeleteGameResultComponent,
    EnumFriendlyNamePipe,
  ],
  exports: [
    HeaderComponent,
    FooterComponent,
    NavComponent,
    MessageComponent,
    ModalComponent,
    ViewGameResultComponent,
    EditGameResultComponent,
    DeleteGameResultComponent,
    EnumFriendlyNamePipe,
  ]
})
export class SharedModule {
  constructor(@Optional() @SkipSelf() parentModule: SharedModule) {
  }
}
