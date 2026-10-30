import { NgFor, NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Card } from 'primeng/card';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Password } from 'primeng/password';
import { AuthService } from '../../core/auth/auth.service';
import { messagesFromError } from '../../core/auth/api-error';

@Component({
  selector: 'app-login',
  imports: [NgIf, NgFor, ReactiveFormsModule, RouterLink, Button, Card, InputText, Message, Password],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  protected readonly submitting = signal(false);
  protected readonly errors = signal<string[]>([]);
  protected readonly signedIn = signal(false);

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errors.set([]);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.signedIn.set(true);
        this.router.navigateByUrl('/home');
      },
      error: (error) => {
        this.submitting.set(false);
        this.errors.set(messagesFromError(error, 'Sign in failed. Please try again.'));
      }
    });
  }
}
