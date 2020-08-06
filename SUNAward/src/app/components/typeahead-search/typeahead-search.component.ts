import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Observable } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, catchError } from 'rxjs/operators';
import { PersonsService } from 'src/app/services/persons.service';
import { Person } from '../../data/person';
@Component({
    selector: 'app-typeahead-search',
    templateUrl: './typeahead-search.component.html',
    styleUrls: ['./typeahead-search.component.css']
})
export class TypeaheadSearchComponent implements OnInit {
    personService: PersonsService;
    @Input() inputId: string;
    @Input() data: Person;
    @Output() selectorChange = new EventEmitter();

    constructor(personService: PersonsService) {
        this.personService = personService;
    }
    title = 'Autocomplete search';
    public model: any;
    public selected: Person;
    public selectedValue: string;

    search = (text$: Observable<string>) =>
        text$.pipe(
            debounceTime(200),
            distinctUntilChanged(),
            switchMap((searchText) => {
                return searchText.length < 3 ? new Observable() : this.personService.getPersons(searchText)
            })
        );

    selectedItem(event: any) {
        this.selected = event.item;
        this.selectorChange.emit(this.selected);
    }

    blur(event: any) {
        this.selectedValue = this.formatter(this.selected);
        /*if (this.selected === null) {
            event.target.value = "";
        } else {
            event.target.value = this.formatter(this.selected);
        }*/
    }

    formatter(value: any) {
        return (value === null) ? "" : value.DisplayName;
    }

    getTitleDepartment(result: Person) {
        // returns "Title, Department" if both are defined or just the defined value (without punctuation)
        // if only one is defined
        return [result.Title, result.Department].filter(x => x).join(", ");
    }

    ngOnInit() {
        this.selected = this.data || null;
        this.selectedValue = this.formatter(this.selected);
    }

}
