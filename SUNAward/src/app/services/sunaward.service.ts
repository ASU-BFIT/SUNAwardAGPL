import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError } from 'rxjs/operators';
import { Observable, throwError } from 'rxjs';
import { SunAwardRecord } from '../data/sunAwardRecord';

@Injectable({
  providedIn: 'root'
})
export class SunawardService {

    submitUrl: string = 'api/SunAwards';
    previewUrl: string = 'Award/Preview'
    httpClient: HttpClient;

    constructor(private http: HttpClient) {
        this.httpClient = http;
    }

    addSUNAwardRecord(sunAwardRecord: SunAwardRecord) {
        return this.httpClient.post(this.submitUrl, sunAwardRecord).pipe(
            catchError(this.handleError)
        );
    }

    handleError(error) {
        return throwError(new Error(error));
    }
}
