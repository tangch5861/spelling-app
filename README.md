# Spelling App

This repository contains a .NET 8 minimal API and a React frontend to help kids practice spellings.

## Backend

The backend is located in the `SpellingApi` folder and exposes minimal endpoints for lessons:

- `POST /api/lessons/upload` – upload an image and extract words (placeholder for OpenAI OCR).
- `POST /api/lessons` – save or update a lesson.
- `GET /api/lessons/{id}` – retrieve a lesson with generated content.
- `POST /api/lessons/{id}/speech` – grade speech result.
- `POST /api/lessons/{id}/handwriting` – grade handwriting result.

The solution file `SpellingApp.sln` includes the API project.

## Frontend

The React app resides in `frontend/spelling-ui`. It contains a simple step-based UI and service worker for push notifications.

The application steps are:
1. upload ➜ 2. review ➜ 3. story ➜ 4. listen ➜ 5. speak ➜ 6. write ➜ 7. repeat.

API calls are implemented in `src/api.js`.

> **Note:** This repository contains only skeleton code and does not fetch dependencies or run build scripts in this environment.
