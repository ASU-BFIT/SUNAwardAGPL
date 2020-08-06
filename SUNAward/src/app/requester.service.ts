import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class RequesterService {

  baseUrl: string = "https://localhost:44336/api/"

  constructor(private http: HttpClient) { }

  get(apiString: string) {
    return this.http.get(this.baseUrl + apiString);
  }
}
