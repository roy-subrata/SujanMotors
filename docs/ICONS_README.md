# PWA Icons Setup

## Required Icons

You need to generate the following icon sizes for the PWA to work properly:

### Standard Icons (Required)
- `icon-72x72.png`
- `icon-96x96.png`
- `icon-128x128.png`
- `icon-144x144.png`
- `icon-152x152.png`
- `icon-192x192.png`
- `icon-384x384.png`
- `icon-512x512.png`

### Additional Icons (Recommended)
- `icon-16x16.png` - Browser favicon
- `icon-32x32.png` - Browser favicon
- `apple-touch-icon.png` - 180x180px for iOS
- `safari-pinned-tab.svg` - SVG for Safari

### iOS Splash Screens (Optional but Recommended)
- `apple-splash-640-1136.png` - iPhone 5
- `apple-splash-750-1334.png` - iPhone 6/7/8
- `apple-splash-1125-2436.png` - iPhone X/XS
- `apple-splash-1242-2688.png` - iPhone XS Max
- `apple-splash-1536-2048.png` - iPad
- `apple-splash-1668-2388.png` - iPad Pro 11"
- `apple-splash-2048-2732.png` - iPad Pro 12.9"

### Screenshots (For App Store listing)
- `screenshot-wide.png` - 1280x720px (desktop)
- `screenshot-narrow.png` - 720x1280px (mobile)

## How to Generate Icons

### Option 1: Use the included SVG
Open `icon.svg` in this folder and export it to different sizes using:
- Adobe Illustrator
- Inkscape (free)
- Figma

### Option 2: Online Tools (Recommended)
1. **PWA Asset Generator**: https://www.pwabuilder.com/imageGenerator
   - Upload your logo/icon
   - It generates all required sizes automatically

2. **RealFaviconGenerator**: https://realfavicongenerator.net/
   - Comprehensive favicon and PWA icon generator
   - Generates iOS splash screens too

3. **Maskable.app**: https://maskable.app/
   - Test and create maskable icons for Android

### Option 3: CLI Tool
```bash
npm install -g pwa-asset-generator
pwa-asset-generator icon.svg ./icons --background "#667eea" --padding "10%"
```

## Testing Your PWA

1. Build in production mode: `ng build --configuration production`
2. Serve the dist folder with a local server: `npx http-server dist/smauto/browser`
3. Open Chrome DevTools > Application > Manifest to verify
4. Use Lighthouse to audit your PWA

## Troubleshooting

- Icons must be PNG format (not SVG) in manifest.webmanifest
- Make sure all paths in manifest.webmanifest are correct
- Service worker only works over HTTPS or localhost
- Clear browser cache when testing changes
