import { ElementRef } from '@angular/core';
import { FormArray, FormGroup, NgForm } from '@angular/forms';
import { Patterns } from './constants/patterns';

export const getRouteFromUrl = (url: string): string | undefined =>
  url.split('?', 1).shift();

export function isPersonNameValid(input: string) {
  // I need to specify 'u' (unicode) for this pattern to work,
  // and I couldn't set this in the HTML input's pattern attribute
  var regexp = new RegExp(Patterns.personName, 'u');
  return regexp.test(input);
}

export function isCompanyNameValid(input: string) {
  var regexp = new RegExp(Patterns.companyName, 'u');
  return regexp.test(input);
}

export const markAllAsTouched = (form: NgForm) => {
  form.control.markAllAsTouched();

  return true;
};

export const focusInvalidInput = (form: ElementRef) => {
  const invalidControl = form.nativeElement.querySelector('.ng-invalid');

  if (invalidControl) {
    invalidControl.focus();

    return false;
  }

  return true;
};

export const updateTreeValidity = (group: FormGroup): boolean => {
  Object.keys(group.controls).forEach((key: string) => {
    const abstractControl = group.controls[key];
    abstractControl.setErrors(null);

    if (abstractControl instanceof FormGroup) {
      updateTreeValidity(abstractControl);
    } else {
      abstractControl.updateValueAndValidity();
    }
  });

  return true;
};

export const generateGuid = (): string => {
  var d = new Date().getTime(); //Timestamp
  var d2 = (performance && performance.now && performance.now() * 1000) || 0; //Time in microseconds since page-load or 0 if unsupported
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
    var r = Math.random() * 16; //random number between 0 and 16
    if (d > 0) {
      //Use timestamp until depleted
      r = (d + r) % 16 | 0;
      d = Math.floor(d / 16);
    } else {
      //Use microseconds since page-load if supported
      r = (d2 + r) % 16 | 0;
      d2 = Math.floor(d2 / 16);
    }
    return (c === 'x' ? r : (r & 0x3) | 0x8).toString(16);
  });
};
