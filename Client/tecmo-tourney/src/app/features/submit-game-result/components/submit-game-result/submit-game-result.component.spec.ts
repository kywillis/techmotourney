import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SubmitGameResultComponent } from './submit-game-result.component';

describe('SubmitGameResultComponent', () => {
  let component: SubmitGameResultComponent;
  let fixture: ComponentFixture<SubmitGameResultComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SubmitGameResultComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SubmitGameResultComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
