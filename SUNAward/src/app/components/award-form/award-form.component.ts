import { Component, OnInit } from '@angular/core';
import { Category } from 'src/app/data/category';
import { CheckboxItem } from 'src/app/checkBoxItem';
import { Location } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Person } from 'src/app/data/person';
import { Alert } from 'src/app/data/alert';
import { SunAwardRecord } from 'src/app/data/sunAwardRecord';
import { NavigationExtras, Router } from '@angular/router';

@Component({
    selector: 'award-form',
    templateUrl: './award-form.component.html',
    styleUrls: ['./award-form.component.css']
})
export class AwardFormComponent implements OnInit {
    public categories: Category[];
    public selectedRecipient: Person;
    public selectedSupervisor: Person;
    public selectedCategories: number[];
    public checkBoxOptions = new Array<CheckboxItem>();
    public forText: string;
    public forDescPreview: string;
    public currentDate: string;
    public currentDatePreview: string;
    public sendToSupervisor: boolean;
    public showYouNameIt: boolean;
    public customAwardName: string;
    public alerts = new Array<Alert>();
    public presenterName: string;
    private router: Router;
    private location: Location;
    readonly youNameItId: number = 18;
    readonly youNameItString: string = "You name it!";

    constructor(http: HttpClient, router: Router, location: Location) {
        this.currentDate = new Date().toLocaleDateString();
        this.router = router;
        this.location = location;
        this.categories = (<any>window).awardCats;
        this.presenterName = (<any>window).presenterName;
    }

    onAwardRecipientChange(value) {
        this.selectedRecipient = value;
    }

    onSupervisorChange(value) {
        this.selectedSupervisor = value;
    }

    validateAndPreview() {
        var noErrors: boolean = true;
        if (!this.selectedCategories || this.selectedCategories.length == 0) {
            const categoryAlert: Alert = {
                type: "danger",
                message: "Select at least one Category"
            }

            noErrors = false;
            this.alerts.push(categoryAlert);
        }

        if (!this.selectedRecipient) {
            const recipientAlert: Alert = {
                type: "danger",
                message: "Select a Recipient"
            }

            noErrors = false;
            this.alerts.push(recipientAlert);
        }

        if (this.sendToSupervisor && !this.selectedSupervisor) {
            const sendToSupervisorAlert: Alert = {
                type: "danger",
                message: "Select a Supervisor or Uncheck the Supervisor checkbox."
            }

            noErrors = false;
            this.alerts.push(sendToSupervisorAlert);
        }

        if (noErrors) {
            // capture existing state and then navigate to preview
            // angular's state tracking is annoyingly broken so it's easier to bypass it
            // and just store the state as a property on window than use the actual APIs for it
            (<any>window).state = this.getState();
            this.router.navigate(["preview"]);
        }

        return noErrors;
    }

    onCategoriesChange(value) {
        this.selectedCategories = value;
        this.showYouNameIt = value.includes(this.youNameItId);
        if (!this.showYouNameIt) {
            this.customAwardName = "";
        }
    }

    getState() {
        var rec = new SunAwardRecord();
        rec.Categories = this.selectedCategories;
        rec.CustomCategory = this.customAwardName;
        rec.Recipient = this.selectedRecipient;
        rec.Supervisor = this.sendToSupervisor ? this.selectedSupervisor : null;
        rec.For = this.forText;

        return rec;
    }

    ngOnInit() {
        var state = <SunAwardRecord>(<any>window).state;
        if (!state || !state.Categories) {
            state = {
                For: "",
                Categories: [],
                CustomCategory: "",
                Recipient: null,
                Supervisor: null
            };
        }

        this.checkBoxOptions = this.categories.map(x => new CheckboxItem(x.id, x.name, x.id in state.Categories));
        this.forText = state.For;
        this.sendToSupervisor = (state.Supervisor !== null);
        this.showYouNameIt = (this.youNameItId in state.Categories);
        this.customAwardName = state.CustomCategory;
        this.selectedRecipient = state.Recipient;
        this.selectedSupervisor = state.Supervisor;
    }

}
