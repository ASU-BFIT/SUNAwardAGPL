import { Component, OnInit } from '@angular/core';
import { Location } from '@angular/common';
import { SunawardService } from 'src/app/services/sunaward.service';
import { SunAwardSubmitResponse } from 'src/app/data/sunAwardSubmitResponse';
import { Router } from '@angular/router';
import { Alert } from 'src/app/data/alert';
import { SunAwardRecord } from '../../data/sunAwardRecord';
import { ReuseStrategy } from '../../reuse-strategy';

@Component({
  selector: 'app-preview-award',
  templateUrl: './preview-award.component.html',
  styleUrls: ['./preview-award.component.css']
})
export class PreviewAwardComponent implements OnInit {
    public location: Location;
    public postSubmitMessage: string;
    public recipientEmail: string;
    public supervisorEmail: string;
    public presenterEmail: string;
    public router: Router;
    public sunAwardService: SunawardService;
    public awardSubmitted: boolean;
    public submitLoading: boolean;
    public state: SunAwardRecord;
    public previewHref: string;
    public alerts = new Array<Alert>();

    sunAwardSubmitResponse: SunAwardSubmitResponse;

    constructor(sunAwardService: SunawardService, router: Router, location: Location) {
        this.sunAwardService = sunAwardService;
        this.router = router;
        this.awardSubmitted = false;
        this.submitLoading = false;
        this.location = location;
    }

    back() {
        this.location.back();
    }

    submit() {
        this.submitLoading = true;
        this.sunAwardService.addSUNAwardRecord(this.state).subscribe(
            (response: SunAwardSubmitResponse) => {
                if (response.Success) {
                    this.awardSubmitted = true;
                    this.alerts.push({
                        type: "success",
                        message: response.Message
                    });

                    // clear saved state
                    (<ReuseStrategy>this.router.routeReuseStrategy).clear();
                } else {
                    this.alerts.push({
                        type: "danger",
                        message: response.Message
                    });
                }

                this.submitLoading = false;
            });
    }

    ngOnInit(): void {
        this.state = <SunAwardRecord>(<any>window).state;
        var href: string[] = [];
        var i: number;
        for (i = 0; i < this.state.Categories.length; ++i) {
            href.push("Categories[" + i + "]=" + this.state.Categories[i]);
        }

        if (this.state.CustomCategory) {
            href.push("CustomCategory=" + encodeURIComponent(this.state.CustomCategory));
        }

        href.push("Recipient.AsuriteId=" + encodeURIComponent(this.state.Recipient.AsuriteId));
        href.push("For=" + encodeURIComponent(this.state.For));

        this.previewHref = "Award/Preview?" + href.join("&");
    }

}
