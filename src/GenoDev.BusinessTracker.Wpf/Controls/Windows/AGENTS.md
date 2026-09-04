# Shared secondary-window guidance

These rules cover `PopupWindow`, `PopupWindowHost`, their registry, and related title-bar/window controls.

## Hosting and session lifecycle

- `PopupWindow` is the single reusable shell with client-drawn content and native Windows chrome for the outer corners and shadow. Preserve two-way `IsOpen`, inherited DataContext, title/command bindings, padding, and optional dimensions when moving inline content into it.
- An explicit open request creates a new session when a live shell already exists; recreate the shell around current content/context so cursor placement, user size, pin/topmost state, and other window-local state reset. Registry restoration of a hidden window restores the existing session instead.
- Technical session replacement suppresses deferred logical-host activation and bypasses close animation. Ordinary user/ViewModel closes retain host reactivation and reverse animation.
- Treat initialization and closure as reentrant. Mark a window closed before unregistering, reject registration after closure, and never retain a stale HWND entry.
- Keep content and subscriptions until animated close reaches `Closed`. During reparenting, pin an inherited DataContext across the parentless gap, reattach content before rebuilding a `ContentTemplate`, then restore inheritance.
- ViewModels replacing popup editors unsubscribe the prior close event and clear the editor reference when the session closes.

## Placement, sizing, and interaction

- After real measurement, constrain the window bounds to the placement monitor's work area. Cursor placement uses the pointer monitor; centered placement uses the monitor containing the resulting window.
- Cursor offsets refer directly to the popup's top-left corner; the shell has no synthetic shadow gutter.
- Let DWM provide the popup's outer corner clipping, themed frame, shadow, and transparent client backing. Keep non-client rendering enabled, disable the system backdrop, enable redirection-bitmap alpha, and force one-pixel border margins on supported Windows builds; do not recreate these effects in WPF or in a companion HWND.
- Use `WindowChrome` caption and resize hit areas for native Windows movement, resizing, and screen-edge layouts. Keep interactive title-bar controls opted into client hit testing, and synchronize `ResizeMode` plus the native resize border with `IsResizable` and maximized state.
- Do not recreate native movement or resizing with WPF pointer handlers. External window managers that add application-edge snapping require their own application compatibility exclusion rather than HWND position filters.
- A natively unowned logical secondary window uses manual startup placement centered on its logical host; do not use `CenterOwner`.

## Registry, ownership, and visibility

- Register each live window in `PopupWindowRegistry` during `OnSourceInitialized` after HWND creation and unregister idempotently on close.
- Remove the registry entry immediately when an animated close is accepted. Do not remove it for minimize/hide because that retained HWND remains recoverable.
- Registry actions show/activate the exact hidden window and expose pin/unpin and close without dismissing the menu unnecessarily.
- Title-bar minimize uses `Hide()` and changes the bound open request to false without destroying the window; a later open restores the same hidden instance. Pin changes never alter visibility.
- Do not use native `Window.Owner` when the main window must be able to cover the secondary window. Keep an explicit logical host; host activation may place an unpinned popup behind it, while popup activation never raises the host.
- Host-driven minimization remembers normal/maximized state, performs the shared animated hide without changing logical `IsOpen`, then restores state while hidden and shows without activation when the host returns. Close with the logical host and detach all host handlers on close.
- Topmost windows enter/leave native topmost bands explicitly through the shared `TopmostButton`; never insert the host relative to a topmost popup. Host minimization still wins.
- Hold-to-peek uses `PeekThroughButton` and changes the opacity of the client visual while capture is held. The DWM redirection bitmap alpha makes that opacity reveal windows underneath without turning the HWND into a layered window; suppress the opaque native border during peek and restore its themed color on release.

## Rendering and animation

- Opening is a synchronized fade and center-origin scale from hidden values; start it immediately after synchronous placement. Closing uses the exact reverse transition. Honor the system client-area animation preference.
- Animate the client visual without introducing a synthetic shadow surface.
- Restorable hide commits the held hidden client-visual opacity and scale before detaching clocks and calling `Hide()`. Opening similarly commits final base values while clocks hold the same effective values, then detaches them to avoid cached DWM flashes. Do not animate `Window.Opacity`, because that reintroduces layered-window composition.
- Logical-host restoration waits for an active hide transition to commit before hiding, restores state while hidden, shows without activation, constrains normal bounds, and immediately replays opening animation.
