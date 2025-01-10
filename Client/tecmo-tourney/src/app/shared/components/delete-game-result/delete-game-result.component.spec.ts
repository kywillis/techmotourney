import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DeleteGameResultComponent } from './delete-game-result.component';

describe('DeleteGameResultComponent', () => {
  let component: DeleteGameResultComponent;
  let fixture: ComponentFixture<DeleteGameResultComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DeleteGameResultComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DeleteGameResultComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
