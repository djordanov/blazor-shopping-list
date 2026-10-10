# Modes

1. Plain SSR + HTMX, without Blazor helpers
2. Plain SSR with Blazor helpers and Blazor interactivity
3. Interactive via SignalR
4. WASM
5. WASM + something for live updates

# Plain SSR templating + HTMX so far

## TODOs

+ add auth & auth
+ add CSRF middleware

## Do Maybe

* notifications on changes
* notifications with undo option on changes
* refactor HTMX minimal endpoints to extract common code
* change entity relationships to make item.section officially required 

## Won't Do

- user-notes and user-pictures on items
- sharing