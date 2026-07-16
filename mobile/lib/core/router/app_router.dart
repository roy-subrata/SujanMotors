import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/auth_controller.dart';
import '../../features/auth/login_screen.dart';
import '../../features/cashbook/cashbook_screen.dart';
import '../../features/customers/add_customer_screen.dart';
import '../../features/customers/customer_detail_screen.dart';
import '../../features/customers/customer_invoices_screen.dart';
import '../../features/customers/customer_payment_history_screen.dart';
import '../../features/customers/customer_statement_screen.dart';
import '../../features/customers/customers_screen.dart';
import '../../features/customers/invoice_detail_screen.dart';
import '../../shared/models/invoice.dart';
import '../../features/customers/receive_payment_screen.dart';
import '../../features/dashboard/dashboard_screen.dart';
import '../../features/suppliers/supplier_payment_screen.dart';
import '../../features/suppliers/supplier_statement_screen.dart';
import '../../features/suppliers/suppliers_screen.dart';
import '../../features/notifications/notifications_screen.dart';
import '../../features/products/product_compatibility_edit_screen.dart';
import '../../features/products/product_detail_screen.dart';
import '../../features/products/product_edit_screen.dart';
import '../../features/products/product_search_screen.dart';
import '../../features/products/product_specs_edit_screen.dart';
import '../../features/sales/quick_sale_screen.dart';
import '../../features/sales/sale_return_screen.dart';
import '../../features/sales/sales_screen.dart';
import '../../features/scanner/scanner_screen.dart';
import '../../features/stock/stock_in_entry_screen.dart';
import '../../features/stock/stock_in_list_screen.dart';
import '../../features/stock/stock_in_screen.dart';
import '../../shared/widgets/splash_screen.dart';

final routerProvider = Provider<GoRouter>((ref) {
  // Bridge Riverpod auth state into a Listenable go_router can refresh on.
  final refreshNotifier =
      ValueNotifier<AsyncValue<Object?>>(const AsyncLoading());
  ref.listen(
    authControllerProvider,
    (_, next) => refreshNotifier.value = next,
    fireImmediately: true,
  );
  ref.onDispose(refreshNotifier.dispose);

  return GoRouter(
    initialLocation: '/',
    refreshListenable: refreshNotifier,
    redirect: (context, state) {
      final auth = ref.read(authControllerProvider);
      final loc = state.matchedLocation;

      if (auth.isLoading) {
        return loc == '/splash' ? null : '/splash';
      }

      final loggedIn = auth.asData?.value != null;
      final atAuthScreen = loc == '/login' || loc == '/splash';

      if (!loggedIn) {
        return atAuthScreen ? (loc == '/splash' ? '/login' : null) : '/login';
      }
      if (atAuthScreen) return '/';
      return null;
    },
    routes: [
      GoRoute(path: '/splash', builder: (_, _) => const SplashScreen()),
      GoRoute(path: '/login', builder: (_, _) => const LoginScreen()),
      GoRoute(path: '/', builder: (_, _) => const DashboardScreen()),
      GoRoute(
          path: '/products',
          builder: (_, _) => const ProductSearchScreen()),
      GoRoute(
        path: '/product/:id',
        builder: (_, state) =>
            ProductDetailScreen(productId: state.pathParameters['id']!),
      ),
      GoRoute(
        path: '/product/:id/edit',
        builder: (_, state) =>
            ProductEditScreen(productId: state.pathParameters['id']!),
      ),
      GoRoute(
        path: '/product/:id/specs',
        builder: (_, state) =>
            ProductSpecsEditScreen(productId: state.pathParameters['id']!),
      ),
      GoRoute(
        path: '/product/:id/compatibility',
        builder: (_, state) => ProductCompatibilityEditScreen(
            productId: state.pathParameters['id']!),
      ),
      GoRoute(path: '/scan', builder: (_, _) => const ScannerScreen()),
      GoRoute(
          path: '/customers',
          builder: (_, _) => const CustomersScreen()),
      GoRoute(
          path: '/customers/new',
          builder: (_, _) => const AddCustomerScreen()),
      GoRoute(
        path: '/customers/:id/edit',
        builder: (_, state) =>
            AddCustomerScreen(customerId: state.pathParameters['id']),
      ),
      GoRoute(path: '/cashbook', builder: (_, _) => const CashBookScreen()),
      GoRoute(
          path: '/quick-sale',
          builder: (_, _) => const QuickSaleScreen()),
      GoRoute(path: '/sales', builder: (_, _) => const SalesScreen()),
      GoRoute(
        path: '/sales/return',
        builder: (_, state) => SaleReturnScreen(
          invoiceId: state.uri.queryParameters['invoiceId'],
          invoice: state.extra is Invoice ? state.extra as Invoice : null,
        ),
      ),
      GoRoute(
          path: '/stock-in', builder: (_, _) => const StockInListScreen()),
      GoRoute(
          path: '/stock-in/new',
          builder: (_, _) => const StockInEntryScreen()),
      GoRoute(
          path: '/stock-in/quick', builder: (_, _) => const StockInScreen()),
      GoRoute(
        path: '/customers/:id',
        builder: (_, state) =>
            CustomerDetailScreen(customerId: state.pathParameters['id']!),
      ),
      GoRoute(
        path: '/customers/:id/orders',
        builder: (_, state) =>
            CustomerInvoicesScreen(customerId: state.pathParameters['id']!),
      ),
      GoRoute(
        path: '/customers/:id/payments',
        builder: (_, state) => CustomerPaymentHistoryScreen(
            customerId: state.pathParameters['id']!),
      ),
      GoRoute(
        path: '/customers/:id/pay',
        builder: (_, state) =>
            ReceivePaymentScreen(customerId: state.pathParameters['id']!),
      ),
      GoRoute(
        path: '/customers/:id/statement',
        builder: (_, state) =>
            CustomerStatementScreen(customerId: state.pathParameters['id']!),
      ),
      GoRoute(
        path: '/notifications',
        builder: (_, _) => const NotificationsScreen(),
      ),
      GoRoute(
        path: '/suppliers',
        builder: (_, _) => const SuppliersScreen(),
      ),
      GoRoute(
        path: '/suppliers/:id/pay',
        builder: (_, state) => SupplierPaymentScreen(
            supplierId: state.pathParameters['id']!),
      ),
      GoRoute(
        path: '/suppliers/:id/statement',
        builder: (_, state) => SupplierStatementScreen(
            supplierId: state.pathParameters['id']!),
      ),
      GoRoute(
        path: '/invoice/:id',
        builder: (_, state) => InvoiceDetailScreen(
          invoiceId: state.pathParameters['id']!,
          invoice: state.extra is Invoice ? state.extra as Invoice : null,
        ),
      ),
    ],
  );
});
