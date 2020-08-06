import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Person } from '../data/person';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators'

@Injectable({
  providedIn: 'root'
})
export class PersonsService {
    baseUrl: string = "api/Persons";
  httpClient: HttpClient;
  constructor(private http: HttpClient) {
    this.httpClient = http;
  }
  getPersons(name): Observable<Person[]> {
    let url = this.baseUrl;
    return this.httpClient
      .get<Person[]>(url, {
        params: {
          name: name
        }
      });
  }
}
