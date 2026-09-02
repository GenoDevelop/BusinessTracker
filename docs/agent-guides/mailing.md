# Order mailing — cross-cutting guide

Read this guide for mailing UI, templates, SMTP configuration, attachments, persistence, delivery, or tests.

## Authoring manual

- [Mail and snippet authoring manual](../mailing/template-authoring.md) is the canonical, standalone Polish reference for users and external AI chats. Read it when changing authoring behavior, and update its supported syntax, variables, formatting, scopes, examples, editor/image limits, and rendering lifecycle in the same change whenever those capabilities change. Keep it complete without requiring repository access; exclude private account/customer data and database-specific snippet inventories.

## Templates and preview

- Mailing is a sibling Orders module. SMTP accounts, templates, snippets, and history are global; order composition starts from an order and client e-mail.
- Templates support HTML-encoded `{{ dotted.variable }}`, snippets `{{> snippet_key }}`, optional blocks, and order product/packing-material loops. Subjects allow scalars only.
- Validate the full reachable snippet graph before save: reject missing references and direct/indirect cycles with the cycle path, cap nesting at 32, and retain renderer guards.
- Editors can preview unsaved HTML with the same renderer/order context used for delivery; explicit no-order preview displays raw HTML.
- Inline PNG/JPEG/GIF images are self-contained Base64 data URLs in persisted HTML, including snippets and queued/resend snapshots; never persist local file paths. Bind mail HTML editors through `MailHtmlEditor.Html`: its editable `Text` uses short session-only references and must never be saved directly. `MailInlineImages` validates persisted HTML and the SMTP factory converts images to deduplicated CID-linked MIME resources. They follow HTML history retention; ordinary attachment retention remains separate. Keep image insertion shared across all mail HTML editors.
- `HtmlPreview` uses WPF `WebBrowser` in ordinary views. Inside layered `PopupWindow`, use the bounds-synchronized opaque companion window and contain expected teardown COM races.
- New/resend composers are resizable/maximizable popup sessions. Put initial/minimum dimensions on `PopupWindowHost`; keep the composer root stretch-aligned without fixed dimensions.

## Queue and delivery

- Queueing snapshots recipient, subject, rendered HTML, and exact attachment bytes before delivery.
- SMTP transmission runs in the hosted outbox processor outside MediatR's transaction, atomically claims pending rows across instances, and records sent, failed, or uncertain outcomes. Never blindly retry uncertain delivery.
- Build attachments explicitly as Base64 MIME parts with attachment disposition and UTF-8 filenames. Log count, total bytes, and metadata before send plus the final `MailMessage` attachment count, never content.
- Retain successful attachment bytes for seven days from `SentAtUtc`; keep pending/processing/failed bytes. Cleanup preserves the manifest, SHA-256, source identity, and deletion time.
- Resend always opens the composer and reports expired manual files and added/removed/renamed/content-changed template attachments before confirmation.
- Keep transport behind `IMailOutboxProcessor`. The built-in implementation supports authenticated SMTP and optional STARTTLS; Gmail uses port 587.
- SMTP settings are central in PostgreSQL. By explicit product decision the password is temporarily plain text: never list, display, log, export, or expose it. Empty edit preserves the existing password.

## Attachment and editor UX

- Use `MailAttachmentCard` everywhere: filename, formatted size, symbolic surface, hover actions, and a single horizontal scrolling row.
- Resend differences are yellow warning cards in that same row with full historical filename/reason tooltips. Each requires explicit remove or replace/accept before queueing.
- Added/changed current template files remain pending yellow cards until accepted or removed; never duplicate them as ordinary attachments. Available bytes allow download/accept/remove; unavailable files allow replace/remove.
- Save-to-computer appears wherever bytes exist. Expected access/I/O failures remain in inline attachment validation; expired/missing files expose no unavailable download action.
- Attachment cards use semantic theme brushes: teal accents for ordinary cards and amber accents for warnings. Keep glyphs readable against normal and hovered surfaces in the active palette.
- Optional attachment validation collapses completely when null. Composer header rows share label columns so recipient/subject editors align.
- Resend shows the original template name read-only and cannot switch/reapply templates; new composition retains its selector/apply action.
- Templates, snippets, and SMTP accounts expose refresh beside create and use shared entity-specific popup confirmations. Successful create reselects the returned ID; ordinary refresh preserves selection.
- Template attachments have one mutable current version. Replacement updates it in place; queued messages retain their exact snapshot until delivery/cleanup.
