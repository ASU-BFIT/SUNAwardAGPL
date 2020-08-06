import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError } from 'rxjs/operators';
import { Observable, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SunawardServiceService {
    baseUrl: string = 'api/sunAwards';
    httpClient: HttpClient;

    constructor(private http: HttpClient) {
        this.httpClient = http;
    }

    addSUNAwardRecord() {
        var objToPost = {
            
        }
        return this.httpClient.post(this.baseUrl, objToPost).pipe(
            catchError(this.handleError)
        );
        ;
    }

    handleError(error) {
        console.log("Submitted")
        return throwError("error");
    }

}
