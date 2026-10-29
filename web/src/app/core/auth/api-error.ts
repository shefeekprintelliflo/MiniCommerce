import { HttpErrorResponse } from '@angular/common/http';

interface ProblemBody {
  title?: string;
  errors?: Record<string, string[]>;
}

// The identity API returns ValidationProblem (400, with an `errors` map) for register
// and ProblemDetails (401, with a `title`) for failed login. Flatten both into messages.
export function messagesFromError(error: unknown, fallback: string): string[] {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as ProblemBody | null;

    if (body?.errors) {
      const messages = Object.values(body.errors).flat();
      if (messages.length > 0) {
        return messages;
      }
    }

    if (body?.title) {
      return [body.title];
    }
  }

  return [fallback];
}
