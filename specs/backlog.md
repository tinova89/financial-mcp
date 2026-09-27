# Backlog

Replaces `follow up tasks.md`. Each item becomes a feature folder (`specs/NNN-FT-name/`) when picked up.

## Open

| ID | Item | Origin | Status |
|---|---|---|---|
| 013 | Fuzzy/phonetic `lookup_category` + `hints` column | Card #13 | Spec ready → [013](specs/013-FT-fuzzy-category-lookup/spec.md) |
| FT-3 | Revert-to-`Scheduled` tool (`Confirmed → Scheduled`, keep `ConfirmedAt`) | Card #14 | Needs spec |
| — | Read isolation by `X-Account-Group` (global EF query filter) | Account group | Needs spec |
| — | Who earns the `approval` scope | Card #15 | Needs decision |
| — | `list_transactions` paging after in-memory filters (`TotalCount` ≠ items) | Card #20 | Needs spec |
| — | CSV parser: wrong delimiter / rows dropped after one failure | Card #20 | Needs spec |
| — | `import_statement` mapping to new statuses | Card #14 | Needs spec |

## Done

| ID | Item |
|---|---|
| 014 | Transaction statuses Revision/Scheduled/Confirmed → [014](features/014-transaction-statuses/spec.md) |
| 015 (FT-1) | `approve_revision` + `list_revisions` → [015](features/015-approve-revision/spec.md) |
| 016 (FT-2, 14.1) | `confirm_transaction` (rename + precondition) → [016](features/016-confirm-transaction/spec.md) |
| 019 | English domain terms → [019](features/019-english-domain-terms/spec.md) |
| 020 | Unit tests for untested tools → [020](features/020-unit-tests-untested-tools/spec.md) |
