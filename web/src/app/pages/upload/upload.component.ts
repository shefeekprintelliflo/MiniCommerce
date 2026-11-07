import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FilesService, UploadedFile, uploadErrorMessage } from '../../core/files/files.service';
import { HeaderComponent } from '../../shared/header/header.component';

// Must match the limits in Microservice.Functions/UploadFunction.cs. The server still checks both.
const MAX_BYTES = 10 * 1024 * 1024;
const ALLOWED_EXTENSIONS = ['.pdf', '.png', '.jpg', '.jpeg', '.csv'];

@Component({
  selector: 'app-upload',
  imports: [DecimalPipe, RouterLink, HeaderComponent],
  templateUrl: './upload.component.html',
  styleUrl: './upload.component.css',
})
export class UploadComponent {
  private readonly files = inject(FilesService);

  protected readonly accept = ALLOWED_EXTENSIONS.join(',');
  protected readonly allowedLabel = ALLOWED_EXTENSIONS.join(', ');
  protected readonly maxMegabytes = MAX_BYTES / (1024 * 1024);

  protected readonly selected = signal<File | null>(null);
  protected readonly uploading = signal(false);
  protected readonly uploaded = signal<UploadedFile | null>(null);
  protected readonly error = signal<string | null>(null);

  protected onFileChosen(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.uploaded.set(null);
    this.error.set(null);

    if (file) {
      const problem = this.validate(file);
      if (problem) {
        this.error.set(problem);
        this.selected.set(null);
        input.value = '';
        return;
      }
    }
    this.selected.set(file);
  }

  protected upload(): void {
    const file = this.selected();
    if (!file) {
      return;
    }

    this.uploading.set(true);
    this.error.set(null);

    this.files.upload(file).subscribe({
      next: (result) => {
        this.uploading.set(false);
        this.uploaded.set(result);
        this.selected.set(null);
      },
      error: (error: unknown) => {
        this.uploading.set(false);
        this.error.set(uploadErrorMessage(error));
      },
    });
  }

  private validate(file: File): string | null {
    const dot = file.name.lastIndexOf('.');
    const extension = dot >= 0 ? file.name.slice(dot).toLowerCase() : '';
    if (!ALLOWED_EXTENSIONS.includes(extension)) {
      return `"${extension}" files are not allowed. Choose one of: ${this.allowedLabel}.`;
    }
    if (file.size === 0) {
      return 'The file is empty.';
    }
    if (file.size > MAX_BYTES) {
      return `The file is larger than ${this.maxMegabytes} MB.`;
    }
    return null;
  }
}
