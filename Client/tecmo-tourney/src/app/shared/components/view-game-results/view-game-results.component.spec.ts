import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ViewGameResultsComponent } from './view-game-results.component';

describe('ViewGameResultsComponent', () => {
  let component: ViewGameResultsComponent;
  let fixture: ComponentFixture<ViewGameResultsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ViewGameResultsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ViewGameResultsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
