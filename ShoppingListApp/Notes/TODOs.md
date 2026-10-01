# Modes

1. Plain SSR + HTMX, without Blazor helpers
2. Plain SSR with Blazor helpers and Blazor interactivity
3. Interactive via SignalR
4. WASM
5. WASM + something for live updates

# Functionality

### ShoppingList

1. Display: Plain table
2. Add new: Plain form on same site as the table → add button inserts an empty row in edit more into bottom of table
3. Delete: Delete button → DELETE to /shopping-list/<id> → 204 No Content
4. Edit: Edit button → turns row editable?

### Items

URL: /shopping-list/<id> displays both sections and items

API-/ HTMX-URLS: /shopping-list/<id>/section and /shopping-list/<id>/item




Stretch: 
- user-defined sort order?
- comments or additional information on table?