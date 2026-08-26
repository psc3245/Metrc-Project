#!/usr/bin/env bash
#
# Full-stack integration test: Auth -> Project -> Ticket -> Comment, exercising
# the whole chain in one run. Prints every major response body so you can
# visually verify shapes, and checks status codes at each step.
#
# Requires: curl, and jq (recommended) for pretty-printing/parsing.
#
# Usage:
#   chmod +x test-integration.sh
#   ./test-integration.sh
#
# Override the base URL if needed:
#   BASE_URL=http://localhost:5224 ./test-integration.sh

set -uo pipefail

BASE_URL="${BASE_URL:-http://localhost:5224}"
PASS=0
FAIL=0

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

json_get() {
  if command -v jq >/dev/null 2>&1; then
    echo "$1" | jq -r ".$2"
  else
    python3 -c "
import sys, json
data = json.loads(sys.argv[1])
path = sys.argv[2].split('.')
for p in path:
    data = data.get(p, '') if isinstance(data, dict) else ''
print(data)
" "$1" "$2" 2>/dev/null
  fi
}

pretty() {
  if command -v jq >/dev/null 2>&1; then
    echo "$1" | jq . 2>/dev/null || echo "$1"
  else
    echo "$1"
  fi
}

show() {
  echo -e "${CYAN}--- $1 ---${NC}"
  pretty "$2"
  echo ""
}

section() {
  echo ""
  echo -e "${BOLD}========== $1 ==========${NC}"
}

check() {
  local desc="$1" expected="$2" actual="$3"
  if [ "$expected" = "$actual" ]; then
    echo -e "${GREEN}PASS${NC} - $desc (status $actual)"
    PASS=$((PASS + 1))
  else
    echo -e "${RED}FAIL${NC} - $desc (expected $expected, got $actual)"
    FAIL=$((FAIL + 1))
  fi
}

check_value() {
  local desc="$1" expected="$2" actual="$3"
  if [ "$expected" = "$actual" ]; then
    echo -e "${GREEN}PASS${NC} - $desc"
    PASS=$((PASS + 1))
  else
    echo -e "${RED}FAIL${NC} - $desc (expected '$expected', got '$actual')"
    FAIL=$((FAIL + 1))
  fi
}

echo "Running full-stack integration tests against $BASE_URL"

# =========================================================
section "SETUP: Auth"
# =========================================================
OWNER_USERNAME="owner_$(date +%s)"
ASSIGNEE_USERNAME="assignee_$(date +%s)"
PASSWORD="Sup3rSecret!"

resp=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/Auth/signup" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$OWNER_USERNAME\",\"password\":\"$PASSWORD\"}")
body=$(echo "$resp" | sed '$d')
status=$(echo "$resp" | tail -n1)
check "Signup owner succeeds" "200" "$status"
show "Owner Signup Response" "$body"

TOKEN=$(json_get "$body" "token")
OWNER_ID=$(json_get "$body" "user.userId")
AUTH_HEADER="Authorization: Bearer $TOKEN"

resp=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/Auth/signup" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$ASSIGNEE_USERNAME\",\"password\":\"$PASSWORD\"}")
body=$(echo "$resp" | sed '$d')
status=$(echo "$resp" | tail -n1)
check "Signup assignee succeeds" "200" "$status"

ASSIGNEE_ID=$(json_get "$body" "user.userId")

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
  echo -e "${RED}Could not obtain auth token - aborting.${NC}"
  exit 1
fi
echo "Owner id: $OWNER_ID"
echo "Assignee id: $ASSIGNEE_ID"

# =========================================================
section "PROJECT FLOW"
# =========================================================
resp=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/Project" \
  -H "Content-Type: application/json" -H "$AUTH_HEADER" \
  -d '{"title":"Integration Test Project","description":"Full stack run","deadline":null}')
body=$(echo "$resp" | sed '$d')
status=$(echo "$resp" | tail -n1)
check "Create project succeeds" "201" "$status"
show "Create Project Response" "$body"

PROJECT_ID=$(json_get "$body" "projectId")

resp=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/Project/participants?projectId=$PROJECT_ID&userId=$ASSIGNEE_ID" \
  -H "$AUTH_HEADER")
body=$(echo "$resp" | sed '$d')
status=$(echo "$resp" | tail -n1)
check "Add assignee as project participant succeeds" "200" "$status"
show "Add Participant Response" "$body"

# =========================================================
section "TICKET FLOW"
# =========================================================
resp=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/Ticket" \
  -H "Content-Type: application/json" -H "$AUTH_HEADER" \
  -d "{\"title\":\"Fix login bug\",\"description\":\"Users can't log in on Safari\",\"deadline\":null,\"priority\":\"HIGH\",\"projectId\":\"$PROJECT_ID\"}")
