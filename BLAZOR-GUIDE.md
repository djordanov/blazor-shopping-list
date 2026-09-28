# Blazor Guide for a Razor Pages Dev — Building the Shopping List App

Written for you specifically: experienced C#/.NET backend dev (Azure Functions), knows Razor Pages (personal-expenses app), went through the Blazor reading-list tutorial a few months ago, knows the SSR vs Interactive vs WASM distinction. This is the cheat sheet + getting-started plan, not a beginner tutorial.

---

## 1. Mental model shift from Razor Pages

| Razor Pages (what you know) | Blazor (.NET 8/9 unified model) |
|---|---|
| `Page.cshtml` + `Page.cshtml.cs` (PageModel) | One `.razor` file: markup + `@code {}` block together |
| Request/response per interaction — every POST reloads the page | Page can become **stateful** (Interactive) — events handled over a persistent connection (SignalR WebSocket), no full reload |
| `OnGet`/`OnPost` handlers | Lifecycle methods (`OnInitializedAsync`, `OnParametersSetAsync`, …) + event handlers |
| TagHelpers (`asp-for`, `asp-page`) | Built-in components (`InputText`, `EditForm`, `NavLink`) |
| Handler params from routing (`@page "{id:int}"`) | `[Parameter]` properties from `@page "/items/{Id:int}"` template |
| `HttpContext` everywhere | `HttpContext` only in SSR mode; inject services instead (`@inject`) |
| Model binding on every request | `@bind` two-way binding in interactive components; form values posted to a static SSR component via `[SupplyParameterFromForm]` |
| Redirect via `RedirectToPage` | `NavigationManager.NavigateTo(...)` |

Key structural difference: **there is no code-behind separation by default.** Everything lives in the `.razor` file under `@code { }`. You can split into a `.razor.cs` partial class if you prefer (most do, once files grow).

## 2. Project structure

A Blazor Web App (the .NET 8+ template) looks like:

```
dotnet-blazor-shopping-list/
├── Program.cs
├── appsettings.json
├── Models/ShoppingItem.cs, ShoppingList.cs
├── Data/AppDbContext.cs
├── Components/
│   ├── App.razor                 # root component (<Routes> inside, like _Layout.cshtml)
│   ├── Routes.razor              # <Router>, matches @page directives
│   ├── _Imports.razor            # like _ViewImports.cshtml — global @using for components
│   ├── Layout/
│   │   ├── MainLayout.razor       # like _Layout.cshtml — @Body is the @RenderBody() equivalent
│   │   └── NavMenu.razor
│   └── Pages/
│       ├── Home.razor
│       ├── ShoppingListPages/
│       │   ├── Index.razor
│       │   ├── Create.razor
│       │   └── Edit.razor
│       └── Error.razor, NotFound.razor
└── wwwroot/
```

Conventions:
- `@page "/items"` = the route template. Routing is attribute-based in the component itself, not in `@page`-folder structures.
- `_Imports.razor` applies `@using` to all `.razor` files below it — put your `Models`, `Data` namespaces here once.
- `app.MapRazorComponents<App>()` in `Program.cs` replaces `MapRazorPages` / `MapControllers`.

## 3. Render modes — when to use which

You know the theory; here's the practical decision table:

| Mode | `@rendermode` | Runs where | State lives | Use for shopping list |
|---|---|---|---|---|
| **Static SSR** | *(none)* | Server, per request | HTTP-only (cookies/forms), no `@bind`, no `@onclick` | Index list read-only, delete via form POST |
| **Interactive Server** | `InteractiveServer` | Server, over SignalR | In server memory (circuit) — survives navigation | Create/Edit forms, check-off items, adding items without reload |
| **Interactive Auto** | `InteractiveAuto` | WASM after first download; Server before | Client (after download) | Only if you want offline/PWA later |
| **Interactive WASM** | `InteractiveWebAssembly` | Browser | Browser memory | Only needed if no server round-trips wanted at all |

