import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DeletePlayerComponent } from './delete-player.component';

describe('DeletePlayerComponent', () => {
  let component: DeletePlayerComponent;
  let fixture: ComponentFixture<DeletePlayerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DeletePlayerComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DeletePlayerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