body=$(echo "$resp" | sed '$d')
status=$(echo "$resp" | tail -n1)
check "Create ticket succeeds" "201" "$status"
show "Create Ticket Response" "$body"

TICKET_ID=$(json_get "$body" "ticketId")
ticket_author=$(json_get "$body" "authorId")
check_value "Ticket author matches authenticated owner (from JWT, not request body)" "$OWNER_ID" "$ticket_author"

resp=$(curl -s -w "\n%{http_code}" -X GET "$BASE_URL/api/Ticket?ticketId=$TICKET_ID" -H "$AUTH_HEADER")
status=$(echo "$resp" | tail -n1)
check "Get ticket by id succeeds" "200" "$status"

resp=$(curl -s -w "\n%{http_code}" -X PUT "$BASE_URL/api/Ticket/assign?ticketId=$TICKET_ID&assigneeId=$ASSIGNEE_ID" \
  -H "$AUTH_HEADER")
body=$(echo "$resp" | sed '$d')
status=$(echo "$resp" | tail -n1)
check "Assign ticket succeeds" "200" "$status"
show "Assign Ticket Response" "$body"

assignee_check=$(json_get "$body" "assigneeId")
check_value "Assignee id reflected in response" "$ASSIGNEE_ID" "$assignee_check"

resp=$(curl -s -w "\n%{http_code}" -X PUT "$BASE_URL/api/Ticket?ticketId=$TICKET_ID" \
  -H "Content-Type: application/json" -H "$AUTH_HEADER" \
  -d '{"title":null,"description":null,"deadline":null,"status":"IN_PROGRESS","priority":null}')
body=$(echo "$resp" | sed '$d')
status=$(echo "$resp" | tail -n1)
check "Update ticket status succeeds" "200" "$status"

status_check=$(json_get "$body" "status")
check_value "Ticket status reflects update" "IN_PROGRESS" "$status_check"

resp=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/Ticket/tags?ticketId=$TICKET_ID" \
  -H "Content-Type: application/json" -H "$AUTH_HEADER" \
  -d '{"name":"bug","color":"#FF0000"}')
status=$(echo "$resp" | tail -n1)
check "Add tag succeeds" "200" "$status"

resp=$(curl -s -w "\n%{http_code}" -X GET "$BASE_URL/api/Ticket/by-project?projectId=$PROJECT_ID" -H "$AUTH_HEADER")
status=$(echo "$resp" | tail -n1)
check "Get tickets by project succeeds" "200" "$status"

resp=$(curl -s -w "\n%{http_code}" -X DELETE "$BASE_URL/api/Ticket/tags?ticketId=$TICKET_ID&tagName=bug" -H "$AUTH_HEADER")
status=$(echo "$resp" | tail -n1)
check "Remove tag succeeds" "200" "$status"

resp=$(curl -s -w "\n%{http_code}" -X DELETE "$BASE_URL/api/Ticket/assign?ticketId=$TICKET_ID" -H "$AUTH_HEADER")
body=$(echo "$resp" | sed '$d')
status=$(echo "$resp" | tail -n1)
check "Unassign ticket succeeds" "200" "$status"

unassign_check=$(json_get "$body" "assigneeId")
check_value "Assignee id cleared after unassign" "null" "$unassign_check"

# =========================================================
section "COMMENT FLOW"
# =========================================================
resp=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/Comment" \
  -H "Content-Type: application/json" -H "$AUTH_HEADER" \
  -d "{\"text\":\"Looks good, ready for review.\",\"ticketId\":\"$TICKET_ID\"}")
body=$(echo "$resp" | sed '$d')
status=$(echo "$resp" | tail -n1)
check "Create comment succeeds" "201" "$status"
show "Create Comment Response" "$body"

COMMENT_ID=$(json_get "$body" "commentId")
comment_commenter=$(json_get "$body" "commenterId")
check_value "Comment commenter matches authenticated owner (from JWT, not request body)" "$OWNER_ID" "$comment_commenter"

resp=$(curl -s -w "\n%{http_code}" -X GET "$BASE_URL/api/Comment/by-ticket?ticketId=$TICKET_ID" -H "$AUTH_HEADER")
body=$(echo "$resp" | sed '$d')
status=$(echo "$resp" | tail -n1)
check "Get comments by ticket succeeds" "200" "$status"
show "Comments By Ticket Response" "$body"

resp=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/Comment" \
  -H "Content-Type: application/json" -H "$AUTH_HEADER" \
  -d "{\"text\":\"\",\"ticketId\":\"$TICKET_ID\"}")
