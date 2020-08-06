import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PreviewAwardComponent } from './preview-award.component';

describe('PreviewAwardComponent', () => {
  let component: PreviewAwardComponent;
  let fixture: ComponentFixture<PreviewAwardComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PreviewAwardComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PreviewAwardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
