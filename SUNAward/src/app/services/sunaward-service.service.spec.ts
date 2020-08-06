import { TestBed } from '@angular/core/testing';

import { SunawardServiceService } from './sunaward-service.service';

describe('SunawardServiceService', () => {
  let service: SunawardServiceService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SunawardServiceService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
