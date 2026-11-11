$body=@'
ASP.NET Core API, contract-first, persisting to SQL via EF Core.

**Scope**
- OpenAPI spec written first (`openapi/v1.yaml`), controllers scaffolded from it
- POST /v1/orders -> 201 + Location header; 400 on invalid body
- GET /v1/orders/{id} -> 200; 404 if missing
- GET /v1/orders -> filters (status, date range) and paging (page, pageSize), 200
- GET /v1/data-quality/reports -> 200 (empty list is fine for now)
- /health/live and /health/ready (ready checks the database)
- EF Core migration for the orders table
- Dockerfile (multi-stage) and docker-compose with the database

**Out of scope:** authentication (tracked separately)

**Done when:**
- [ ] Spec is committed and the API matches it
- [ ] Unit and integration tests pass for each endpoint, including 400 and 404 cases
- [ ] `docker compose up` starts the API and database, and /health/ready returns 200
'@
GH="/c/Program Files/GitHub CLI/gh.exe"
"$GH" issue create --title "Orders API skeleton" --label "task" --assignee "@me" --body "$body"