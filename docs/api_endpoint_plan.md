# RaceDay – API Endpoint Plan

This table lists every API endpoint the RaceDay system will expose, covering Authentication, User Profile, Events, Categories, Event Enrolments, and Results, as required for Part 1, Section B. This plan will be implemented as-is in Part 2.

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates a new user account as either an Organiser or a Participant. | None (public) | `{ fullName, email, password, phoneNumber, role }` | 201 Created – returns the new user's id and role. 400 Bad Request – validation failed. 409 Conflict – email already registered. |
| POST | /api/auth/login | Authenticates a user and returns a JWT access token. | None (public) | `{ email, password }` | 200 OK – returns JWT token, userId, and role. 401 Unauthorized – invalid credentials. |
| GET | /api/users/{id} | Retrieves a user's profile information. | Any (logged in) – only own profile, or Organiser | None | 200 OK – returns user profile. 404 Not Found – user does not exist. |
| PUT | /api/users/{id} | Updates a user's own profile details. | Any (logged in) – own profile only | `{ fullName, phoneNumber }` | 200 OK – returns updated profile. 403 Forbidden – attempting to edit another user's profile. |
| GET | /api/events | Lists all upcoming events, with optional filtering. | None (public) | None | 200 OK – returns array of events. |
| GET | /api/events/{id} | Retrieves full details of a single event, including its categories and routes. | None (public) | None | 200 OK – returns event details. 404 Not Found – event does not exist. |
| POST | /api/events | Creates a new event. | Organiser | `{ eventName, eventDate, location, description }` | 201 Created – returns the new event. 400 Bad Request – validation failed. |
| PUT | /api/events/{id} | Updates an existing event's details. | Organiser (owner) | `{ eventName, eventDate, location, description }` | 200 OK – returns updated event. 403 Forbidden – not the event's organiser. 404 Not Found. |
| DELETE | /api/events/{id} | Deletes an event and its related categories, routes, and enrolments. | Organiser (owner) | None | 200 OK – confirmation message. 403 Forbidden. 404 Not Found. |
| GET | /api/events/{id}/categories | Lists all categories for a specific event. | None (public) | None | 200 OK – returns array of categories. 404 Not Found – event does not exist. |
| POST | /api/events/{id}/categories | Adds a new category (e.g. distance/entry option) to an event. | Organiser (owner) | `{ categoryName, distanceKm, entryFee, maxParticipants }` | 201 Created – returns new category. 403 Forbidden. 404 Not Found – event does not exist. |
| PUT | /api/categories/{id} | Updates an existing category's details. | Organiser (owner of parent event) | `{ categoryName, distanceKm, entryFee, maxParticipants }` | 200 OK – returns updated category. 403 Forbidden. 404 Not Found. |
| DELETE | /api/categories/{id} | Removes a category from an event. | Organiser (owner of parent event) | None | 200 OK – confirmation message. 403 Forbidden. 404 Not Found. |
| GET | /api/events/{id}/routes | Lists route information (distance, elevation, map) for an event. | None (public) | None | 200 OK – returns array of routes. 404 Not Found – event does not exist. |
| POST | /api/events/{id}/enrolments | Enrols the logged-in participant into a category for an event. | Participant | `{ categoryId }` | 201 Created – returns enrolment with generated bib number. 404 Not Found – category does not exist. 409 Conflict – already enrolled in this category. |
| GET | /api/enrolments/my | Retrieves the logged-in participant's own enrolment history. | Participant | None | 200 OK – returns array of the participant's enrolments. |
| GET | /api/events/{id}/enrolments | Retrieves all participant enrolments for a specific event. | Organiser (owner) | None | 200 OK – returns array of enrolments. 403 Forbidden. 404 Not Found. |
| DELETE | /api/enrolments/{id} | Cancels/withdraws a participant's own enrolment. | Participant (owner) | None | 200 OK – confirmation message. 403 Forbidden. 404 Not Found. |
| POST | /api/results | Captures a result for a participant's enrolment. | Organiser | `{ enrolmentId, finishTime, position, status }` | 201 Created – returns the new result. 400 Bad Request. 404 Not Found – enrolment does not exist. |
| GET | /api/results/{enrolmentId} | Retrieves the result for a specific enrolment. | Any (logged in) – owner participant or Organiser | None | 200 OK – returns result. 404 Not Found. |
| GET | /api/users/{id}/results | Retrieves a participant's full personal performance history across all events. | Any (logged in) – own results only, or Organiser | None | 200 OK – returns array of results with event and category context. |

**Note:** Unexplained deviations between this plan and the implemented API in Part 2 will be avoided; any necessary changes will be documented in the README.
