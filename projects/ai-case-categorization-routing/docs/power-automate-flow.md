# Power Automate Flow — Step-by-Step

This documents the flow logic for the AI Case Categorization & Routing solution. Field and table names are fictionalized versions of the real implementation.

## Trigger

**When a row is added, modified or deleted** (Dataverse connector)
- Table: `incident` (Case)
- Scope: Organization
- Trigger condition: only run when `new_sourcechannel` equals "Email" and the case role is ARC — this filters out cases created through other intake channels that don't need AI categorization.

## Step 1 — Get the Case's Queue

**Action: Get a row by ID** (Dataverse)
- Table: `queue`
- Row ID: the Case's owning queue (or a related queue lookup field, depending on how the case was routed at creation)
- Purpose: this queue record holds the `new_workcategorydefinition` field — the text that defines which categories are valid for this specific queue and what each one means.

## Step 2 — Build the AI Builder prompt

**Action: Compose** (or inline in the next step)
- Combine the queue's `new_workcategorydefinition` text with a short instruction, e.g.:
  "Read the email subject and body below. Classify it into exactly one of the categories described here: [queue's Work Category Definition]. Respond with only the category name."

This keeps the instruction consistent across all queues while letting each queue supply its own category list.

## Step 3 — Run the AI Builder prompt

**Action: Run a prompt** (AI Builder connector)
- Input 1: Email Subject (from the Case's related email or its `new_emailsubject` field)
- Input 2: Email Body (from the Case's related email or its `new_emailbody` field)
- Input 3: Prompt (the composed text from Step 2)
- Output: a text response — the category name the model selected

## Step 4 — Look up the matching Work Category record

**Action: List rows** (Dataverse)
- Table: `new_workcategory`
- Filter: `new_name` equals the AI Builder output text (or contains, if exact match is unreliable)
- Purpose: converts the AI's free-text answer into an actual Dataverse record reference, so the Case can be linked to it as a lookup, not just stamped with plain text.

## Step 5 — Update the Case

**Action: Update a row** (Dataverse)
- Table: `incident`
- Set `new_workcategory` to the matched Work Category record from Step 4

## Step 6 — Route based on escalation level

**Action: Condition** (or Switch)
- Check the matched Work Category record's `new_escalationlevel` field
- Branch: High Escalation -> assign the Case to the High Escalation team/queue
- Branch: Normal Escalation -> assign the Case to the Normal Escalation team/queue
- Additional branches can be added per escalation tier without changing Steps 1-5

**Action: Assign** or **Update a row** (setting `ownerid`)
- Sets the Case's owner to the resolved queue or team member

## Error handling notes

- If Step 4 finds no matching Work Category (AI Builder returned an unrecognized category, or the queue's definition text doesn't match any table row), the flow should fall back to a default "Unclassified" category and a manual-review queue, rather than leaving the Case unassigned.
- If the AI Builder call fails or times out, the same fallback path applies — the Case should never be left un-triaged because of an AI Builder error.
