# Quick Reference: Blazor Render Modes

## One-Line Summary
**Every Razor page needs `@rendermode` to be interactive!**

---

## Template

### For Your Project (Admin/Internal App):
```razor
@page "/your-page-path"
@rendermode InteractiveServer
@using YourNamespace
@inject YourService Service

<PageTitle>Page Title</PageTitle>

<!-- Your HTML here -->

@code {
    // Your C# code here
}
```

---

## Three Render Modes

### 1. Static (No Render Mode)
```razor
@page "/page"
<!-- No @rendermode means STATIC - no interactivity -->
```
- ✅ Fastest initial load
- ❌ No buttons, forms, or state changes work
- ❌ Event callbacks don't fire

### 2. InteractiveServer
```razor
@page "/page"
@rendermode InteractiveServer
```
- ✅ Full interactivity
- ✅ Best for database apps
- ✅ Works offline initially
- ❌ Slight delay on user actions (50-200ms)
- **Use for:** Admin panels, forms, CRUD operations

### 3. InteractiveWebAssembly
```razor
@page "/page"
@rendermode InteractiveWebAssembly
```
- ✅ Instant responsiveness
- ✅ Works offline
- ✅ Reduces server load
- ❌ Requires API calls (can't inject services)
- ❌ Larger download (~2MB)
- **Use for:** Rich UI, desktop-like apps

### 4. InteractiveAuto (Best Choice)
```razor
@page "/page"
@rendermode InteractiveAuto
```
- ✅ Fast initial load + instant interactions
- ✅ Best of both worlds
- ✅ Recommended for new projects

---

## Event Callbacks (The Most Important!)

### Parent Component:
```razor
@rendermode InteractiveServer

<ChildComponent OnValueChanged="HandleValueChanged" />

@code {
    private async Task HandleValueChanged(string value)
    {
        // This now works because page is interactive!
    }
}
```

### Child Component:
```razor
<button @onclick="SendValue">Send</button>

@code {
    [Parameter]
    public EventCallback<string> OnValueChanged { get; set; }

    private async Task SendValue()
    {
        await OnValueChanged.InvokeAsync("data");
    }
}
```

---

## Common Mistakes

### ❌ WRONG:
```razor
@page "/inventory/categories"
<button @onclick="MyMethod">Click Me</button>  <!-- Won't work! -->

@code {
    private void MyMethod() { }
}
```

### ✅ CORRECT:
```razor
@page "/inventory/categories"
@rendermode InteractiveServer
<button @onclick="MyMethod">Click Me</button>  <!-- Works! -->

@code {
    private void MyMethod() { }
}
```

---

## Your Application

Your app uses:
```csharp
// Program.cs
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();  // ← InteractiveServer
```

So use: **`@rendermode InteractiveServer`** ✅

---

## Quick Checklist

When creating a new page:
- [ ] Add `@page` directive
- [ ] Add `@rendermode InteractiveServer` (for your app)
- [ ] Add necessary `@using` statements
- [ ] Add necessary `@inject` statements
- [ ] Test that buttons/forms work

---

## StateHasChanged() Rule

### InteractiveServer:
```csharp
private async Task HandleCallback(string value)
{
    MyProperty = value;
    StateHasChanged();  // ✅ REQUIRED
}
```

### InteractiveWebAssembly:
```csharp
private async Task HandleCallback(string value)
{
    MyProperty = value;
    // ❌ NOT needed - auto re-renders
}
```

### InteractiveAuto:
```csharp
private async Task HandleCallback(string value)
{
    MyProperty = value;
    StateHasChanged();  // ✅ Safe to include
}
```

---

## All Pages in Your App

Status: ✅ **ALL 70 PAGES FIXED** (as of latest update)

Files with `@rendermode InteractiveServer`:
- All pages in `Components/Pages/Inventory/`
- All pages in `Components/Pages/Procurement/`
- All pages in `Components/Pages/`

---

## Need More Info?

- `BLAZOR_RENDERMODE_GUIDE.md` - Full render mode guide
- `CALLBACK_FLOW_COMPARISON.md` - How callbacks execute
- `RENDERMODE_FIX_SUMMARY.md` - What was fixed and why
