import { AbstractControl, ValidationErrors } from '@angular/forms';

// Matches the rules the identity service enforces in PasswordPolicy.cs.
export function passwordRules(control: AbstractControl): ValidationErrors | null {
  const value: string = control.value ?? '';

  if (!value) {
    return null;
  }

  const errors: ValidationErrors = {};

  if (value.length < 8) {
    errors['tooShort'] = true;
  }
  if (!/\d/.test(value)) {
    errors['noDigit'] = true;
  }
  if (!/[a-z]/.test(value)) {
    errors['noLower'] = true;
  }
  if (!/[A-Z]/.test(value)) {
    errors['noUpper'] = true;
  }
  if (!/[^A-Za-z0-9]/.test(value)) {
    errors['noSymbol'] = true;
  }

  return Object.keys(errors).length > 0 ? errors : null;
}
