import { Injectable } from '@angular/core';
import { Observable, of, delay } from 'rxjs';
import {
  CatalogLandingResponse,
  CatalogFilterResponse,
  CatalogProductListItem,
  CatalogProductDetail,
  CatalogSearchRequest,
  CatalogCategory,
} from '../models/catalog.model';

const CATEGORIES: CatalogCategory[] = [
  { id: 'cat-1', name: 'Engine Parts', parentCategoryId: null, depthLevel: 0, displayOrder: 1, childCount: 2 },
  { id: 'cat-1-1', name: 'Gaskets', parentCategoryId: 'cat-1', depthLevel: 1, displayOrder: 1, childCount: 0 },
  { id: 'cat-1-2', name: 'Sensors', parentCategoryId: 'cat-1', depthLevel: 1, displayOrder: 2, childCount: 0 },

  { id: 'cat-2', name: 'Brake System', parentCategoryId: null, depthLevel: 0, displayOrder: 2, childCount: 2 },
  { id: 'cat-2-1', name: 'Brake Pads', parentCategoryId: 'cat-2', depthLevel: 1, displayOrder: 1, childCount: 0 },
  { id: 'cat-2-2', name: 'Brake Rotors', parentCategoryId: 'cat-2', depthLevel: 1, displayOrder: 2, childCount: 0 },

  { id: 'cat-3', name: 'Suspension', parentCategoryId: null, depthLevel: 0, displayOrder: 3, childCount: 2 },
  { id: 'cat-3-1', name: 'Shocks & Struts', parentCategoryId: 'cat-3', depthLevel: 1, displayOrder: 1, childCount: 0 },
  { id: 'cat-3-2', name: 'Control Arms', parentCategoryId: 'cat-3', depthLevel: 1, displayOrder: 2, childCount: 0 },

  { id: 'cat-4', name: 'Electrical', parentCategoryId: null, depthLevel: 0, displayOrder: 4, childCount: 2 },
  { id: 'cat-4-1', name: 'Batteries', parentCategoryId: 'cat-4', depthLevel: 1, displayOrder: 1, childCount: 0 },
  { id: 'cat-4-2', name: 'Lighting', parentCategoryId: 'cat-4', depthLevel: 1, displayOrder: 2, childCount: 0 },

  { id: 'cat-5', name: 'Body Parts', parentCategoryId: null, depthLevel: 0, displayOrder: 5, childCount: 2 },
  { id: 'cat-5-1', name: 'Mirrors', parentCategoryId: 'cat-5', depthLevel: 1, displayOrder: 1, childCount: 0 },
  { id: 'cat-5-2', name: 'Bumpers', parentCategoryId: 'cat-5', depthLevel: 1, displayOrder: 2, childCount: 0 },

  { id: 'cat-6', name: 'Oils & Fluids', parentCategoryId: null, depthLevel: 0, displayOrder: 6, childCount: 2 },
  { id: 'cat-6-1', name: 'Engine Oil', parentCategoryId: 'cat-6', depthLevel: 1, displayOrder: 1, childCount: 0 },
  { id: 'cat-6-2', name: 'Coolant', parentCategoryId: 'cat-6', depthLevel: 1, displayOrder: 2, childCount: 0 },

  { id: 'cat-7', name: 'Filters', parentCategoryId: null, depthLevel: 0, displayOrder: 7, childCount: 2 },
  { id: 'cat-7-1', name: 'Oil Filters', parentCategoryId: 'cat-7', depthLevel: 1, displayOrder: 1, childCount: 0 },
  { id: 'cat-7-2', name: 'Air Filters', parentCategoryId: 'cat-7', depthLevel: 1, displayOrder: 2, childCount: 0 },

  { id: 'cat-8', name: 'Tires & Wheels', parentCategoryId: null, depthLevel: 0, displayOrder: 8, childCount: 2 },
  { id: 'cat-8-1', name: 'Tires', parentCategoryId: 'cat-8', depthLevel: 1, displayOrder: 1, childCount: 0 },
  { id: 'cat-8-2', name: 'Alloy Wheels', parentCategoryId: 'cat-8', depthLevel: 1, displayOrder: 2, childCount: 0 },
];

const BRANDS = ['Bosch', 'Denso', 'NGK', 'Brembo', 'Hella', 'Monroe', 'Mann', 'Castrol', 'Mobil', 'Valvoline', 'Continental', 'ACDelco'];

