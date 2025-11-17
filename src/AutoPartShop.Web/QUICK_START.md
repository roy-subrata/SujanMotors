# Quick Start Guide - AutoParts Shop Web

## 🚀 Get Running in 2 Minutes

### Prerequisites
- Node.js 16+ installed
- .NET 9.0 SDK installed
- Visual Studio Code or Visual Studio 2022

### Step 1: Install Dependencies (1 minute)

Open PowerShell in the web project directory:

```powershell
cd src/AutoPartShop.Web
npm install
```

### Step 2: Build Tailwind CSS (30 seconds)

```powershell
npm run tailwind:build
```

### Step 3: Run the Application

**Option A: With Hot-Reload (Recommended)**

Open two PowerShell windows:

**Window 1** - Watch Tailwind CSS:
```powershell
npm run dev
```

**Window 2** - Run Blazor:
```powershell
dotnet run
```

**Option B: Just Run It**
```powershell
dotnet run
```

### Step 4: Open in Browser

Visit: **https://localhost:7020**

---

## ✅ What You Should See

1. **Header** with logo, search, notifications, and user profile
2. **Sidebar** (hidden on mobile) with collapsible menus:
   - Dashboard
   - Inventory
   - Sales & Orders
   - Customers
   - Reports
   - Settings
3. **Dashboard Page** with:
   - 4 metric cards (Sales, Orders, Inventory, Customers)
   - Sales trend chart
   - Top products list
   - Recent orders table
   - Quick stat cards

---

## 📁 Key Files

| File | Purpose |
|------|---------|
| `Components/Layout/MainLayout.razor` | Main page structure |
| `Components/Layout/NavMenu.razor` | Sidebar navigation |
| `Components/Pages/Home.razor` | Dashboard page |
| `Styles/input.css` | Tailwind CSS configuration |
| `tailwind.config.js` | Color & theme settings |
| `package.json` | NPM scripts & dependencies |

---

## 🎨 Colors

**Primary (Blue)**: Used for buttons, links, highlights
**Dark (Gray)**: Used for text, backgrounds, borders

Edit colors in `tailwind.config.js` → `theme.extend.colors`

---

## 🔧 Common Tasks

### Add a New Page

1. Create file: `Components/Pages/YourPage.razor`
2. Add route: `@page "/your-route"`
3. Use existing cards and components

### Change Sidebar Menu

Edit: `Components/Layout/NavMenu.razor`

Look for:
```html
<NavLink href="/inventory/parts" class="nav-menu-item">
  <div class="flex items-center space-x-3">
    <svg>...</svg>
    <span>Menu Item</span>
  </div>
</NavLink>
```

### Modify Colors

Edit: `tailwind.config.js`
```javascript
primary: {
  600: '#your-new-color',
}
```

Then rebuild: `npm run tailwind:build`

### Add Custom Button Style

Edit: `Styles/input.css`
```css
@layer components {
  .btn-custom {
    @apply px-4 py-2 bg-blue-500 text-white rounded-lg;
  }
}
```

---

## 📖 Documentation

- **TAILWIND_SETUP.md** - Full setup guide
- **COMPONENT_GUIDE.md** - Component examples
- **EXAMPLE_PAGES.md** - Ready-to-use page templates
- **README_INTEGRATION.md** - Architecture overview

---

## 🐛 Troubleshooting

### "Tailwind CSS not loading"
```bash
npm run tailwind:build
# Clear browser cache: Ctrl+Shift+Delete
```

### "Classes not working"
1. Check filename is in `tailwind.config.js` content paths
2. Rebuild CSS: `npm run tailwind:build`
3. Restart `dotnet run`

### "Sidebar not showing"
- On mobile? It's hidden by default (click hamburger menu)
- Check `MainLayout.razor` for the sidebar div

### "npm not found"
- Install Node.js from nodejs.org
- Restart PowerShell/Terminal

---

## 🎯 Next Steps

1. ✅ Run the app (you're here!)
2. 📝 Create your first page using `EXAMPLE_PAGES.md`
3. 🔌 Connect API endpoints
4. 📊 Add charts (Chart.js, Plotly)
5. 🔐 Add authentication
6. 🎨 Customize colors & branding

---

## 💡 Tips

- Use the **Component Guide** for copy-paste code
- All utilities are in **Styles/input.css**
- Tailwind docs: https://tailwindcss.com/docs
- Watch for file changes: `npm run dev` in separate terminal

---

## 📞 Need Help?

Check these files in order:
1. `COMPONENT_GUIDE.md` - Common components
2. `EXAMPLE_PAGES.md` - Full page examples
3. `TAILWIND_SETUP.md` - Setup issues
4. `README_INTEGRATION.md` - Architecture

---

**Ready? Let's go!** 🚀

```bash
cd src/AutoPartShop.Web
npm install
npm run dev
# In another terminal:
dotnet run
# Visit: https://localhost:7020
```

Your modern, enterprise-grade AutoParts inventory system is ready to build! 🎉
