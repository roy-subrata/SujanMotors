export interface CatalogLandingResponse {
  categories: CatalogCategory[];
  featured: CatalogProductListItem[];
  popular: CatalogProductListItem[];
  latest: CatalogProductListItem[];
}

export interface CatalogCategory {
  id: string;
  name: string;
  parentCategoryId?: string | null;
  depthLevel: number;
  displayOrder: number;
  childCount: number;
}

export interface CatalogProductListItem {
  partId: string;
  variantId?: string | null;
  name: string;
  categoryName: string;
  brandName?: string | null;
  price: number;
  salePrice?: number | null;
  originalPrice?: number | null;
  isOnSale?: boolean;
  currency: string;
  inStock: boolean;
  slug: string;
  primaryImageUrl: string;
}

export interface CatalogFilterResponse {
  categoryId: string;
  filters: CatalogFilter[];
  priceRange: PriceRangeFilter;
  availability: AvailabilityFilter;
}

export interface CatalogFilter {
  attributeId: string;
  name: string;
  filterType: string;
  sortOrder: number;
  options: CatalogFilterOption[];
  min?: number | null;
  max?: number | null;
  unit?: string | null;
}

export interface CatalogFilterOption {
  value: string;
  count: number;
}

export interface PriceRangeFilter {
  min?: number | null;
  max?: number | null;
  currency: string;
}

export interface AvailabilityFilter {
  inStockAvailable: boolean;
}

export interface CatalogSearchRequest {
  search: string;
  pageNumber: number;
  pageSize: number;
  categoryId?: string | null;
  includeDescendants: boolean;
  priceMin?: number | null;
  priceMax?: number | null;
  inStockOnly: boolean;
  onSaleOnly?: boolean;
  attributeFilters: AttributeFilterRequest[];
  vehicleId?: string | null;
}

export interface VehicleOption {
  id: string;
  make: string;
  model: string;
  year: number;
  engineType: string;
}

export interface AttributeFilterRequest {
  attributeId: string;
  values: string[];
  min?: number | null;
  max?: number | null;
}

export interface CatalogProductDetail {
  partId: string;
  name: string;
  description: string;
  richDescription?: string | null;
  shortDescription: string;
  categoryName: string;
  brandName?: string | null;
  sku?: string | null;
  tags?: string | null;
  inStock: boolean;
  slug: string;
  primaryImageUrl: string;
  basePrice: number;
  salePrice?: number | null;
  originalPrice?: number | null;
  isOnSale?: boolean;
  currency: string;
  hasWarranty: boolean;
  warrantyPeriodMonths?: number | null;
  warrantyType?: string | null;
  warrantyTerms?: string | null;
  variants: CatalogVariant[];
  specifications: CatalogAttributeGroup[];
  media: CatalogMedia[];
  relatedProducts: CatalogProductListItem[];
}

export interface CatalogVariant {
  variantId: string;
  name: string;
  code: string;
  sku?: string | null;
  price: number;           // Effective price (after discount)
  originalPrice?: number | null;
  salePrice?: number | null;
  isOnSale?: boolean;
  currency: string;
  inStock: boolean;
  attributes: CatalogAttributeValue[];
  specifications: CatalogAttributeGroup[];
}

export interface CatalogAttributeGroup {
  groupName: string;
  sortOrder: number;
  attributes: CatalogAttributeValue[];
}

export interface CatalogAttributeValue {
  attributeId: string;
  attributeName: string;
  value: string;
  unit?: string | null;
}

export interface CatalogMedia {
  url: string;
  mediaType: string;
  sortOrder: number;
  isPrimary: boolean;
}

export interface ShopPolicies {
  freeShippingEnabled: boolean;
  freeShippingThreshold: number;
  freeShippingCurrency: string;
  returnPolicyDays: number;
  returnPolicyText: string;
}

export const DEFAULT_SHOP_POLICIES: ShopPolicies = {
  freeShippingEnabled: true,
  freeShippingThreshold: 5000,
  freeShippingCurrency: 'BDT',
  returnPolicyDays: 30,
  returnPolicyText: '30-day return policy',
};
