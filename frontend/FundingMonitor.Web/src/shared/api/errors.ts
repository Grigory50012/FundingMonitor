type ApiErrorLike = {
  message?: unknown;
  response?: {
    data?: {
      details?: unknown;
      error?: unknown;
    };
  };
};

const isObject = (value: unknown): value is Record<string, unknown> =>
  typeof value === "object" && value !== null;

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!isObject(error)) return fallback;

  const candidate = error as ApiErrorLike;
  const details = candidate.response?.data?.details;
  if (typeof details === "string" && details.trim().length > 0) {
    return details;
  }

  const apiError = candidate.response?.data?.error;
  if (typeof apiError === "string" && apiError.trim().length > 0) {
    return apiError;
  }

  if (
    typeof candidate.message === "string" &&
    candidate.message.trim().length > 0
  ) {
    return candidate.message;
  }

  return fallback;
}