Notes:
- Render mode is set per **page/component**: `@rendermode InteractiveServer` on a page, or globally via `AddInteractiveServerRenderMode()` on `MapRazorComponents` in Program.cs.
- Interactive components **prerender** by default (SSR'd first, then the connection activates) — that's why you see `Loading...` / double `OnInitializedAsync` in dev. Disable with `prerender:false` if it bites you.
- **Everything rendered inside an interactive component is interactive too** — a boundary is set once and children inherit it.
- You cannot stream `HttpContext` into an interactive component; it's `null` there.

**Shopping list plan:** make `Index.razor`, `Create.razor`, `Edit.razor` `InteractiveServer`. That gives instant add/remove/check-off with no page reloads — the demo of *why* Blazor is different from Razor Pages.

## 4. Component anatomy

```razor
@page "/items/{Id:int}"
@rendermode InteractiveServer

@using MyApp.Models
@using Microsoft.EntityFrameworkCore
@inject IDbContextFactory<AppDbContext> DbFactory
@inject NavigationManager NavigationManager
@implements IAsyncDisposable

<h1>Edit item</h1>

@if (Item is null)
{
    <p><em>Loading...</em></p>
}
else
{
    <EditForm Model="Item" OnValidSubmit="UpdateItem" FormName="edit" Enhance>
        <DataAnnotationsValidator />
        <ValidationSummary />
        <InputText @bind-Value="Item.Name" />
        <ValidationMessage For="() => Item.Name" />
        <button type="submit">Save</button>
    </EditForm>
    <button @onclick="DeleteItem">Delete</button>
}

@code {
    [Parameter] public int Id { get; set; }

    public ShoppingItem? Item { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Item = await db.Items.FindAsync(Id);
    }

    private async Task UpdateItem() { /* EF save, then NavigationManager.NavigateTo("/items") */ }
    private async Task DeleteItem() { /* same pattern */ }
}
```

Directives you'll actually use:

- `@page` — route
- `@rendermode` — interactivity
- `@inject` — DI (constructor injection isn't available; this is the way)
- `@implements`, `@inherits`, `@layout` — what they say
- `@code { }` — the "code-behind" block (fields, properties, methods)
- `@attribute [Authorize]` — per-page auth

## 5. Lifecycle (the big change from OnGet/OnPost)

There is no per-request handler anymore in interactive mode. Instead:

1. `SetParametersAsync` — params from route/parent assigned
2. `OnInitialized` / `OnInitializedAsync` — **load data here** (runs on circuit start; on prerender runs twice — once SSR, once on activation)
3. `OnParametersSet` / `OnParametersSetAsync` — after params change (e.g. navigating `/items/1` → `/items/2` reuses the same component instance, so `OnInitializedAsync` does *not* rerun — use this)
4. Render
5. `OnAfterRenderAsync(firstRender)` — after DOM is real; JS interop only valid from here

Rerenders happen automatically when state changes (event handler runs → component re-renders). No manual "return Page()".

⚠️ Navigation between different route parameters **reuses the component** — refresh EF data in `OnParametersSetAsync`, not `OnInitializedAsync`, or you'll see stale data in Edit pages.

## 6. Data access pattern (use this, not what the movie tutorial did)

From your reading-list app you've already seen the recommended .NET 8+ pattern — keep it:

```csharp
// Program.cs
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite(...));
```

- Inject `IDbContextFactory<AppDbContext>` and `CreateDbContextAsync()` per operation — components outlive requests (interactive mode!), so a scoped DbContext dies with the wrong thing. This is the same "don't capture a DbContext in a long-lived object" rule you know from backend work.
- For a long-lived **interactive page** holding a list: `@implements IAsyncDisposable`, create the context in `OnInitializedAsync`, expose `IQueryable` or a materialized list, dispose in `DisposeAsync`.
- Static SSR pages (like a read-only Index): create the context in `OnInitializedAsync`, query with `.ToListAsync()` before rendering, or use `QuickGrid` with `Items="@db.Items"` (EF adapter streams query results *during* render — that's what `AddQuickGridEntityFrameworkAdapter()` was for).

## 7. Events & binding — the "full stack" magic

These only work in **interactive** components (in static SSR they throw at build/run):

```razor
<button @onclick="AddItem">Add</button>
<button @onclick="() => RemoveItem(item)">Remove</button>          <!-- lambda to close over the item -->
<input @bind="item.Name" />                                        <!-- two-way -->
<input @bind="item.Name" @bind:event="oninput" />                  <!-- live, not onchange -->
<input @onchange="(e => ...)" />                                   <!-- manual when you need raw event -->
<button @onclick="AddItemAsync" @onclick:preventDefault />          <!-- like e.preventDefault -->
```

- `@bind-Value` on `InputText`/`InputDate`/`InputSelect`/`InputCheckbox` — the built-in form components. `InputSelect` is the one you'll reach for most in a shopping list (categories/units).
- Events can be `async Task` — the UI re-renders when the task completes.
- `EventCallback<T>` for child→parent communication:

```razor
<!-- Child -->
[Parameter] public EventCallback<ShoppingItem> OnItemDeleted { get; set; }
<button @onclick="() => OnItemDeleted.InvokeAsync(Item)">×</button>

<!-- Parent -->
<ListItems OnItemDeleted="HandleDeleted" />
```

This replaces "partial view + handler postback" — you'll build the whole shopping list out of small components calling each other like this.

## 8. Forms & validation

Two worlds — know which you're in:

1. **Static SSR form** (`EditForm` with `method="post"`, `FormName="..."`, `Enhance`): values bind via `[SupplyParameterFromForm]` on POST — this is what your reading-list Edit page does. Form name + antiforgery handled automatically (`UseAntiforgery()` middleware). Choose this when you want a plain POSTable form (SEO-friendly, works without JS).
2. **Interactive form** (`EditForm` + `OnValidSubmit` handler, no `FormName` needed): model + `DataAnnotationsValidator` + your DataAnnotations (`[Required]`, `[Range]`) drive validation client-side-ish; submit runs your handler directly. Choose this for the shopping list UX.

Both use `<DataAnnotationsValidator />`, `<ValidationSummary />`, `<ValidationMessage For="() => Model.Prop" />` — same validation attributes you already use in Razor Pages and Azure Functions DTOs.

## 9. Routing & navigation

- `@page "/items"` and `@page "/items/{Id:int}"` — route constraints work like Razor Pages (`{Id:int}`, `{Slug?}`).
- `[Parameter] public int Id { get; set; }` gets the route value — no `OnGet(int? id)` handler signature dance.
- `NavigationManager.NavigateTo("/items", forceLoad: false)` — SPA-style redirect, no browser reload.
- `NavigationManager.NotFound()` → shows `NotFound.razor` (you saw this in the reading list Edit page).
- `NavLink` component = `asp-page` anchor with automatic active-class highlighting:
  ```razor
  <NavLink href="/items" Match="NavLinkMatch.All">Items</NavLink>
  ```

## 10. Layout & wwwroot

- `MainLayout.razor` = `_Layout.cshtml`. `@Body` where the page goes. `@layout MainLayout` sets it (or inherit from `App.razor` default).
- `App.razor` holds `<Routes>`, `<HeadOutlet>`, base HTML — replaces the root `_ViewStart`/page html wrapper.
- CSS: co-located `MyComponent.razor.css` = **CSS isolation scoped to that component**. Global CSS goes to `wwwroot/css/app.css`. `MapStaticAssets()` replaces `UseStaticFiles` (fingerprinted, better caching).
- Plain `<script>` in a component doesn't reliably load — use JS interop (`IJSRuntime`) or put scripts in `App.razor` / wwwroot.

## 11. Things that will bite you (coming from Razor Pages)

1. **`@bind`/`@onclick` in a static SSR page throws** — the build error looks cryptic; remember: no `@rendermode`, no events.
2. **Prerendering runs `OnInitializedAsync` twice** and `HttpContext` is null on the second (interactive) run. Guard with `if (OperatingSystem.IsBrowser())`-style checks or just don't use `HttpContext` in pages.
3. **`OnParametersSet` vs `OnInitialized` on route param change** — stale Edit page data is symptom #1.
4. **Scoping**: interactive components are **singleton-lifetime-ish** from the DI container's perspective (a circuit). Injecting a scoped service like a DbContext breaks — hence the factory pattern. For custom services use `@inject` but register them appropriately (`AddScoped` works via circuit scope, but DbContext specifically needs the factory).
5. **QuickGrid + EF in SSR mode** needs `AddQuickGridEntityFrameworkAdapter()` or you'll get "no IQueryable sync enumeration" errors.
6. **Parameters are read-only after set** — overwriting `[Parameter]` properties in code doesn't rebind to the parent; use a private field for local edits.
7. **Overposting** still applies: `[SupplyParameterFromForm]` on SSR forms binds the model — bind only what you accept (same caution as Razor Pages model binding).

## 12. Three ways to check off an item — the render modes in practice

The same feature ("toggle `IsPurchased` on a shopping item") looks completely different depending on the render mode. This is the clearest way to internalize the difference between the three worlds.

### a) Static SSR — form POST, server re-renders the page

No `@rendermode`, no `@bind`, no `@onclick`. The page is just HTML; the browser POSTs a form; the server runs the toggle, re-renders the whole page, sends back new HTML. This is **exactly the Razor Pages model** you already know (`OnPost` handler that re-renders the page).

Lifecycle: **everything is per-request.** `OnInitializedAsync` runs on *every* request (GET and POST alike), no state survives between requests, and the form's data arrives via `[SupplyParameterFromForm]` — the moral equivalent of model binding in Razor Pages. The `OnValidSubmit` handler is executed server-side during the render pass of the POST request, and the response to that POST *is* the re-rendered page — no redirect needed.

```razor
@page "/items"
@using Microsoft.EntityFrameworkCore
@inject IDbContextFactory<AppDbContext> DbFactory

<h1>Shopping list</h1>
<ul>
@foreach (var item in Items)
{
    <li>
        @item.Name
        <EditForm method="post" FormName="toggle" OnValidSubmit="ToggleItem" class="d-inline">
            <input type="hidden" name="ItemId" value="@item.Id" />
            <button type="submit">@(item.IsPurchased ? "Undo" : "✓")</button>
        </EditForm>
    </li>
}
</ul>

@code {
    [SupplyParameterFromForm]      // binds the hidden "ItemId" input on POST
    private int? ItemId { get; set; }

    private List<ShoppingItem> Items = [];

    protected override async Task OnInitializedAsync()   // runs on EVERY request
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Items = await db.Items.AsNoTracking().ToListAsync();
    }

    // Runs on the server when the form is POSTed; the POST response is the
    // re-rendered page, so refresh the list here.
    private async Task ToggleItem()
    {
        if (ItemId is int id)
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            var item = await db.Items.FindAsync(id);
            if (item is not null)
            {
                item.IsPurchased = !item.IsPurchased;
                await db.SaveChangesAsync();
            }
            Items = await db.Items.AsNoTracking().ToListAsync();
        }
    }
}
```

Notes:
- `FormName` + `method="post"` is required so the antiforgery token is emitted and the form can be bound. `UseAntiforgery()` middleware must be in the pipeline (it is in the template).
- Each item gets its own `<form>` because a plain HTML form submits everything — the hidden `ItemId` input selects which item's button was clicked.

#### a2) Static SSR + HTMX instead of a full-page POST

Since static SSR Blazor emits **plain HTML**, HTMX works exactly like in your Go blood-pressure app — no SignalR, no WASM, just HTTP. Replace the submit button with an `hx-post` to a minimal API that returns an HTML fragment:

```razor
<button hx-post="@($"/api/items/{item.Id}/toggle")"
        hx-target="closest li"
        hx-swap="outerHTML">✓</button>
```

```csharp
// Program.cs — minimal API returning a rendered fragment
app.MapPost("/api/items/{id:int}/toggle", async (int id, AppDbContext db) =>
    Results.Text($"<li class="{(toggled.IsPurchased ? "done" : "")}">{toggled.Name} …</li>",
        "text/html"));
```

Why you might prefer this over Interactive Server: zero runtime state on the server, works without JS beyond HTMX, scales statelessly like your Azure Functions mindset. Why you might not: you're back to writing manual HTML fragments / partial rendering, which is exactly the plumbing Blazor Interactive removes.

### b) Interactive Server — the circuit model

`@rendermode InteractiveServer`. After the initial SSR response, the browser establishes a **SignalR (WebSocket) connection** — the "circuit". The component instance now lives **in server memory**, and every `@onclick` / `@onchange` is a message over that socket, handled by your C# code running on the server. Blazor then re-renders only the DOM diff over the socket. No HTTP request, no full page reload, and your state (`Items`, even the `DbContext`) survives between interactions.

Lifecycle:
1. **Prerender pass**: component renders once on the server → `OnInitializedAsync` runs, HTML is sent (this is why it runs *twice* in dev — once for prerender, once when the circuit starts).
2. **Circuit activation**: component is instantiated *again* inside the circuit → `OnInitializedAsync` runs again → render → connection is live.
3. **Every event** (click, input, `@bind`): handler runs in the server process → automatic re-render of the diff → browser patches the DOM. `Items` in memory is already fresh — nothing to reload.
4. Circuit dies on disconnect/timeout → all state is gone.

```razor
@page "/items"
@rendermode InteractiveServer
@implements IAsyncDisposable
@using Microsoft.EntityFrameworkCore
@inject IDbContextFactory<AppDbContext> DbFactory

<h1>Shopping list</h1>
<ul>
@foreach (var item in Items)
{
    <li>
        <input type="checkbox" checked="@item.IsPurchased"
               @onchange="() => Toggle(item)" />
        @item.Name
    </li>
}
</ul>

@code {
    private List<ShoppingItem> Items = [];
    private AppDbContext db = default!;      // OK here: lives in the circuit, disposed below

    protected override async Task OnInitializedAsync()
    {
        db = DbFactory.CreateDbContext();
        Items = await db.Items.ToListAsync();   // tracked entities — SaveChanges just works
    }

    private async Task Toggle(ShoppingItem item)   // runs on the SERVER, over the socket
    {
        item.IsPurchased = !item.IsPurchased;
        await db.SaveChangesAsync();             // re-render happens automatically after
    }

    public async ValueTask DisposeAsync() => await db.DisposeAsync();
}
```

Notes:
- The handler mutates the tracked EF entity and saves — no reload, no redirect, no `Items = await …ToListAsync()`. Blazor re-renders automatically when the handler completes. That's the whole payoff of Interactive mode.
- Long-lived `DbContext` in a circuit is exactly why the factory pattern exists (see §6) — one circuit = one user session, not one request.

### c) Pure WASM — the component runs in the browser

`@rendermode InteractiveWebAssembly` (or `InteractiveAuto`, which is Server until the bundle is downloaded, then WASM). Structural difference: components that run in the browser **must live in a separate `.Client` project** (`dotnet new blazor -int WebAssembly` scaffolds this), and the server only serves the WASM bundle + an API.

The critical change: **there is no server-side circuit — and therefore no direct `DbContext`.** EF can't open a connection to your SQLite/SQL Server from inside the user's browser. All persistence goes through `HttpClient` calls to an API on the server project — you're back to a classic frontend/backend split, except the frontend is C#.

Lifecycle:
1. **Prerender** (if enabled — it is by default): the server renders the component once → `OnInitializedAsync` runs *on the server* (⚠️ `HttpClient` with relative URLs fails here — see note).
2. **Download & boot**: the browser downloads the WASM runtime + your DLLs, instantiates the component → `OnInitializedAsync` runs *in the browser* → you fetch data over HTTP.
3. **Every event**: handler runs **in the browser** — UI updates (including optimistic state changes) are instant with zero server involvement; only the actual persistence needs one HTTP call.
4. State lives in browser memory; a refresh kills it → refetch on load.

Client component (in `ShoppingList.Client/Pages/Items.razor`):

```razor
@page "/items"
@rendermode InteractiveWebAssembly
@using System.Net.Http.Json
@inject HttpClient Http

<h1>Shopping list</h1>
<ul>
@foreach (var item in Items)
{
    <li>
        <input type="checkbox" checked="@item.IsPurchased"
               @onchange="() => Toggle(item)" />
        @item.Name
    </li>
}
</ul>

@code {
    private List<ShoppingItem> Items = [];

    protected override async Task OnInitializedAsync()   // runs in the BROWSER
        => Items = await Http.GetFromJsonAsync<List<ShoppingItem>>("/api/items") ?? [];

    private async Task Toggle(ShoppingItem item)
    {
        item.IsPurchased = !item.IsPurchased;   // optimistic: instant UI update, no server round trip
        await Http.PatchAsJsonAsync($"/api/items/{item.Id}/toggle", default(object));
    }
}
```

Server project (`Program.cs`) — a normal ASP.NET API, the part that's most familiar to you:

```csharp
// Scoped DbContext is fine here: API endpoints are per-request, like your other apps
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

app.MapGet("/api/items", async (AppDbContext db) =>
    await db.Items.AsNoTracking().ToListAsync());

app.MapPatch("/api/items/{id:int}/toggle", async (int id, AppDbContext db) =>
{
    var item = await db.Items.FindAsync(id);
    if (item is null) return Results.NotFound();
    item.IsPurchased = !item.IsPurchased;
    await db.SaveChangesAsync();
    return Results.NoContent();
});
```

Notes:
- The `.Client` template registers `HttpClient` with the app's base address for you — don't re-register it with an absolute URL.
- **Prerendering gotcha**: with prerender enabled, `OnInitializedAsync` runs once on the server where `HttpClient` + relative URL works fine, then again in the browser. If you disable WASM prerendering (`prerender:false`), the user stares at a blank/`Loading...` page while the bundle downloads.
- `InteractiveAuto` = b) for the first visit(s), then automatically c) once the bundle is cached — nice default for a public app, overkill for a personal shopping list.

### Comparison at a glance

| | a) Static SSR (+HTMX) | b) Interactive Server | c) Interactive WASM |
|---|---|---|---|
| Toggle runs where | Server (HTTP POST per action) | Server (over one WebSocket) | **Browser** |
| UI update | Full page reload (or HTMX swap) | DOM diff over socket | Local re-render, zero server involvement |
| State lives | Nowhere (per request) | Server memory (circuit) | Browser memory |
| DB access | Direct EF | Direct EF | Only via HTTP API |
| Server footprint | Stateless — scales like Azure Functions | Stateful per connected user (memory, circuit) | Stateless except the API |
| Code surface | Razor-Pages-like + HTMX attributes | Pure C#, no JS | Pure C# + an API controller layer |

