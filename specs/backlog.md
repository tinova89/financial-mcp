# Backlog

This file replaces `follow up tasks.md`. When we start work on an item, it gets its own feature folder (`specs/NNN-FT-name/`).

## Open

| ID | Item | Origin | Status |
|---|---|---|---|
| 013 | Fuzzy/phonetic `lookup_category` + `hints` column | Card #13 | Spec is ready → [013](013-FT-fuzzy-category-lookup/spec.md) |
| — | `update_category_hints` saves the phonetic form of each hint term too, so `lookup_category` does not compute it again on every search (existing hints need a backfill) | Feature 013 | Needs a spec (after 013) |
| — | There is no way to clear a category's hints: `update_category_hints` (today `update_category_instruction`) rejects empty text, so hints can only be overwritten, never removed | Feature 013 | Needs a spec |
| — | Fuller tool descriptions for `lookup_category` and `update_category_hints`: both modes and what each returns (null fields), limits (5 candidates / threshold vs. no limit), what the search matches (hint terms + category name, never history), ordering, when to call it and to suggest a new hint on no match; hint format (comma/semicolon terms) | Feature 013 (FR-012 kept minimal) | Needs a spec (after 013) |
| FT-3 | Tool to go back to `Scheduled` (`Confirmed → Scheduled`, keep `ConfirmedAt`) | Card #14 | Needs a spec |
| — | Separate the data people can read by `X-Account-Group` (global EF query filter) | Account group | Needs a spec |
| — | Who gets the `approval` scope | Card #15 | Needs a decision |
| — | `list_transactions` makes pages before some filters run in memory (`TotalCount` ≠ items) | Card #20 | Needs a spec |
| — | CSV parser: wrong delimiter / rows are lost after one row fails | Card #20 | Needs a spec |
| — | `import_statement` mapping to the new statuses | Card #14 | Needs a spec |

## Done

| ID | Item |
|---|---|
| 014 | Transaction statuses Revision/Scheduled/Confirmed (no spec folder) |
| 015 (FT-1) | `approve_revision` + `list_revisions` (no spec folder) |
| 016 (FT-2, 14.1) | `confirm_transaction` (new name + precondition) (no spec folder) |
| 019 | Domain terms in English (no spec folder) |
| 020 | Unit tests for tools that had no tests (no spec folder) |
