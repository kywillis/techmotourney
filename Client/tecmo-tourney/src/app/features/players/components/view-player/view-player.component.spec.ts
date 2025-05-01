import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ViewPlayerComponent } from './view-player.component';

describe('ViewPlayerComponent', () => {
  let component: ViewPlayerComponent;
  let fixture: ComponentFixture<ViewPlayerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ViewPlayerComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ViewPlayerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
