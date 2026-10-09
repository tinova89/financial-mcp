# Feature Specification: Phonetic/Fuzzy Category Lookup

**Feature Branch**: `013-FT-fuzzy-category-lookup`

**Created**: 2026-09-27

**Status**: Draft

**Input**: User description: "Phonetic/fuzzy search for lookup_category"

## Context

Today `lookup_category` has no input: it returns every category with Hints (keywords like
"Rede economia, Extra hiper"; today called "Instruction") and the assistant compares them with the
transaction by itself. Statement descriptions rarely match hints literally (`PAG*REDEECONOMIA 0231`,
`CARREFUR COM IND`), so the assistant picks the wrong category or asks the user.

This feature adds an optional description to `lookup_category` and returns the most likely
categories, tolerant to typos, spacing, accents and words that sound alike in pt-BR. It also
renames "Instruction" to **Hints** and `update_category_instruction` to **`update_category_hints`**.

## Clarifications

### Session 2026-09-27

- Q: Search old transaction descriptions too? → A: No, only hint terms and category names (FR-004).
- Q: Separate response shape for list mode? → A: No, one shape; matched term and score are null
  in list mode (FR-002f).

### Session 2026-10-01

- Q: Return the full Hints in search mode? → A: No, Hints is null in search mode (FR-009).
- Q: Rename "Instruction" to "Hints"? → A: Yes, in this feature, tool included (FR-014–FR-017).
- Q: Rename internal code names too? → A: Yes, except already-applied migrations (FR-018).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Find the category for a messy description (Priority: P1)

The assistant sends the raw description and gets a short ranked list of categories, each with the
matched hint term and a score, so it can choose without asking the user.

**Why this priority**: Main value — right category on the first try.

**Independent Test**: Save Hints on a category, search a misspelled/noisy variant of a hint term,
check the category comes first.

**Acceptance Scenarios**:

1. **Given** `Mercado/Avulso` has Hints "Rede economia, Extra hiper", **When** searching
   `PAG*REDEECONOMIA 0231`, **Then** it is first, matched term "Rede economia", Hints null.
2. **Given** the same Hints, **When** searching `EXTRA HIPER 1234 SAO PAULO`, **Then**
   `Mercado/Avulso` is first.
3. **Given** Hints "Carrefour", **When** searching `CARREFUR COM IND`, **Then** that category is
   returned.
4. **Given** Hints "Farmácia São João", **When** searching `FARMACIA SAO JOAO` or
   `farmasia sao joão`, **Then** that category is first.
5. **Given** two categories partially matching, **When** searching, **Then** both are returned,
   strongest first.

---

### User Story 2 - Clear "no match" answer (Priority: P2)

When nothing matches well, the result is empty, so the assistant asks the user.

**Why this priority**: A confident wrong answer silently breaks budgets.

**Independent Test**: Search an unrelated description; the result is empty.

**Acceptance Scenarios**:

1. **Given** no hint resembles `UBER TRIP`, **When** searching it, **Then** the result is an empty
   list, not an error.
2. **Given** a description sharing only noise with hints (`PAG*`, `MP*`, `IFD*`, `COM`, `LTDA`,
   numbers, cities), **When** searching, **Then** nothing is returned because of that noise.

---

### User Story 3 - Keep "list all hints" (Priority: P3)

Without a description, the tool lists all categories with Hints, like today.

**Why this priority**: Existing usage must not break.

**Independent Test**: Call with no description and compare with today's output.

**Acceptance Scenarios**:

1. **Given** three categories with Hints, **When** no description is sent, **Then** all three
   return in today's order, with matched term and score null.
2. **Given** an empty or whitespace-only description, **Then** the result equals no description.
3. **Given** `Mercado` and `Mercado/Avulso` both with Hints, **Then** `Mercado` comes first.
4. **Given** 30 categories with Hints and 10 without, **Then** exactly the 30 return (no limit
   of 5).
5. **Given** a category whose Hints are only whitespace, **Then** it is not returned.
6. **Given** no category has Hints, **Then** the result is an empty list, not an error.
7. **Given** the description `1234`, **Then** it runs a search and returns an empty list.

---

### User Story 4 - Manage hints with clear names (Priority: P2)

The user saves keywords with `update_category_hints` and sees them in the `hints` field; text
saved before as Instruction is kept.

**Why this priority**: Search depends on good hints; the old name read like an instruction.

**Independent Test**: Save Hints with the new tool, then list them through `lookup_category`.

**Acceptance Scenarios**:

1. **Given** a category, **When** `update_category_hints` saves "Rede economia, Extra hiper",
   **Then** the answer shows it in `hints` and the next lookup uses it.
2. **Given** a category had the Instruction "Carrefour", **When** the feature ships, **Then** its
   Hints are "Carrefour".
3. **Given** the feature shipped, **Then** `update_category_instruction` no longer exists.
4. **Given** any answer of both tools, **Then** it has `hints` and no `instruction` field.

