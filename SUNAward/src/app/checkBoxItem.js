"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var CheckboxItem = /** @class */ (function () {
    function CheckboxItem(value, label, checked, active, disabled) {
        if (checked === void 0) { checked = false; }
        if (active === void 0) { active = true; }
        if (disabled === void 0) { disabled = false; }
        this.value = value;
        this.label = label;
        this.checked = checked;
        this.active = active;
        this.disabled = disabled;
    }
    return CheckboxItem;
}());
exports.CheckboxItem = CheckboxItem;
//# sourceMappingURL=checkBoxItem.js.map