const CATEGORY_FILTERS: Record<string, CatalogFilterResponse['filters']> = {
  'cat-1': [
    {
      attributeId: 'attr-brand',
      name: 'Brand',
      filterType: 'select',
      sortOrder: 1,
      options: BRANDS.slice(0, 6).map(b => ({ value: b, count: Math.floor(Math.random() * 10) + 1 })),
      min: null,
      max: null,
      unit: null,
    },
    {
      attributeId: 'attr-engine-type',
      name: 'Engine Type',
      filterType: 'select',
      sortOrder: 2,
      options: [
        { value: 'Inline-4', count: 12 },
        { value: 'V6', count: 8 },
        { value: 'V8', count: 5 },
      ],
      min: null,
      max: null,
      unit: null,
    },
    {
      attributeId: 'attr-position',
      name: 'Position',
      filterType: 'select',
      sortOrder: 3,
      options: [
        { value: 'Front', count: 14 },
        { value: 'Rear', count: 7 },
      ],
      min: null,
      max: null,
      unit: null,
    },
  ],
  'cat-2': [
    {
      attributeId: 'attr-brand',
      name: 'Brand',
      filterType: 'select',
      sortOrder: 1,
      options: BRANDS.slice(2, 8).map(b => ({ value: b, count: Math.floor(Math.random() * 10) + 1 })),
      min: null,
      max: null,
      unit: null,
    },
    {
      attributeId: 'attr-brake-type',
      name: 'Brake Type',
      filterType: 'select',
      sortOrder: 2,
      options: [
        { value: 'Disc', count: 18 },
        { value: 'Drum', count: 6 },
      ],
      min: null,
      max: null,
      unit: null,
    },
    {
      attributeId: 'attr-axle',
      name: 'Axle',
      filterType: 'select',
      sortOrder: 3,
      options: [
        { value: 'Front', count: 10 },
        { value: 'Rear', count: 9 },
      ],
      min: null,
      max: null,
      unit: null,
    },
  ],
  'cat-3': [
    {
      attributeId: 'attr-brand',
      name: 'Brand',
      filterType: 'select',
      sortOrder: 1,
      options: BRANDS.slice(1, 7).map(b => ({ value: b, count: Math.floor(Math.random() * 10) + 1 })),
      min: null,
      max: null,
      unit: null,
    },
    {
      attributeId: 'attr-position',
      name: 'Position',
      filterType: 'select',
      sortOrder: 2,
      options: [
        { value: 'Front', count: 11 },
        { value: 'Rear', count: 6 },
      ],
      min: null,
      max: null,
      unit: null,
    },
    {
      attributeId: 'attr-material',
      name: 'Material',
      filterType: 'select',
      sortOrder: 3,
      options: [
        { value: 'Steel', count: 9 },
        { value: 'Aluminum', count: 7 },
        { value: 'Composite', count: 5 },
      ],
      min: null,
      max: null,
      unit: null,
    },
  ],
  'cat-4': [
    {
      attributeId: 'attr-brand',
      name: 'Brand',
      filterType: 'select',
      sortOrder: 1,
      options: BRANDS.slice(0, 5).map(b => ({ value: b, count: Math.floor(Math.random() * 10) + 1 })),
      min: null,
      max: null,
      unit: null,
    },
    {
      attributeId: 'attr-voltage',
      name: 'Voltage',
      filterType: 'select',
      sortOrder: 2,
      options: [
        { value: '12V', count: 20 },
        { value: '24V', count: 6 },
      ],
      min: null,
      max: null,
      unit: null,
    },
    {
      attributeId: 'attr-condition',
      name: 'Condition',
      filterType: 'select',
      sortOrder: 3,
      options: [
        { value: 'New', count: 18 },
        { value: 'Refurbished', count: 4 },
      ],
      min: null,
      max: null,
      unit: null,
    },
  ],
  'cat-5': [
    {
      attributeId: 'attr-brand',
      name: 'Brand',
      filterType: 'select',
      sortOrder: 1,
      options: BRANDS.slice(3, 9).map(b => ({ value: b, count: Math.floor(Math.random() * 10) + 1 })),
      min: null,
      max: null,
      unit: null,
    },
    {
      attributeId: 'attr-color',
      name: 'Color',
      filterType: 'select',
      sortOrder: 2,
      options: [
        { value: 'Black', count: 9 },
        { value: 'Silver', count: 7 },
        { value: 'Primed', count: 6 },
      ],
      min: null,
      max: null,
      unit: null,
    },
    {
      attributeId: 'attr-fitment',
      name: 'Fitment',
      filterType: 'select',
      sortOrder: 3,
      options: [
        { value: 'OEM', count: 10 },
        { value: 'Aftermarket', count: 8 },
      ],
      min: null,
      max: null,
      unit: null,
    },
  ],
  'cat-6': [
    {
      attributeId: 'attr-brand',
      name: 'Brand',
      filterType: 'select',
      sortOrder: 1,
      options: BRANDS.slice(6, 12).map(b => ({ value: b, count: Math.floor(Math.random() * 10) + 1 })),
      min: null,
      max: null,
      unit: null,
    },
    {
      attributeId: 'attr-viscosity',
      name: 'Viscosity',
      filterType: 'select',
      sortOrder: 2,
      options: [
        { value: '0W-20', count: 6 },
        { value: '5W-30', count: 12 },
        { value: '10W-40', count: 8 },
      ],
      min: null,
      max: null,
      unit: null,
    },
    {
      attributeId: 'attr-volume',
      name: 'Volume',
      filterType: 'select',
      sortOrder: 3,
      options: [
        { value: '1L', count: 8 },
        { value: '4L', count: 10 },
        { value: '5L', count: 6 },
      ],
      min: null,
      max: null,
      unit: null,
    },
  ],
  'cat-7': [
    {
      attributeId: 'attr-brand',
      name: 'Brand',
      filterType: 'select',
      sortOrder: 1,
      options: BRANDS.slice(0, 6).map(b => ({ value: b, count: Math.floor(Math.random() * 10) + 1 })),
      min: null,
      max: null,
      unit: null,
    },
    {
      attributeId: 'attr-filter-type',
      name: 'Filter Type',
      filterType: 'select',
      sortOrder: 2,
      options: [
        { value: 'Oil', count: 9 },
        { value: 'Air', count: 8 },
        { value: 'Fuel', count: 6 },
        { value: 'Cabin', count: 7 },
      ],
      min: null,
      max: null,
      unit: null,
    },
    {
      attributeId: 'attr-size',
      name: 'Size',
      filterType: 'select',
      sortOrder: 3,
      options: [
        { value: 'Small', count: 6 },
        { value: 'Medium', count: 9 },
        { value: 'Large', count: 5 },
      ],
      min: null,
      max: null,
      unit: null,
    },
  ],
  'cat-8': [
    {
      attributeId: 'attr-brand',
      name: 'Brand',
      filterType: 'select',
      sortOrder: 1,
      options: BRANDS.slice(4, 10).map(b => ({ value: b, count: Math.floor(Math.random() * 10) + 1 })),
      min: null,
      max: null,
      unit: null,
    },
    {
      attributeId: 'attr-wheel-size',
      name: 'Wheel Size',
      filterType: 'select',
      sortOrder: 2,
      options: [
        { value: '15"', count: 6 },
        { value: '16"', count: 8 },
        { value: '17"', count: 9 },
        { value: '18"', count: 5 },
      ],
      min: null,
      max: null,
      unit: null,
    },
    {
      attributeId: 'attr-tire-type',
      name: 'Tire Type',
      filterType: 'select',
      sortOrder: 3,
      options: [
        { value: 'All Season', count: 10 },
        { value: 'Winter', count: 4 },
        { value: 'Performance', count: 6 },
      ],
      min: null,
      max: null,
      unit: null,
    },
  ],
};

