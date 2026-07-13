# Blazor Render Modes & Event Callbacks Guide

## Quick Comparison: InteractiveServer vs InteractiveWebAssembly

### **InteractiveServer Callbacks**
```razor
@page "/inventory/categories"
@rendermode InteractiveServer
@using AutoPartShop.Web.Services

<TestPage OnMessageReceived="HandleMessageFromChild" />

@code {
    private string ErrorMessage = string.Empty;

    private async Task HandleMessageFromChild(string message)
    {
        ErrorMessage = message;
        StateHasChanged();  // Needed to trigger re-render
    }
}
```

**Event Flow:**
1. User clicks button in browser
2. JavaScript intercepts click event
3. JavaScript sends message to SERVER via WebSocket
4. C# code on SERVER runs HandleMessageFromChild()
5. Server updates ErrorMessage variable
6. Server calls StateHasChanged() to mark component dirty
7. Server re-renders component
8. Server sends HTML diff to browser
9. JavaScript applies patch to DOM
10. User sees update (slight delay due to network)

**Latency:** ~50-200ms (network roundtrip)
**Code Execution Location:** SERVER
**State Location:** SERVER

---

### **InteractiveWebAssembly Callbacks**
```razor
@page "/inventory/categories"
@rendermode InteractiveWebAssembly
@using AutoPartShop.Web.Services

<TestPage OnMessageReceived="HandleMessageFromChild" />

@code {
    private string ErrorMessage = string.Empty;

    private async Task HandleMessageFromChild(string message)
    {
        ErrorMessage = message;
        // StateHasChanged() not needed - WebAssembly auto re-renders
    }
}
```

**Event Flow:**
1. User clicks button in browser
2. .NET runtime (running in browser via WebAssembly) intercepts click
3. C# code in BROWSER runs HandleMessageFromChild() immediately
4. Browser updates ErrorMessage variable
5. Browser component automatically re-renders (no StateHasChanged needed)
6. JavaScript updates DOM
7. User sees update instantly (no network delay)

**Latency:** ~0-5ms (instant)
**Code Execution Location:** BROWSER
**State Location:** BROWSER

---

## Side-by-Side Comparison

| Aspect | InteractiveServer | InteractiveWebAssembly |
|--------|-------------------|------------------------|
| **Code Execution** | Server | Browser |
| **Event Handling** | Roundtrip via WebSocket | Instant in browser |
| **Latency** | 50-200ms | 0-5ms |
| **StateHasChanged()** | ✅ Required | ❌ Not needed |
| **Network Required** | ✅ Always | ❌ Only for API calls |
| **Works Offline** | ❌ No | ✅ Yes |
| **Server CPU** | ⬆️ Higher | ⬇️ Lower |
| **Browser Download** | ⬇️ Small | ⬆️ Large (~2MB .NET runtime) |
| **Connection Loss** | ❌ App stops | ✅ Works locally |

---

## Real Code Examples

### Example 1: Simple Counter (Both Modes)

**InteractiveServer:**
```razor
@page "/counter"
@rendermode InteractiveServer

<h1>Counter (Server)</h1>
<p>Count: @count</p>
<button @onclick="Increment">Click Me</button>

@code {
    private int count = 0;

    private void Increment()
    {
        count++;
        // StateHasChanged called automatically by InteractiveServer
    }
}
```

**InteractiveWebAssembly:**
```razor
@page "/counter"
@rendermode InteractiveWebAssembly

<h1>Counter (WebAssembly)</h1>
<p>Count: @count</p>
<button @onclick="Increment">Click Me</button>

@code {
    private int count = 0;

    private void Increment()
    {
        count++;
        // Auto re-renders - no StateHasChanged needed
    }
}
```

**Result:** Identical behavior, but server version has ~100ms delay, WebAssembly is instant.

---

### Example 2: Parent-Child Event Callbacks (Both Modes)

**Parent Component (Either Mode):**
```razor
@rendermode InteractiveServer  <!-- or InteractiveWebAssembly -->

<ChildComponent OnValueChanged="HandleValueChanged" />
<p>Received from child: @receivedValue</p>

@code {
    private string receivedValue = "";

    private async Task HandleValueChanged(string value)
    {
        receivedValue = value;
        StateHasChanged();  // Required for InteractiveServer only
    }
}
```

**Child Component (Same for Both):**
```razor
<button @onclick="SendValue">Send to Parent</button>

@code {
    [Parameter]
    public EventCallback<string> OnValueChanged { get; set; }

    private async Task SendValue()
    {
        await OnValueChanged.InvokeAsync("Hello from child!");
    }
}
```

