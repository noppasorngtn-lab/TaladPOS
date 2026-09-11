const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5260";

export class ApiError extends Error {
  status: number;
  code: string;

  constructor(status: number, code: string, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
  }
}

interface ApiFetchOptions extends RequestInit {
  /** JWT to attach as `Authorization: Bearer <token>` (contracts/README.md). */
  token?: string | null;
}

/**
 * Thin typed REST client wrapping every call to `api/` (research.md item 8).
 * Parses the `{ error: { code, message } }` shape produced by the API's
 * error-handling middleware and throws it as an ApiError.
 */
export async function apiFetch<T>(path: string, options: ApiFetchOptions = {}): Promise<T> {
  const { token, headers, ...rest } = options;

  // FormData bodies (multipart product uploads) must NOT get a Content-Type here —
  // the browser sets its own header including the multipart boundary.
  const isFormData = typeof FormData !== "undefined" && rest.body instanceof FormData;

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...rest,
    headers: {
      ...(isFormData ? {} : { "Content-Type": "application/json" }),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...headers,
    },
  });

  if (!response.ok) {
    let code = "unknown_error";
    let message = response.statusText;
    try {
      const body = (await response.json()) as { error?: { code?: string; message?: string } };
      code = body.error?.code ?? code;
      message = body.error?.message ?? message;
    } catch {
      // Response had no JSON body (e.g. network-level failure surfaced by a proxy).
    }
    throw new ApiError(response.status, code, message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

interface ApiFetchBlobOptions {
  /** JWT to attach as `Authorization: Bearer <token>` (contracts/README.md). */
  token?: string | null;
}

/**
 * Like {@link apiFetch}, but for the two export endpoints that return a binary `.xlsx` body
 * instead of JSON (research.md item 3, feature 002-export-reports-sales-history). A plain browser
 * navigation to the export URL can't attach the Authorization header the JWT lives behind, so
 * callers fetch the file as a Blob here and hand it to `downloadBlob` (lib/download.ts) instead.
 */
export async function apiFetchBlob(path: string, options: ApiFetchBlobOptions = {}): Promise<Blob> {
  const { token } = options;

  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (!response.ok) {
    // Export failures still return the normal JSON error shape (contracts/README.md), never a
    // malformed file — parse it the same way apiFetch does.
    let code = "unknown_error";
    let message = response.statusText;
    try {
      const body = (await response.json()) as { error?: { code?: string; message?: string } };
      code = body.error?.code ?? code;
      message = body.error?.message ?? message;
    } catch {
      // Response had no JSON body (e.g. network-level failure surfaced by a proxy).
    }
    throw new ApiError(response.status, code, message);
  }

  return response.blob();
}
