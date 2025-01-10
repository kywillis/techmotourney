import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ViewGameResultComponent } from './view-game-result.component';

describe('ViewGameResultComponent', () => {
  let component: ViewGameResultComponent;
  let fixture: ComponentFixture<ViewGameResultComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ViewGameResultComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ViewGameResultComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
