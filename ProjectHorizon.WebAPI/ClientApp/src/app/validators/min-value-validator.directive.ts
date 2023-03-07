import { Directive, forwardRef, Input } from '@angular/core';
import {
  AbstractControl,
  NG_VALIDATORS,
  ValidationErrors,
  Validator,
} from '@angular/forms';
import { CustomValidators } from './validators';

@Directive({
  selector:
    '[minValue][formControlName],[minValue][formControl],[minValue][ngModel]',
  providers: [
    {
      provide: NG_VALIDATORS,
      useExisting: MinValueValidator,
      multi: true,
    },
  ],
})
export class MinValueValidator implements Validator {
  @Input() minValue: number;

  validate(control: AbstractControl): ValidationErrors | null {
    return CustomValidators.minValueValidator(this.minValue)(control);
  }
}
