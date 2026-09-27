# Feature Specification: Phonetic/Fuzzy Category Lookup

**Feature Branch**: `013-FT-fuzzy-category-lookup`

**Created**: 2026-09-27

**Status**: Draft

**Input**: User description: "Phonetic/fuzzy search for lookup_category"

## Context

Today `lookup_category` takes no input: it returns every category that carries an
Instruction hint (free text such as "Rede economia, Extra hiper, Carrefour") and leaves the
assistant to compare a transaction's description against all of them by itself. Bank and card
statement descriptions rarely match those hints literally — they come truncated, upper-cased,
without accents, glued together, prefixed by payment processors and followed by store numbers
or cities (e.g. `PAG*REDEECONOMIA 0231`, `EXTRA HIPER 1234 SAO PAULO`, `CARREFUR COM IND`). As
the list of categories grows, the assistant either guesses the wrong category or asks the user
every time.

This feature lets the caller pass a transaction description to `lookup_category` and receive
the categories most likely to match it, ranked, tolerating misspellings, spacing differences,
accents and words that merely *sound* alike in Brazilian Portuguese.

## Clarifications

### Session 2026-09-27

- Q: Should the search also consider past transaction descriptions already assigned to a
  category? → A: No. Only Instruction hint terms and the category/subcategory's own name are
  searched (FR-004).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Find the category for a messy statement description (Priority: P1)

While registering or importing a transaction, the assistant sends the raw description to
`lookup_category` and gets back a short, ranked list of candidate categories, each with a
confidence indication and the hint term that matched, so it can pick the category without
asking the user.

**Why this priority**: This is the whole value of the feature — correct categorization on the
first try for descriptions that don't literally match any hint.

**Independent Test**: Register an Instruction on a category, call `lookup_category` with a
description that is a misspelled, spaced-out, accent-stripped or noise-padded variant of one of
its hint terms, and verify that category is returned first.

**Acceptance Scenarios**:

1. **Given** category `Mercado/Avulso` has the hint "Rede economia, Extra hiper", **When**
   the caller searches `PAG*REDEECONOMIA 0231`, **Then** `Mercado/Avulso` is the top result and
   the matched term reported is "Rede economia".
2. **Given** the same hint, **When** the caller searches `EXTRA HIPER 1234 SAO PAULO`, **Then**
   `Mercado/Avulso` is the top result.
3. **Given** a category hinted with "Carrefour", **When** the caller searches `CARREFUR COM IND`
   (misspelled), **Then** that category is returned among the results.
4. **Given** a category hinted with "Farmácia São João", **When** the caller searches
   `FARMACIA SAO JOAO` (no accents) or `farmasia sao joão` (sounds alike), **Then** that category
   is returned first.
5. **Given** two categories whose hints both partially match a description, **When** the caller
   searches it, **Then** both are returned, ordered from the strongest match to the weakest.

---

### User Story 2 - Clear "no match" answer (Priority: P2)

When nothing plausibly matches, the tool says so instead of returning weak, misleading
candidates, so the assistant knows to ask the user (and possibly record a new hint).

**Why this priority**: A wrong confident answer is worse than no answer for financial data;
false positives silently corrupt budget calculations.

**Independent Test**: Search a description unrelated to every registered hint and verify the
result is empty.

**Acceptance Scenarios**:

1. **Given** no hint resembles `UBER TRIP`, **When** the caller searches it, **Then** the
   result is an empty list (not an error).
2. **Given** a description that only shares generic noise with hints (store numbers, city
   names, processor prefixes such as `PAG*`, `MP*`, `IFD*`, `COM`, `LTDA`), **When** the caller
   searches it, **Then** no category is returned on the strength of that noise alone.

---

### User Story 3 - Keep the existing "list all hints" behavior (Priority: P3)

Callers that invoke `lookup_category` without a description keep receiving the full list of
categories with Instructions, exactly as today.

**Why this priority**: Backward compatibility for existing conversations and prompts; lowest
new value but must not regress.

**Independent Test**: Call `lookup_category` with no description and compare with the current
output.

**Acceptance Scenarios**:

1. **Given** three categories with Instructions, **When** the caller omits the description,
   **Then** all three are returned in the current order (parent category, then subcategory),
   with no score.
2. **Given** a description made only of whitespace, **When** the caller searches, **Then** it
   behaves as if the description was omitted.

---

### Edge Cases

- Description longer than the free-text limit (256 characters) → rejected by validation with a
  clear message.
- Description consisting only of noise (digits, punctuation, processor prefixes) → empty result.
- A hint term that is very short (1–2 letters) → never matched fuzzily or phonetically, only by
  exact whole-word equality, to avoid spurious hits.
