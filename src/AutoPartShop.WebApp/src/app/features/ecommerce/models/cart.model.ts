export interface CartItem {
  partId: string;
  variantId?: string | null;
  name: string;
  variantName?: string;
  categoryName: string;
  price: number;
  currency: string;
  quantity: number;
  primaryImageUrl: string;
  inStock: boolean;
}

export interface CheckoutFormData {
  customer: {
    fullName: string;
    email: string;
    phone: string;
  };
  shipping: {
    address: string;
    city: string;
    postalCode: string;
    country: string;
    notes?: string;
  };
  paymentMethod: string;
}

export interface Order {
  orderId: string;
  items: CartItem[];
  subtotal: number;
  currency: string;
  customer: CheckoutFormData['customer'];
  shipping: CheckoutFormData['shipping'];
  paymentMethod: string;
  createdAt: string;
  status: 'pending' | 'confirmed';
}
