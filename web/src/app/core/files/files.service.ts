import { HttpClient, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

export interface UploadedFile {
  name: string;
  size: number;
  url: string;
}

@Injectable({ providedIn: 'root' })
export class FilesService {
  private readonly http = inject(HttpClient);

  // Relative URL: the dev server proxies /api/files to the Azure Function (see proxy.conf.json).
  private readonly baseUrl = '/api/files';

  // The function reads the file from the form field named "file" and returns 201 with the blob URL in Location.
  upload(file: File): Observable<UploadedFile> {
    const form = new FormData();
    form.append('file', file, file.name);

    return this.http
      .post<{ name: string; size: number }>(this.baseUrl, form, { observe: 'response' })
      .pipe(
        map((response: HttpResponse<{ name: string; size: number }>) => ({
          name: response.body?.name ?? '',
          size: response.body?.size ?? 0,
          url: response.headers.get('Location') ?? '',
        })),
      );
  }
}

// The function returns plain text for validation errors and a JSON body for successes.
export function uploadErrorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 401 || error.status === 403) {
      return 'You are not allowed to upload files. Check the function key.';
    }
    if (error.status === 413) {
      return 'The file is larger than 10 MB.';
    }
    if (typeof error.error === 'string' && error.error.trim()) {
      return error.error;
    }
  }
  return 'The file could not be uploaded. Please try again.';
}
