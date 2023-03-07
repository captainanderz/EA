import { Directive, forwardRef, Input } from '@angular/core';
import {
  AbstractControl,
  NG_VALIDATORS,
  ValidationErrors,
  Validator,
  Validators,
} from '@angular/forms';
import { CustomValidators } from './validators';

@Directive({
  selector:
    '[maxValue][formControlName],[maxValue][formControl],[maxValue][ngModel]',
  providers: [
    {
      provide: NG_VALIDATORS,
      useExisting: MaxValueValidator,
      multi: true,
    },
  ],
})
export class MaxValueValidator implements Validator {
  @Input() maxValue: number;

  validate(control: AbstractControl): ValidationErrors | null {
    return CustomValidators.maxValueValidator(this.maxValue)(control);
  }
}
