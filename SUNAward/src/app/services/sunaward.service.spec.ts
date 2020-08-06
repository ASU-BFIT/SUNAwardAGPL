import { TestBed } from '@angular/core/testing';

import { SunawardService } from './sunaward.service';

describe('SunawardService', () => {
  let service: SunawardService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SunawardService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
