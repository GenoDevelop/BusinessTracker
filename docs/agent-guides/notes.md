# Notes and rich text — cross-cutting guide

- Persist note content as opaque RTF in `Note.ContentRtf`; RichTextBox/FlowDocument conversion belongs to WPF.
- Keep paged note lists lightweight by projecting only identity/name. Load selected RTF details separately with latest-request-wins cancellation.
- Creating a note sets its name and empty content. Rich-content editing is a separate explicit save; formatting never persists merely because focus or selection changed.
- Formatting controls preserve editor focus/selection and reflect the current caret/selection format.
- Changing the active note while dirty requires save/discard/cancel. Apply the guard to direct selection and indirect paging, filtering, sorting, and newly created-note selection; cancel preserves editor content and logical selection.