function mockProduct(id: number, category: string, categoryId: string, brand: string, featured = false): CatalogProductListItem {
  const names: Record<string, string[]> = {
    'Engine Parts': ['Timing Belt Kit', 'Piston Ring Set', 'Cylinder Head Gasket', 'Fuel Injector', 'Camshaft Sensor', 'Oil Pump', 'Water Pump Assembly', 'Turbocharger'],
    'Brake System': ['Brake Pad Set Front', 'Brake Disc Rotor', 'Brake Caliper', 'Brake Master Cylinder', 'Brake Fluid DOT4', 'Parking Brake Cable'],
    'Suspension': ['Shock Absorber Front', 'Coil Spring', 'Control Arm Lower', 'Ball Joint', 'Stabilizer Link', 'Strut Mount'],
    'Electrical': ['Alternator Assembly', 'Starter Motor', 'Ignition Coil', 'Spark Plug Set', 'Battery 12V 60Ah', 'LED Headlight Bulb', 'Wiring Harness'],
    'Body Parts': ['Side Mirror Left', 'Bumper Cover Front', 'Fender Panel', 'Hood Latch', 'Door Handle', 'Windshield Wiper Blade'],
    'Oils & Fluids': ['Engine Oil 5W-30 4L', 'Transmission Fluid', 'Coolant Concentrate', 'Power Steering Fluid', 'Brake Cleaner Spray'],
    'Filters': ['Oil Filter', 'Air Filter', 'Fuel Filter', 'Cabin Air Filter', 'Transmission Filter'],
    'Tires & Wheels': ['Alloy Wheel 17"', 'All Season Tire 205/55R16', 'Wheel Bearing Kit', 'Lug Nut Set'],
  };
  const catNames = names[category] || names['Engine Parts'];
  const name = `${brand} ${catNames[id % catNames.length]}`;
  const price = Math.floor(Math.random() * 15000) + 500;
  const imageSeed = `${categoryId}-${id}`;
  const primaryImageUrl = `https://picsum.photos/seed/${imageSeed}/640/480`;

  return {
    partId: `part-${id}`,
    variantId: null,
    name,
    categoryName: category,
    brandName: brand,
    price,
    currency: 'BDT',
    inStock: Math.random() > 0.15,
    slug: name.toLowerCase().replace(/\s+/g, '-').replace(/[^a-z0-9-]/g, ''),
    primaryImageUrl,
  };
}

