import { Component, OnInit, Input, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { CheckboxItem } from 'src/app/checkBoxItem';

@Component({
  selector: 'checkbox-list',
  templateUrl: './checkbox-list.component.html',
  styleUrls: ['./checkbox-list.component.css']
})
export class CheckboxListComponent implements OnInit {
  @Input() options = new Array<CheckboxItem>();
  @Output() toggle = new EventEmitter<any[]>();
  constructor() { }

  ngOnInit() {
  }

  onToggle() {
    const checkedOptions = this.options.filter(x => x.checked);
    this.toggle.emit(checkedOptions.map(x => x.value));
  }

}
