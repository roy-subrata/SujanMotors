// Category Models
export interface Category {
  id: string;
  code: string;
  name: string;
  description: string;
  parentCategoryId: string | null;
  isActive: boolean;
  displayOrder: number;
  createdBy: string;
  modifiedBy: string;
  breadcrumbPath: string;
  depthLevel: number;
  childCount: number;
  SubCategories: Category[];
}

export interface CreateCategoryRequest {
  code: string;
  name: string;
  description: string;
  parentId: string | null;
}

export interface UpdateCategoryRequest {
  code: string;
  name: string;
  description: string;
  parentId: string | null;
}

// Part Models
export interface Part {
  id: string;
  name: string;
  sku: string;
  categoryId: string;
  description: string;
  price: number;
  costPrice: number;
  quantity: number;
  isActive: boolean;
}

// Supplier Models
export interface Supplier {
  id: string;
  name: string;
  email: string;
  phone: string;
  address: string;
  isActive: boolean;
}

// Warehouse Models
export interface Warehouse {
  id: string;
  name: string;
  location: string;
  isActive: boolean;
}

// Stock Models
export interface Stock {
  id: string;
  partId: string;
  warehouseId: string;
  quantity: number;
  minLevel: number;
  maxLevel: number;
}

// Order Models
export interface Order {
  id: string;
  orderNumber: string;
  customerId: string;
  orderDate: Date;
  status: OrderStatus;
  totalAmount: number;
  items: OrderItem[];
}

export interface OrderItem {
  id: string;
  partId: string;
  quantity: number;
  price: number;
}

export enum OrderStatus {
  Pending = 'Pending',
  Confirmed = 'Confirmed',
  Dispatched = 'Dispatched',
  Delivered = 'Delivered',
  Cancelled = 'Cancelled'
}

// Pagination Models
export interface PaginatedResult<T> {
  data: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface PaginationParams {
  pageNumber: number;
  pageSize: number;
  searchTerm?: string;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

// API Response Models
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: string[];
}

export interface ApiErrorResponse {
  success: boolean;
  message: string;
  errors?: { [key: string]: string[] };
}
