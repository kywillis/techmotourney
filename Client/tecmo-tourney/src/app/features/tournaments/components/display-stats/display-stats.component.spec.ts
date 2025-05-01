import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DisplayStatsComponent } from './display-stats.component';

describe('DisplayStatsComponent', () => {
  let component: DisplayStatsComponent;
  let fixture: ComponentFixture<DisplayStatsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DisplayStatsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DisplayStatsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