function generateProducts(): CatalogProductListItem[] {
  const products: CatalogProductListItem[] = [];
  let id = 1;
  for (const cat of CATEGORIES) {
    for (let i = 0; i < 6; i++) {
      products.push(mockProduct(id++, cat.name, cat.id, BRANDS[id % BRANDS.length]));
    }
  }
  return products;
}

const ALL_PRODUCTS = generateProducts();

@Injectable({ providedIn: 'root' })
export class MockCatalogService {

  getCategories(): Observable<CatalogCategory[]> {
    return of(CATEGORIES).pipe(delay(200));
  }

  getSaleProducts(): Observable<CatalogProductListItem[]> {
    const sale = [...ALL_PRODUCTS].slice(0, 12).map(item => {
      const originalPrice = Math.round(item.price * 1.25);
      const salePrice = Math.round(item.price * 0.85);
      return {
        ...item,
        price: salePrice,
        salePrice,
        originalPrice,
        isOnSale: true,
      };
    });
    return of(sale).pipe(delay(300));
  }

  getLanding(): Observable<CatalogLandingResponse> {
    const featured = ALL_PRODUCTS.slice(0, 8);
    const popular = ALL_PRODUCTS.slice(8, 16);
    const latest = ALL_PRODUCTS.slice(16, 24);

    // Mark a few featured items as on-sale for home banners/cards.
    const saleTagged = featured.map((item, index) => {
      if (index % 3 !== 0) return item;
      const originalPrice = Math.round(item.price * 1.2);
      const salePrice = Math.round(item.price * 0.9);
      return {
        ...item,
        price: salePrice,
        salePrice,
        originalPrice,
        isOnSale: true,
      };
    });

    return of({ categories: CATEGORIES, featured: saleTagged, popular, latest }).pipe(delay(300));
  }

  getFilters(categoryId: string): Observable<CatalogFilterResponse> {
    const categoryProducts = ALL_PRODUCTS.filter(p => p.categoryName === (CATEGORIES.find(c => c.id === categoryId)?.name || ''));
    const prices = categoryProducts.map(p => p.price);
    const minPrice = prices.length ? Math.min(...prices) : 500;
    const maxPrice = prices.length ? Math.max(...prices) : 15000;

    return of({
      categoryId,
      filters: CATEGORY_FILTERS[categoryId] || [
        {
          attributeId: 'attr-brand',
          name: 'Brand',
          filterType: 'select',
          sortOrder: 1,
          options: BRANDS.slice(0, 6).map(b => ({ value: b, count: Math.floor(Math.random() * 10) + 1 })),
          min: null,
          max: null,
          unit: null,
        },
        {
          attributeId: 'attr-condition',
          name: 'Condition',
          filterType: 'select',
          sortOrder: 2,
          options: [
            { value: 'New', count: 20 },
            { value: 'Refurbished', count: 5 },
          ],
          min: null,
          max: null,
          unit: null,
        },
      ],
      priceRange: { min: minPrice, max: maxPrice, currency: 'BDT' },
      availability: { inStockAvailable: categoryProducts.some(p => p.inStock) },
    }).pipe(delay(200));
  }

