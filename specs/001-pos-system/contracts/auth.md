# Contract: Auth

Traceability: FR-007, FR-034

## POST /auth/login

Authenticate a staff member and issue a JWT.

- **Auth**: none
- **Request body**: `{ "username": string, "password": string }`
- **Response 200**: `{ "token": string, "expiresAt": datetime, "staff": { "id", "name", "role" } }`
- **Response 401**: invalid username/password

## POST /auth/logout

Invalidate the client-side session (stateless JWT — this endpoint exists for symmetry / future
token-revocation support; MVP behavior is client discards the token).

- **Auth**: any authenticated staff
- **Response 204**: no content

## GET /auth/me

Return the currently authenticated staff member's identity and role, used by `web/` to gate
admin-only screens.

- **Auth**: any authenticated staff
- **Response 200**: `{ "id", "name", "role" }`
