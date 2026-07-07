import {
  AfterViewInit,
  Component,
  ElementRef,
  forwardRef,
  OnDestroy,
  ViewChild
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import flatpickr from 'flatpickr';
import type { Instance } from 'flatpickr/dist/types/instance';

@Component({
  selector: 'app-dob-picker',
  standalone: true,
  template: `
    <div class="dob-picker">
      <input #input type="text" placeholder="DD/MM/YYYY" readonly />
    </div>
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
    }

    .dob-picker {
      width: 100%;
    }

    :host ::ng-deep input.dob-picker-input,
    :host ::ng-deep .dob-picker input.dob-picker-input {
      display: block;
      width: 100%;
      box-sizing: border-box;
      padding: 0.625rem 0.875rem;
      border: 1px solid #e2e8f0;
      border-radius: 10px;
      font-size: 0.95rem;
      font-family: inherit;
      line-height: 1.5;
      color: #0f172a;
      background: #fff;
      cursor: pointer;
    }

    :host ::ng-deep input.dob-picker-input:focus {
      outline: 2px solid #99f6e4;
      border-color: #0f766e;
    }

    :host ::ng-deep input.dob-picker-input::placeholder {
      color: #94a3b8;
    }

    :host ::ng-deep input[data-input] {
      display: none !important;
    }
  `],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DobPickerComponent),
      multi: true
    }
  ]
})
export class DobPickerComponent implements ControlValueAccessor, AfterViewInit, OnDestroy {
  @ViewChild('input') inputRef!: ElementRef<HTMLInputElement>;

  private picker?: Instance;
  private pendingValue: string | null = null;
  private onChange: (value: string | null) => void = () => {};
  private onTouched: () => void = () => {};

  ngAfterViewInit() {
    const minDate = new Date();
    minDate.setFullYear(minDate.getFullYear() - 120);

    this.picker = flatpickr(this.inputRef.nativeElement, {
      dateFormat: 'Y-m-d',
      altInput: true,
      altFormat: 'd/m/Y',
      altInputClass: 'dob-picker-input',
      allowInput: false,
      disableMobile: true,
      maxDate: 'today',
      minDate,
      monthSelectorType: 'dropdown',
      shorthandCurrentMonth: false,
      onChange: (_dates, dateStr) => {
        this.onChange(dateStr || null);
        this.onTouched();
      },
      onClose: () => this.onTouched()
    });

    if (this.pendingValue) {
      this.picker.setDate(this.pendingValue, false);
      this.pendingValue = null;
    }
  }

  ngOnDestroy() {
    this.picker?.destroy();
  }

  writeValue(value: string | null): void {
    if (this.picker) {
      if (value) {
        this.picker.setDate(value, false);
      } else {
        this.picker.clear();
      }
      return;
    }
    this.pendingValue = value;
  }

  registerOnChange(fn: (value: string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    const visible = this.picker?.altInput ?? this.inputRef?.nativeElement;
    if (visible) {
      visible.disabled = isDisabled;
    }
  }
}
