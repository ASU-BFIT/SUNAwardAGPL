import { Component, OnInit, Input } from '@angular/core';
import { Alert } from 'src/app/data/alert';

@Component({
  selector: 'app-alert-closeable',
  templateUrl: './alert-closeable.component.html',
  styleUrls: ['./alert-closeable.component.css']
})
export class AlertCloseableComponent implements OnInit {
    @Input() alerts = new Array<Alert>();
  constructor() { }

    ngOnInit(): void {
    }

    close(alert: Alert) {
        this.alerts.splice(this.alerts.indexOf(alert), 1);
    }
}