**Key Point:** The callback code is IDENTICAL for both modes. Only the execution location changes.

---

## When to Use Each Mode

### Use **InteractiveServer** When:
- ✅ You need server-side logic (database access, authentication, etc.)
- ✅ You have sensitive data (don't want code exposed in browser)
- ✅ You need server resources (file operations, background jobs)
- ✅ Users have good internet connection
- ✅ Your app is for internal/admin use
- ✅ Initial page load speed is critical
- **Example:** Admin panels, business dashboards, CRM systems

### Use **InteractiveWebAssembly** When:
- ✅ You want instant UI responsiveness
- ✅ You need offline support
- ✅ Logic is purely client-side (math, validations, UI state)
- ✅ You want to reduce server load
- ✅ Network latency is a problem (slow internet users)
- ✅ You're building a desktop-like experience
- **Example:** Drawing apps, rich text editors, calculators, design tools

### Use **InteractiveAuto** When:
- ✅ You want the best of both worlds
- ✅ Best for most modern applications
- ✅ Fast initial load + instant interactions after WebAssembly loads

---

## Your Project: Categories Page

Your current setup:
```razor
@page "/inventory/categories"
@rendermode InteractiveServer
```

**This is correct because:**
- ✅ You're fetching categories from database (server-side)
- ✅ You're deleting/editing categories (server-side operations)
- ✅ You need authentication (server-side)
- ✅ This is an admin panel (internal use)

**Could you use WebAssembly?**
Only if you:
- Move all category logic to client-side (not recommended for database operations)
- Call API endpoints instead of injecting services
- Accept the larger download size

---

## Important: StateHasChanged() Behavior

### InteractiveServer
```csharp
private async Task HandleMessageFromChild(string Message)
{
    ErrorMessage = Message;
    StateHasChanged();  // ✅ REQUIRED - tells server to re-render
}
```

**Why?** Server doesn't know when to re-render without being told.

### InteractiveWebAssembly
```csharp
private async Task HandleMessageFromChild(string Message)
{
    ErrorMessage = Message;
    // ❌ StateHasChanged() not needed - automatically re-renders
}
```

**Why?** WebAssembly detects state changes automatically and re-renders.

### InteractiveAuto
```csharp
private async Task HandleMessageFromChild(string Message)
{
    ErrorMessage = Message;
    // ✅ Include StateHasChanged() - safe for both modes
}
```

**Why?** Still works in Server mode, no harm in WebAssembly mode.

---

## Debugging Callbacks in Each Mode

### InteractiveServer Debug:
```csharp
private async Task HandleMessageFromChild(string Message)
{
    Console.WriteLine($"[Server] Callback received: {Message}");  // Check server console
    Logger.LogInformation($"Message: {Message}");  // Check application logs

    ErrorMessage = Message;
    StateHasChanged();
}
```

**Check:** Server console / Application event viewer

### InteractiveWebAssembly Debug:
```csharp
private async Task HandleMessageFromChild(string Message)
{
    Console.WriteLine($"[Browser] Callback received: {Message}");  // Check browser console

    ErrorMessage = Message;
    // Auto re-renders
}
```

**Check:** Browser DevTools Console (F12 → Console tab)

---

## Performance Comparison

### Initial Page Load
- **Server:** Fast (30-100ms) - just HTML
- **WebAssembly:** Slow (2-5 seconds) - downloads .NET runtime, assemblies, app
- **Winner:** Server ✅

### User Interaction (Button Click)
- **Server:** Medium (50-200ms) - network roundtrip + server processing
- **WebAssembly:** Instant (0-5ms) - runs in browser immediately
- **Winner:** WebAssembly ✅

### Server Resource Usage
- **Server:** High - processes every click
- **WebAssembly:** Low - server only needed for API calls
- **Winner:** WebAssembly ✅

### User Experience
- **Server:** Noticeable delay on clicks
- **WebAssembly:** Snappy, responsive
- **Winner:** WebAssembly ✅

---

## Conclusion

**Event callbacks work IDENTICALLY in both modes** - the code is the same:
- Parameter definition: `public EventCallback<T> OnCallback { get; set; }`
- Invoking: `await OnCallback.InvokeAsync(value)`
- Handling: `private async Task HandleCallback(T value) { ... }`

**The only differences are:**
1. Where the code executes (server vs browser)
2. Whether you need `StateHasChanged()` (required in Server, optional in WebAssembly)
3. Latency (Server: 50-200ms, WebAssembly: 0-5ms)

Choose the mode that fits your app's needs!
