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
    '[unicodePattern][formControlName],[unicodePattern][formControl],[unicodePattern][ngModel]',
  providers: [
    {
      provide: NG_VALIDATORS,
      useExisting: UnicodePatternValidator,
      multi: true,
    },
  ],
})
export class UnicodePatternValidator implements Validator {
  @Input() unicodePattern: string | RegExp;

  validate(control: AbstractControl): ValidationErrors | null {
    return CustomValidators.unicodePatternValidator(this.unicodePattern)(
      control
    );
  }
}
