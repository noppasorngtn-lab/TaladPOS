# Contract: Membership

Traceability: FR-015–FR-019

## GET /members?phone={phone}

Look up a member by phone number during checkout (FR-017).

- **Auth**: any authenticated staff
- **Response 200**: `{ "id", "name", "phoneNumber", "accumulatedPurchaseTotal" }`
- **Response 404**: no member with that phone number (client offers "sign up" or "continue
  without member" per spec edge case)

## POST /members

Sign up a new member (FR-015).

- **Auth**: any authenticated staff
- **Request body**: `{ "name": string, "phoneNumber": string }`
- **Response 201**: created member, `accumulatedPurchaseTotal: 0`
- **Response 422**: `phoneNumber` already registered (FR-016)

## GET /members/{id}

- **Auth**: any authenticated staff
- **Response 200**: full member record
- **Response 404**: not found