- The same category matches through several of its hint terms → returned once, with its best
  score and best matching term.
- A hint appears on both a parent category and one of its subcategories → both are returned as
  distinct candidates; the more specific (subcategory) wins a tie.
- Instructions are edited through `update_category_instruction` → the next lookup reflects the
  new text immediately.
- Categories with no Instruction → still eligible to match through their own category/subcategory
  name (see FR-004).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `lookup_category` MUST accept an optional `description` input (the transaction
  description to categorize), limited to 256 characters.
- **FR-002**: When `description` is absent or blank, the tool MUST return exactly the current
  output (all categories with an Instruction, ordered by parent category then subcategory).
- **FR-003**: When `description` is provided, the tool MUST return only candidate categories
  whose match strength reaches a minimum confidence threshold, ordered by match strength
  (strongest first), limited to at most 5 candidates.
- **FR-004**: The searchable terms for a category MUST be each comma/semicolon-separated term of
  its Instruction and the category's own name (parent or subcategory name) — nothing else. Past
  transaction descriptions MUST NOT be searched or used as a signal: learning from history was
  intentionally removed, and the user controls matching exclusively through Instructions.
- **FR-005**: Matching MUST be insensitive to letter case, accents/diacritics, punctuation and
  whitespace differences, including words glued together (`REDEECONOMIA` ≈ "Rede economia").
- **FR-006**: Matching MUST tolerate small spelling differences (e.g. one or two letters
  inserted, omitted, swapped or replaced, proportional to term length).
- **FR-007**: Matching MUST recognize words that sound alike in Brazilian Portuguese even when
  spelled differently (e.g. `ç`/`ss`/`s`/`c`, `ch`/`x`, `ph`/`f`, `y`/`i`, `k`/`c`/`qu`, silent
  `h`, doubled consonants).
- **FR-008**: Before matching, the description MUST be stripped of common statement noise:
  numbers and store codes, payment-processor prefixes (such as `PAG*`, `MP*`, `IFD*`, `PIX`),
  legal-entity suffixes (such as `LTDA`, `ME`, `SA`, `COM`, `IND`) and trailing city names;
  noise alone MUST never produce a match.
- **FR-009**: Each candidate MUST include the category id, parent category, subcategory, the
  full Instruction, the specific term that matched, and a match score on a 0–1 scale.
- **FR-010**: A category MUST appear at most once per result, with its best score; ties are
  broken by preferring a subcategory over its parent, then alphabetical order, so results are
  deterministic.
- **FR-011**: The tool MUST remain read-only and MUST NOT change any category or Instruction.
- **FR-012**: The tool's description (as seen by the assistant) MUST explain the new parameter,
  the score meaning, the threshold behavior and the "empty means ask the user" guidance.
- **FR-013**: The domain documentation for `lookup_category` MUST be updated to describe the
  new behavior before the feature is marked implemented.

### Key Entities

- **Category**: parent category or subcategory; has a name and an optional Instruction.
- **Instruction hint term**: one comma/semicolon-separated fragment of a category's Instruction
  (e.g. "Extra hiper"); the unit that gets matched.
- **Lookup candidate**: a category proposed for a given description, with the matched term and
  a 0–1 score.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On a reference set of at least 50 real-world-style statement descriptions paired
  with their correct category, the correct category is the top candidate for at least 90% of
  them.
- **SC-002**: On the same reference set, at least 97% of descriptions have the correct category
  somewhere in the returned candidates.
- **SC-003**: On a reference set of at least 20 descriptions with no correct category, at least
  95% return an empty result.
- **SC-004**: With up to 500 categories carrying Instructions, a lookup answers fast enough to
  feel instant to the user (under 1 second).
- **SC-005**: Existing callers that don't pass a description see no change in output
  (100% of the current behavior's tests keep passing unchanged).

## Assumptions

- Descriptions and hints are in Brazilian Portuguese; phonetic rules target that language only.
- The default confidence threshold and the 5-candidate limit are tuned against the reference
  sets in SC-001–SC-003 and may be adjusted during planning without changing this spec.
- Categories are global (not scoped by account group), matching current behavior; group
  isolation is a separate, pending project-wide effort.
- The number of categories stays in the hundreds, so matching over all of them per lookup is
  acceptable; no pre-built search index is required by this spec.
- `create_transaction`, `update_transaction` and `import_statement` do not call the lookup
  automatically; the assistant decides when to use it. Auto-categorization on import is out of
  scope.
- The reference sets for SC-001–SC-003 will be built from anonymized/synthetic descriptions,
  never real personal financial data (per the constitution's Quality Gates).