---

### Edge Cases

- Description over 256 characters → rejected by validation.
- Description with only noise → empty result.
- Hint terms of 1–2 letters → only exact whole-word matches.
- Category matching through several terms → returned once, with its best score and term.
- Same hint on a parent and its subcategory → both returned; subcategory wins a tie.
- Hints changed → the next lookup uses them right away.
- Category without Hints → can still match by its name in search mode (FR-004).
- Hints over 2000 characters → rejected, like today.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `lookup_category` MUST accept an optional `description` (max 256 characters).
- **FR-002**: Missing or blank `description` MUST run **list mode**, returning what it returns
  today:
  - **FR-002a**: Blank = not sent, empty, or only whitespace. Anything else (even `1234`) is a
    search.
  - **FR-002b**: Only categories (parent or subcategory) with non-blank Hints; the name alone never
    includes a category here.
  - **FR-002c**: No threshold and no 5-item limit.
  - **FR-002d**: Ordered by parent category, then subcategory; parent first; stable across calls.
  - **FR-002e**: Each item has id, parent category, subcategory (empty for a parent) and Hints
    exactly as saved.
  - **FR-002f**: Same response shape as search; matched term and score are null.
  - **FR-002g**: No categories with Hints → empty list, not an error.
  - **FR-002h**: Same ids, names, Hints and order as before; only `hints` (was `instruction`) and
    the two null fields differ.
- **FR-003**: With `description`, MUST return only candidates above a minimum confidence, strongest
  first, at most 5.
- **FR-004**: Searchable terms MUST be the Hints terms (split by comma/semicolon) and the category's
  own name — never past transaction descriptions.
- **FR-005**: Matching MUST ignore case, accents, punctuation, whitespace and glued words
  (`REDEECONOMIA` ≈ "Rede economia").
- **FR-006**: Matching MUST tolerate small typos, proportional to term length.
- **FR-007**: Matching MUST treat pt-BR sound-alikes as equal (`ç`/`ss`/`s`/`c`, `ch`/`x`,
  `ph`/`f`, `y`/`i`, `k`/`c`/`qu`, silent `h`, double consonants).
- **FR-008**: Statement noise MUST be removed before matching (numbers, store codes, `PAG*`, `MP*`,
  `IFD*`, `PIX`, `LTDA`, `ME`, `SA`, `COM`, `IND`, trailing cities); noise alone never matches.
- **FR-009**: One response shape for both modes: id, parent category, subcategory, Hints, matched
  term, score (0–1). List mode fills Hints (matched term/score null); search mode fills matched
  term/score (Hints null).
- **FR-010**: Each category at most once, with its best score; ties: subcategory first, then
  alphabetical.
- **FR-011**: The tool MUST stay read-only.
- **FR-012**: The tool description MUST explain the new parameter, the score, the threshold and
  "empty means ask the user".
- **FR-013**: The domain documentation for `lookup_category` MUST be updated before release.
- **FR-014**: "Instruction" MUST be called **Hints** everywhere the user or assistant sees it.
- **FR-015**: Answers MUST use `hints` instead of `instruction`, with no copy under the old name.
- **FR-016**: `update_category_instruction` MUST become `update_category_hints` (parameter `hints`),
  same behavior; the old name MUST NOT remain.
- **FR-017**: Every saved Instruction MUST be kept unchanged as the same category's Hints.
- **FR-018**: The rename MUST also cover internal names (storage, domain, operations, validation,
  messages, comments, tests); already-applied database changes MUST NOT be edited (Quality Gate 5).

### Key Entities

- **Category**: parent or subcategory, with a name and optional Hints.
- **Hints**: user-set keywords showing how a category appears in statements; never learned.
- **Hint term**: one comma/semicolon-separated part of Hints; the unit matched.
- **Lookup candidate**: a suggested category with matched term and score; no full Hints.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On ≥50 realistic descriptions, the correct category is first in ≥90%.
- **SC-002**: On the same set, the correct category is among the results in ≥97%.
- **SC-003**: On ≥20 descriptions with no correct category, ≥95% return empty.
- **SC-004**: With up to 500 categories with Hints, a lookup takes under 1 second.
- **SC-005**: Without a description, 100% of calls return the same categories, Hints and order as
  before; only `hints` naming and the null fields differ.
- **SC-006**: 100% of saved Instructions survive as Hints, and "instruction" appears nowhere for
  this concept except already-applied migrations.

## Assumptions

- Descriptions and hints are pt-BR; phonetic rules target only pt-BR.
- Threshold and 5-item limit are tuned in planning against SC-001–SC-003.
- Categories stay global (group isolation is a separate pending task).
- Hundreds of categories; no pre-built index required.
- Other tools don't call the lookup automatically; auto-categorization on import is out of scope.
- Reference sets use synthetic data only.
- The rename breaks old callers; accepted, no alias, since the assistant reloads tools each
  connection.
