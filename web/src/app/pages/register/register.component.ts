import { NgFor, NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Card } from 'primeng/card';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Password } from 'primeng/password';
import { AuthService } from '../../core/auth/auth.service';
import { messagesFromError } from '../../core/auth/api-error';
import { passwordRules } from '../../core/auth/password-rules';
import { email } from '@angular/forms/signals';

// Both password fields must match. Checked on the group so the error can name the confirm field.
function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return password && confirm && password !== confirm ? { mismatch: true } : null;
}

@Component({
  selector: 'app-register',
  imports: [NgIf, NgFor, ReactiveFormsModule, RouterLink, Button, Card, InputText, Message, Password],
  templateUrl: './register.component.html',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly form = this.fb.nonNullable.group(
    {
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, passwordRules]],
      confirmPassword: ['', Validators.required]
    },
    { validators: passwordsMatch }
  );

  protected readonly  form2 = this.fb.nonNullable.group(
    {
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, passwordRules]],
      confirmPassword: ['', Validators.required]
    },
    { validators: passwordsMatch }
  );

  protected readonly submitting = signal(false);
  protected readonly errors = signal<string[]>([]);
  protected readonly registeredEmail = signal<string | null>(null);

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errors.set([]);

    const { email, password } = this.form.getRawValue(); // get all the form values even the disabled ones

    this.auth.register({ email, password }).subscribe({
      next: (response) => {
        this.submitting.set(false);
        this.registeredEmail.set(response.email);
        this.form.reset();
        this.router.navigateByUrl('/login');
      },
      error: (error) => {
        this.submitting.set(false);
        this.errors.set(messagesFromError(error, 'Registration failed. Please try again.'));
      }
    });
  }
}