For the shopping list: start with **b)** to learn the Blazor-native model, then re-implement the toggle as **a)** once to feel the difference — it's the fastest way to understand what each render mode actually costs you.

## 13. Shopping list app — suggested build plan

**Goal:** master Blazor by doing CRUD + the interactive stuff Razor Pages can't do.

### Step 0 — scaffold (or use your reading-list flake.nix as base)

```bash
dotnet new blazor -n ShoppingList -ai false        # Blazor Web App, Interactive Server only
cd ShoppingList
dotnet tool install -g dotnet-ef                   # if not already
```

`Program.cs` essentials (you already know this shape from the reading list):

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDbContextFactory<AppDbContext>(o =>
    o.UseSqlite(config.GetConnectionString("Default")));

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();   // global default: every page Interactive Server
```

### Model & persistence

```csharp
public class ShoppingItem
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Range(1, 100)] public int Quantity { get; set; } = 1;
    public bool IsPurchased { get; set; }
    public int ShoppingListId { get; set; }
    public ShoppingList? ShoppingList { get; set; }
}

public class ShoppingList
{
    public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
    public List<ShoppingItem> Items { get; set; } = [];
}
```

`dotnet ef migrations add Init && dotnet ef database update` — same as your other apps.

### Build order (each step teaches one Blazor concept)

1. **Index page, static SSR, QuickGrid** — lists lists. Review: routing, layouts, QuickGrid (you've seen it in the reading list).
2. **Create page, Interactive Server, `EditForm` + validation** — review: lifecycle, binding, `NavigationManager` redirect.
3. **Edit page with route param** — learn: `OnParametersSetAsync` refresh, `NotFound()` handling.
4. **Extract `ItemRow.razor` / `ItemForm.razor` components** — learn: `[Parameter]`, `EventCallback<T>`, component composition (the core skill Razor Pages never gave you).
5. **The interactive payoff: an items grid with checkboxes + inline add row that never reloads** — this is the "full stack single round-trip feel" that required HTMX in your Go app. Blazor gives it to you in C#.
6. **Stretch goals:** `InteractiveAuto` + WASM to compare; auth via `@attribute [Authorize]`; service layer (`ItemsService`) instead of EF-in-component, to mirror how you'd structure an Azure Functions app.

### Definition of done

- Add/remove/rename/check-off items with no page reloads
- Validation errors inline, no JS written by you
- Each page ≤ ~150 lines or refactored into child components
- You can explain to a colleague why you chose `InteractiveServer` over `Auto`/`WASM` for this app (server holds state, small user base, no offline requirement)

## 14. Reference

- Official docs home: https://learn.microsoft.com/aspnet/core/blazor/
- Render modes: https://learn.microsoft.com/aspnet/core/blazor/components/render-modes
- Forms & validation: https://learn.microsoft.com/aspnet/core/blazor/forms/input-components
- QuickGrid demo: https://aspnet.github.io/quickgridsamples/
- EF + Blazor guidance (the factory pattern): https://learn.microsoft.com/ef/core/dbcontext-configuration/#avoiding-dbcontext-threading-issues — plus the "Blazor Server + EF Core" doc section
- Your best reference: `~/code/dotnet-blazor-reading-list` — it already demonstrates most of the patterns above (factory, QuickGrid, SupplyParameterFromForm, NotFound)
