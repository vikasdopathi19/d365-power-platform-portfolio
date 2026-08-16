# AI-Powered Case Categorization & Routing — D365 + AI Builder

A no-code Power Automate solution that categorizes incoming Cases using AI Builder's "Run a prompt" connector, then automatically routes each Case to the correct queue or team based on the categorization result and escalation level.

Built for a support-intake scenario where Cases arrive via email and need to be triaged by work category before a human ever looks at them.

## Business problem

Cases created from an email channel need to be classified into a work category before they can be routed to the right team. Doing this manually means every case sits untouched until someone reads the email and decides where it belongs. This solution classifies and routes the case automatically at creation time.

## Architecture

    Case created (source: Email, role: ARC)
            |
            v
    Power Automate flow triggers on Case create
            |
            v
    Get the Case's Queue record -> read its Work Category Definition field
    (this defines which category values are valid for that queue)
            |
            v
    AI Builder "Run a prompt" connector
    Inputs: Email Subject, Email Body, Prompt (built from the queue's
    Work Category Definition)
            |
            v
    AI Builder returns a category name (free text, derived from the prompt)
            |
            v
    Flow looks up that category name in the Work Category table
            |
            v
    Case is updated with the matched Work Category
            |
            v
    Based on the Work Category's escalation level (e.g. High Escalation,
    Normal Escalation), the flow assigns the Case to the matching queue
    or team members

## Why the prompt is queue-specific, not global

Different queues handle different kinds of work, so the same email content can mean different things depending on which queue the case landed in. Rather than one global prompt trying to cover every category across the business, each queue stores its own Work Category Definition — a plain-text description of the categories relevant to that queue. The flow reads this definition at run time and builds the AI Builder prompt from it, so the categorization logic scales to new queues without touching the flow or the prompt itself. Adding a new queue's categories is a data change, not a flow change.

## Key design decisions

- **Only three inputs to the AI model** — email subject, email body, and the prompt text. Keeping the AI Builder call this narrow makes it predictable, cheap to run, and easy to debug when a categorization looks wrong (you can inspect exactly what the model saw).
- **Category matching is text lookup, not a fixed choice list** — the AI Builder output is free text, matched against the Work Category table by name. This means new categories can be added to the table without redeploying the flow or the prompt structure.
- **Escalation level drives routing, not category directly** — the Work Category record carries an escalation level (e.g. High Escalation, Normal Escalation), and routing logic branches on that level. This keeps the routing rules centralized on the Work Category table rather than duplicated across flow conditions for every possible category.

## Required Dataverse schema (fictionalized, for demo purposes)

| Entity | Field | Type | Purpose |
|---|---|---|---|
| `incident` (Case) | `new_sourcechannel` | Choice | Identifies email-originated, ARC-role cases |
| `incident` (Case) | `new_workcategory` | Lookup | Set by the flow after AI Builder categorization |
| `queue` | `new_workcategorydefinition` | Text (multiline) | Defines valid categories and their meaning for this queue's prompt |
| `new_workcategory` (Work Category) | `new_name` | Text | Category name, matched against AI Builder output |
| `new_workcategory` (Work Category) | `new_escalationlevel` | Choice | Drives which team/queue the case is routed to |

## What's intentionally out of scope

This documents the design and flow logic rather than including an exported flow package, since the actual flow contains client-specific queue and category data. The next addition to this project would be a sanitized flow export (.zip) built against a demo environment with the fictional schema above.
