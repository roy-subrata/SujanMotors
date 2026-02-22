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
  attributeFilters: AttributeFilterRequest[];
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
  shortDescription: string;
  categoryName: string;
  brandName?: string | null;
  inStock: boolean;
  slug: string;
  primaryImageUrl: string;
  salePrice?: number | null;
  originalPrice?: number | null;
  isOnSale?: boolean;
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
  price: number;
  currency: string;
  inStock: boolean;
  attributes: CatalogAttributeValue[];
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
