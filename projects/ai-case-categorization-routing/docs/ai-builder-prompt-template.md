# AI Builder Prompt Template

This documents the structure of the "Run a prompt" call used for Case categorization, with a fictionalized example so the pattern is reusable outside the original client environment.

## The three inputs

The AI Builder connector receives exactly three values, kept deliberately narrow so the model's behavior stays predictable and auditable:

1. **Email Subject** — plain text, pulled from the Case's source email
2. **Email Body** — plain text, pulled from the Case's source email
3. **Prompt** — assembled at run time from the owning queue's Work Category Definition field

## How the prompt is assembled

The queue record stores a plain-text definition of its valid categories, for example:

    Billing Inquiry: questions about invoices, charges, or payment methods
    Technical Issue: product not working, error messages, login problems
    Account Change: requests to update account details, cancel, or upgrade
    General Question: anything that does not clearly match the above

The flow wraps this text in a consistent instruction before sending it to AI Builder:

    Read the email subject and body below. Classify it into exactly one
    of the categories described here:

    {Work Category Definition text from the queue record}

    Email Subject: {Email Subject}
    Email Body: {Email Body}

    Respond with only the category name, exactly as written above.
    Do not explain your answer.

## Example run

**Queue's Work Category Definition:**

    Billing Inquiry: questions about invoices, charges, or payment methods
    Technical Issue: product not working, error messages, login problems
    Account Change: requests to update account details, cancel, or upgrade
    General Question: anything that does not clearly match the above

**Email Subject:** "Charged twice this month"

**Email Body:** "Hi, I noticed two charges on my statement this month for the same subscription. Can you check this and refund the duplicate?"

**AI Builder output:** `Billing Inquiry`

The flow then looks up "Billing Inquiry" in the Work Category table (see `power-automate-flow.md`, Step 4), retrieves its escalation level, and routes the Case accordingly.

## Why "respond with only the category name"

AI Builder's Run a Prompt output is free text. Without this instruction, the model tends to add explanation ("This appears to be a Billing Inquiry because...") which breaks the exact-match lookup against the Work Category table in Step 4 of the flow. Constraining the output format is what makes the free-text response usable as a lookup key downstream.

## Tuning notes

- If categorization accuracy is low for a specific queue, the fix is almost always to make that queue's Work Category Definition more specific (clearer examples per category), not to change the flow logic.
- Category names in the Work Category table should match the wording used in the queue's definition text exactly, since Step 4's lookup depends on that consistency.