  searchProducts(request: CatalogSearchRequest): Observable<{ items: CatalogProductListItem[]; pageNumber: number; pageSize: number; totalCount: number }> {
    let filtered = [...ALL_PRODUCTS];

    if (request.categoryId) {
      const cat = CATEGORIES.find(c => c.id === request.categoryId);
      if (cat) {
        filtered = filtered.filter(p => p.categoryName === cat.name);
      }
    }

    if (request.search) {
      const term = request.search.toLowerCase();
      filtered = filtered.filter(p => p.name.toLowerCase().includes(term) || p.categoryName.toLowerCase().includes(term));
    }

    if (request.inStockOnly) {
      filtered = filtered.filter(p => p.inStock);
    }

    if (request.priceMin != null) {
      filtered = filtered.filter(p => p.price >= request.priceMin!);
    }
    if (request.priceMax != null) {
      filtered = filtered.filter(p => p.price <= request.priceMax!);
    }

    const totalCount = filtered.length;
    const start = (request.pageNumber - 1) * request.pageSize;
    const items = filtered.slice(start, start + request.pageSize);

    return of({ items, pageNumber: request.pageNumber, pageSize: request.pageSize, totalCount }).pipe(delay(300));
  }

  getProductDetail(partId: string): Observable<CatalogProductDetail> {
    const product = ALL_PRODUCTS.find(p => p.partId === partId) || ALL_PRODUCTS[0];
    const isOnSale = product.isOnSale ?? false;
    const originalPrice = product.originalPrice ?? (isOnSale ? Math.round(product.price * 1.2) : null);
    const salePrice = product.salePrice ?? (isOnSale ? Math.round(product.price * 0.9) : null);
    const detail: CatalogProductDetail = {
      partId: product.partId,
      name: product.name,
      description: `High-quality ${product.name} designed for optimal performance and durability. This ${product.categoryName.toLowerCase()} component meets or exceeds OEM specifications and is compatible with a wide range of vehicle makes and models. Manufactured using premium materials for extended service life.`,
      shortDescription: `Premium ${product.categoryName.toLowerCase()} component - ${product.brandName || 'OEM'} quality.`,
      categoryName: product.categoryName,
      brandName: product.brandName,
      inStock: product.inStock,
      slug: product.slug,
      primaryImageUrl: product.primaryImageUrl,
      isOnSale,
      salePrice,
      originalPrice,
      variants: [
        {
          variantId: 'var-1',
          name: 'Standard',
          code: 'STD',
          sku: `SKU-${product.partId}-STD`,
          price: salePrice ?? product.price,
          currency: product.currency,
          inStock: product.inStock,
          attributes: [{ attributeId: 'a1', attributeName: 'Grade', value: 'Standard', unit: null }],
        },
        {
          variantId: 'var-2',
          name: 'Premium',
          code: 'PRM',
          sku: `SKU-${product.partId}-PRM`,
          price: Math.round((salePrice ?? product.price) * 1.35),
          currency: product.currency,
          inStock: true,
          attributes: [{ attributeId: 'a1', attributeName: 'Grade', value: 'Premium', unit: null }],
        },
      ],
      specifications: [
        {
          groupName: 'General',
          sortOrder: 1,
          attributes: [
            { attributeId: 's1', attributeName: 'Brand', value: product.brandName || 'OEM', unit: null },
            { attributeId: 's2', attributeName: 'Category', value: product.categoryName, unit: null },
            { attributeId: 's3', attributeName: 'Condition', value: 'New', unit: null },
            { attributeId: 's4', attributeName: 'Warranty', value: '12', unit: 'months' },
          ],
        },
        {
          groupName: 'Fitment',
          sortOrder: 2,
          attributes: [
            { attributeId: 's5', attributeName: 'Vehicle Type', value: 'Universal / Multi-fit', unit: null },
            { attributeId: 's6', attributeName: 'Position', value: 'Front / Rear', unit: null },
          ],
        },
      ],
      media: [
        { url: product.primaryImageUrl, mediaType: 'image', sortOrder: 1, isPrimary: true },
        { url: `https://picsum.photos/seed/${product.partId}-alt1/640/480`, mediaType: 'image', sortOrder: 2, isPrimary: false },
        { url: `https://picsum.photos/seed/${product.partId}-alt2/640/480`, mediaType: 'image', sortOrder: 3, isPrimary: false },
      ],
      relatedProducts: ALL_PRODUCTS.filter(p => p.categoryName === product.categoryName && p.partId !== product.partId).slice(0, 4),
    };
    return of(detail).pipe(delay(300));
  }
}