status=$(echo "$resp" | tail -n1)
check "Create comment with empty text returns 400" "400" "$status"

# Assignee is a project participant but did not author the comment
ASSIGNEE_TOKEN_RESP=$(curl -s -X POST "$BASE_URL/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$ASSIGNEE_USERNAME\",\"password\":\"$PASSWORD\"}")
ASSIGNEE_TOKEN=$(json_get "$ASSIGNEE_TOKEN_RESP" "token")
ASSIGNEE_AUTH_HEADER="Authorization: Bearer $ASSIGNEE_TOKEN"

resp=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE "$BASE_URL/api/Comment?commentId=$COMMENT_ID" \
  -H "$ASSIGNEE_AUTH_HEADER")
check "Deleting someone else's comment returns 403" "403" "$resp"

# A user who was never added to the project can't comment at all
resp=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/Auth/signup" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"outsider_$(date +%s)\",\"password\":\"$PASSWORD\"}")
body=$(echo "$resp" | sed '$d')
OUTSIDER_TOKEN=$(json_get "$body" "token")
OUTSIDER_AUTH_HEADER="Authorization: Bearer $OUTSIDER_TOKEN"

resp=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/Comment" \
  -H "Content-Type: application/json" -H "$OUTSIDER_AUTH_HEADER" \
  -d "{\"text\":\"Sneaking in\",\"ticketId\":\"$TICKET_ID\"}")
check "Non-participant creating a comment returns 403" "403" "$resp"

resp=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE "$BASE_URL/api/Comment?commentId=$COMMENT_ID" \
  -H "$AUTH_HEADER")
check "Deleting own comment succeeds" "204" "$resp"

resp=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE "$BASE_URL/api/Comment?commentId=$COMMENT_ID" \
  -H "$AUTH_HEADER")
check "Deleting already-deleted comment returns 404" "404" "$resp"

# =========================================================
section "NEGATIVE CASES"
# =========================================================
resp=$(curl -s -w "\n%{http_code}" -X GET "$BASE_URL/api/Ticket/all")
status=$(echo "$resp" | tail -n1)
check "Get all tickets with no token returns 401" "401" "$status"

resp=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/Ticket" \
  -H "Content-Type: application/json" -H "$AUTH_HEADER" \
  -d '{"title":"Orphan Ticket","description":null,"deadline":null,"priority":"LOW","projectId":"00000000-0000-0000-0000-000000000000"}')
status=$(echo "$resp" | tail -n1)
check "Create ticket in nonexistent project returns 404" "404" "$status"

resp=$(curl -s -w "\n%{http_code}" -X PUT "$BASE_URL/api/Ticket/assign?ticketId=$TICKET_ID&assigneeId=00000000-0000-0000-0000-000000000000" \
  -H "$AUTH_HEADER")
status=$(echo "$resp" | tail -n1)
check "Assign ticket to nonexistent user returns 404" "404" "$status"

resp=$(curl -s -w "\n%{http_code}" -X GET "$BASE_URL/api/Ticket?ticketId=00000000-0000-0000-0000-000000000000" \
  -H "$AUTH_HEADER")
status=$(echo "$resp" | tail -n1)
check "Get nonexistent ticket returns 404" "404" "$status"

# =========================================================
section "CLEANUP (verifies cascade delete: project -> tickets -> comments)"
# =========================================================
# Leave one more comment so we can prove it disappears when the project cascades
resp=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/Comment" \
  -H "Content-Type: application/json" -H "$AUTH_HEADER" \
  -d "{\"text\":\"About to be cascaded away\",\"ticketId\":\"$TICKET_ID\"}")
status=$(echo "$resp" | tail -n1)
check "Create comment for cascade check succeeds" "201" "$status"

resp=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE "$BASE_URL/api/Project?projectId=$PROJECT_ID" -H "$AUTH_HEADER")
check "Delete project succeeds" "204" "$resp"

resp=$(curl -s -w "\n%{http_code}" -X GET "$BASE_URL/api/Ticket?ticketId=$TICKET_ID" -H "$AUTH_HEADER")
status=$(echo "$resp" | tail -n1)
check "Ticket is gone after project cascade delete" "404" "$status"

resp=$(curl -s -w "\n%{http_code}" -X GET "$BASE_URL/api/Comment/by-ticket?ticketId=$TICKET_ID" -H "$AUTH_HEADER")
status=$(echo "$resp" | tail -n1)
check "Comments query 404s too, since their ticket cascaded away" "404" "$status"

# =========================================================
section "RESULTS"
# =========================================================
echo -e "Results: ${GREEN}$PASS passed${NC}, ${RED}$FAIL failed${NC}"

if [ "$FAIL" -gt 0 ]; then
  exit 1
fi
exit 0
