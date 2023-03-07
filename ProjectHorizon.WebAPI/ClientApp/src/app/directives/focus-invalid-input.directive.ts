import { Directive, ElementRef, HostListener } from '@angular/core';

@Directive({
  selector: '[focusInvalidInput]',
})
export class FocusInvalidInputDirective {
  constructor(private element: ElementRef) {}

  @HostListener('submit')
  onFormSubmit() {
    const invalidControl =
      this.element.nativeElement.querySelector('.ng-invalid');

    if (invalidControl) {
      invalidControl.focus();
    }
  }
}
