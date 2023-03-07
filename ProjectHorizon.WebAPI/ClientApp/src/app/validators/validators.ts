import {
  AbstractControl,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';

export class CustomValidators {
  static isEmptyInputValue(value: any): boolean {
    /**
     * Check if the object is a string or array before evaluating the length attribute.
     * This avoids falsely rejecting objects that contain a custom length attribute.
     * For example, the object {id: 1, length: 0, width: 0} should not be returned as empty.
     */
    return (
      value == null ||
      ((typeof value === 'string' || Array.isArray(value)) &&
        value.length === 0)
    );
  }

  static unicodePatternValidator(unicodePattern: string | RegExp): ValidatorFn {
    if (!unicodePattern) {
      return Validators.nullValidator;
    }

    let regex: RegExp;
    let regexStr: string;

    if (typeof unicodePattern === 'string') {
      regexStr = '';

      if (unicodePattern.charAt(0) !== '^') regexStr += '^';

      regexStr += unicodePattern;

      if (unicodePattern.charAt(unicodePattern.length - 1) !== '$')
        regexStr += '$';

      regex = new RegExp(regexStr, 'u');
    } else {
      regexStr = unicodePattern.toString();
      regex = unicodePattern;
    }

    return (control: AbstractControl): ValidationErrors | null => {
      if (CustomValidators.isEmptyInputValue(control.value)) {
        return null; // don't validate empty values to allow optional controls
      }

      const value: string = control.value;
      return regex.test(value)
        ? null
        : { unicodePattern: { requiredPattern: regexStr, actualValue: value } };
    };
  }

  static maxValueValidator(max: number): ValidatorFn {
    if (max === undefined) {
      return Validators.nullValidator;
    }

    return (control: AbstractControl): ValidationErrors | null => {
      if (CustomValidators.isEmptyInputValue(control.value)) {
        return null; // don't validate empty values to allow optional controls
      }

      const value: number = control.value;
      return value <= max
        ? null
        : { maxValue: { max: max, actualValue: value } };
    };
  }

  static minValueValidator(min: number): ValidatorFn {
    if (min === undefined) {
      return Validators.nullValidator;
    }

    return (control: AbstractControl): ValidationErrors | null => {
      if (CustomValidators.isEmptyInputValue(control.value)) {
        return null; // don't validate empty values to allow optional controls
      }

      const value: number = control.value;
      return value >= min
        ? null
        : { minValue: { min: min, actualValue: value } };
    };
  }
}
