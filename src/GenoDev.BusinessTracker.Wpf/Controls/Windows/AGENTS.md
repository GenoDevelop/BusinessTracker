# Shared secondary-window guidance

These rules cover `PopupWindow`, `PopupWindowHost`, their registry, and related title-bar/window controls.

## Hosting and session lifecycle

- `PopupWindow` is the single borderless, layered, reusable shell. Preserve two-way `IsOpen`, inherited DataContext, title/command bindings, padding, and optional dimensions when moving inline content into it.
- An explicit open request creates a new session when a live shell already exists; recreate the shell around current content/context so cursor placement, user size, pin/topmost state, and other window-local state reset. Registry restoration of a hidden window restores the existing session instead.
- Technical session replacement suppresses deferred logical-host activation and bypasses close animation. Ordinary user/ViewModel closes retain host reactivation and reverse animation.
- Treat initialization and closure as reentrant. Mark a window closed before unregistering, reject registration after closure, and never retain a stale HWND entry.
- Keep content and subscriptions until animated close reaches `Closed`. During reparenting, pin an inherited DataContext across the parentless gap, reattach content before rebuilding a `ContentTemplate`, then restore inheritance.
- ViewModels replacing popup editors unsubscribe the prior close event and clear the editor reference when the session closes.

## Placement, sizing, and interaction

- After real measurement, constrain the visible border to the placement monitor's work area. Cursor placement uses the pointer monitor; centered placement uses the monitor containing the resulting window.
- Cursor offsets refer to the visible corner, so subtract the shared shadow gutter before assigning `Left`/`Top` and then constrain to the work area.
- The layered HWND keeps a symmetric transparent shadow gutter while normal and removes gutter, shadow, and rounding while maximized. Movement, snapping, and resize calculations use the visible border.
- Use explicit pointer tracking and transparent WPF `Thumb`s; keep `ResizeMode="NoResize"` and do not attach `WindowChrome`. Snap only to work-area edges, never other application windows.
- Resize thumbs are sibling overlays of the inset visible border, centered on its edges with the same compact thickness for edges/corners. Gutter pixels need practically invisible nonzero alpha for native hit testing.
- During pointer drag, initialize and enforce the cursor-derived native target immediately after capture, rejecting OS movement corrections until capture ends.
- A natively unowned logical secondary window uses manual startup placement centered on its logical host; do not use `CenterOwner`.

## Registry, ownership, and visibility

- Register each live window in `PopupWindowRegistry` during `OnSourceInitialized` after HWND creation and unregister idempotently on close.
- Remove the registry entry immediately when an animated close is accepted. Do not remove it for minimize/hide because that retained HWND remains recoverable.
- Registry actions show/activate the exact hidden window and expose pin/unpin and close without dismissing the menu unnecessarily.
- Title-bar minimize uses `Hide()` and changes the bound open request to false without destroying the window; a later open restores the same hidden instance. Pin changes never alter visibility.
- Do not use native `Window.Owner` when the main window must be able to cover the secondary window. Keep an explicit logical host; host activation may place an unpinned popup behind it, while popup activation never raises the host.
- Host-driven minimization remembers normal/maximized state, performs the shared animated hide without changing logical `IsOpen`, then restores state while hidden and shows without activation when the host returns. Close with the logical host and detach all host handlers on close.
- Topmost windows enter/leave native topmost bands explicitly through the shared `TopmostButton`; never insert the host relative to a topmost popup. Host minimization still wins.
- Hold-to-peek uses `PeekThroughButton` and synchronized content/shadow opacity while capture is held. The content remains hit-testable.

## Rendering and animation

- Render visible shadow pixels in a separate non-activating, click-through companion HWND directly behind the content. Track bounds, visibility, opacity, topmost state, z-order, and closure together.
- Keep content-window gutter alpha zero except for narrow resize hit bands so shadows never block applications underneath.
- Opening is a synchronized fade and center-origin scale from hidden values; start it immediately after synchronous placement. Closing uses the exact reverse transition. Honor the system client-area animation preference.
- Animate the visible border and companion shadow content rather than layered HWND transforms. Cancel shadow fade before applying peek opacity.
- Restorable hide commits the held hidden opacity/scale base values before detaching clocks and calling `Hide()`. Opening similarly commits final base values while clocks hold the same effective values, then detaches them to avoid cached DWM flashes.
- Logical-host restoration waits for an active hide transition to commit before hiding, restores state while hidden, shows without activation, constrains normal bounds, and immediately replays opening animation.
