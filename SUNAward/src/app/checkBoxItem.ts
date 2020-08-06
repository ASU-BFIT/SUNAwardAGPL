export class CheckboxItem {
  value: string;
  label: string;
  checked: boolean;
  active: boolean;
  disabled: boolean;
  constructor(value: any, label: any, checked: boolean = false, active: boolean = true, disabled: boolean = false) {
    this.value = value;
    this.label = label;
    this.checked = checked;
    this.active = active;
    this.disabled = disabled;
  }
}
