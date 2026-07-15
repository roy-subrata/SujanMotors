import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/customer.dart';
import '../../shared/models/invoice.dart';
import '../../shared/models/sale_return.dart';
import '../../shared/widgets/design_system.dart';
import '../../shared/widgets/state_views.dart';
import '../sales/sales_returns_repository.dart';
import 'customers_repository.dart';

class CustomerDetailScreen extends ConsumerWidget {
  const CustomerDetailScreen({super.key, required this.customerId});

  final String customerId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final customerAsync = ref.watch(customerDetailProvider(customerId));

    return Scaffold(
      appBar: AppBar(
        title: Text(
          'Customer',
          style: GoogleFonts.instrumentSans(
            fontSize: 16,
            fontWeight: FontWeight.w700
          ),
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.edit_outlined),
            onPressed: () {},
          ),
        ],
      ),
      body: customerAsync.when(
        loading: () => const LoadingView(),
        error: (e, _) => ListView(children: [
          const SizedBox(height: 120),
          ErrorView(
            message:
                e is AppException ? e.message : 'Failed to load customer.',
            onRetry: () =>
                ref.invalidate(customerDetailProvider(customerId)),
          ),
        ]),
        data: (customer) => _Body(customer: customer),
      ),
    );
  }
}

// ── Body ─────────────────────────────────────────────────────────────────────

class _Body extends ConsumerStatefulWidget {
  const _Body({required this.customer});

  final Customer customer;

  @override
  ConsumerState<_Body> createState() => _BodyState();
}

class _BodyState extends ConsumerState<_Body> {
  int _tabIndex = 0;
  final _scrollCtrl = ScrollController();

  // Invoice pagination
  final _invoices = <Invoice>[];
  int _invoicePage = 0;
  bool _invoiceLoading = false;
  bool _invoiceHasMore = true;
  String? _invoiceError;
  bool _invoiceInitialized = false;

  // Payment pagination
  final _payments = <PaymentHistoryItem>[];
  int _paymentPage = 0;
  bool _paymentLoading = false;
  bool _paymentHasMore = true;
  String? _paymentError;
  bool _paymentInitialized = false;

  // Returns pagination
  final _returns = <SalesReturn>[];
  int _returnPage = 0;
  bool _returnLoading = false;
  bool _returnHasMore = true;
  String? _returnError;
  bool _returnInitialized = false;

  @override
  void initState() {
    super.initState();
    _scrollCtrl.addListener(_onScroll);
    WidgetsBinding.instance
        .addPostFrameCallback((_) => _loadMoreInvoices());
  }

  @override
  void dispose() {
    _scrollCtrl.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scrollCtrl.position.pixels <
        _scrollCtrl.position.maxScrollExtent - 300) {
      return;
    }
    if (_tabIndex == 0) {
      _loadMoreInvoices();
    } else if (_tabIndex == 1) {
      _loadMorePayments();
    } else if (_tabIndex == 2) {
      _loadMoreReturns();
    }
  }

  void _onTabSelect(int i) {
    setState(() => _tabIndex = i);
    if (i == 1 && !_paymentInitialized) _loadMorePayments();
    if (i == 2 && !_returnInitialized) _loadMoreReturns();
  }

  Future<void> _loadMoreInvoices() async {
    if (_invoiceLoading || !_invoiceHasMore) return;
    setState(() => _invoiceLoading = true);
    try {
      final chunk =
          await ref.read(customersRepositoryProvider).invoicesPage(
                customerId: widget.customer.id,
                page: _invoicePage + 1,
              );
      if (!mounted) return;
      setState(() {
        _invoices.addAll(chunk.items);
        _invoicePage++;
        _invoiceHasMore = chunk.hasMore;
        _invoiceLoading = false;
        _invoiceInitialized = true;
        _invoiceError = null;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _invoiceLoading = false;
        _invoiceError = e.toString();
        _invoiceInitialized = true;
      });
    }
  }

  Future<void> _loadMorePayments() async {
    if (_paymentLoading || !_paymentHasMore) return;
    setState(() => _paymentLoading = true);
    try {
      final chunk =
          await ref.read(customersRepositoryProvider).paymentsPage(
                customerId: widget.customer.id,
                page: _paymentPage + 1,
              );
      if (!mounted) return;
      setState(() {
        _payments.addAll(chunk.items);
        _paymentPage++;
        _paymentHasMore = chunk.hasMore;
        _paymentLoading = false;
        _paymentInitialized = true;
        _paymentError = null;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _paymentLoading = false;
        _paymentError = e.toString();
        _paymentInitialized = true;
      });
    }
  }

  Future<void> _loadMoreReturns() async {
    if (_returnLoading || !_returnHasMore) return;
    setState(() => _returnLoading = true);
    try {
      final chunk = await ref
          .read(salesReturnsRepositoryProvider)
          .list(
            searchTerm: widget.customer.fullName,
            page: _returnPage + 1,
          );
      if (!mounted) return;
      setState(() {
        _returns.addAll(chunk.items);
        _returnPage++;
        _returnHasMore = chunk.hasMore;
        _returnLoading = false;
        _returnInitialized = true;
        _returnError = null;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _returnLoading = false;
        _returnError = e.toString();
        _returnInitialized = true;
      });
    }
  }

  Future<void> _refresh() async {
    final id = widget.customer.id;
    ref.invalidate(customerDetailProvider(id));
    ref.invalidate(customerPaymentSummaryProvider(id));
    setState(() {
      _invoices.clear();
      _invoicePage = 0;
      _invoiceHasMore = true;
      _invoiceLoading = false;
      _invoiceError = null;
      _invoiceInitialized = false;
      _payments.clear();
      _paymentPage = 0;
      _paymentHasMore = true;
      _paymentLoading = false;
      _paymentError = null;
      _paymentInitialized = false;
      _returns.clear();
      _returnPage = 0;
      _returnHasMore = true;
      _returnLoading = false;
      _returnError = null;
      _returnInitialized = false;
    });
    await _loadMoreInvoices();
  }

  @override
  Widget build(BuildContext context) {
    final id = widget.customer.id;
    final summaryAsync = ref.watch(customerPaymentSummaryProvider(id));

    return RefreshIndicator(
      onRefresh: _refresh,
      child: ListView(
        controller: _scrollCtrl,
        padding: const EdgeInsets.fromLTRB(16, 4, 16, 24),
        children: [
          // ── Header card ──────────────────────────────────────────────
          CardSection(
            padding: const EdgeInsets.all(15),
            child: Column(
              children: [
                // Avatar + name + meta
                Row(
                  children: [
                    InitialsAvatar(
                        name: widget.customer.fullName, radius: 24),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            widget.customer.fullName,
                            style: GoogleFonts.instrumentSans(
                              fontSize: 16,
                              fontWeight: FontWeight.w700
                            ),
                          ),
                          const SizedBox(height: 3),
                          Text(
                            [
                              if ((widget.customer.phone ?? '').isNotEmpty)
                                widget.customer.phone!,
                              if ((widget.customer.customerType ?? '')
                                  .isNotEmpty)
                                widget.customer.customerType!,
                              if (widget.customer.lastPurchaseDate != null)
                                'since ${formatDate(widget.customer.lastPurchaseDate!)}',
                            ].join(' · '),
                            style: GoogleFonts.instrumentSans(
                              fontSize: 12
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 14),

                // 3-stat row
                summaryAsync.when(
                  data: (s) => Row(
                    children: [
                      _StatBox(
                        label: 'Due',
                        value: formatCurrency(s.amountDue),
                        valueColor: AppColors.red,
                        bg: AppColors.redBg,
                      ),
                      const SizedBox(width: 8),
                      _StatBox(
                        label: 'Lifetime',
                        value: formatCurrency(
                            widget.customer.totalPurchaseAmount),
                      ),
                      const SizedBox(width: 8),
                      _StatBox(
                        label: 'Invoices',
                        value: '${s.totalInvoices}',
                      ),
                    ],
                  ),
                  loading: () => const SizedBox(
                      height: 56,
                      child: Center(child: CircularProgressIndicator())),
                  error: (_, _) { return const SizedBox.shrink(); },
                ),

                const SizedBox(height: 14),

                // 2×2 action grid
                GridView.count(
                  crossAxisCount: 2,
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  mainAxisSpacing: 8,
                  crossAxisSpacing: 8,
                  childAspectRatio: 3.2,
                  children: [
                    _ActionButton(
                      label: 'Receive payment',
                      icon: Icons.payments_outlined,
                      filled: true,
                      onTap: () => context
                          .push('/customers/${widget.customer.id}/pay'),
                    ),
                    _ActionButton(
                      label: 'Send reminder',
                      icon: Icons.notifications_active_outlined,
                      filled: false,
                      onTap: () => _showReminderSheet(context),
                    ),
                    _ActionButton(
                      label: 'Statement',
                      icon: Icons.description_outlined,
                      filled: false,
                      onTap: () => context.push(
                          '/customers/${widget.customer.id}/statement'),
                    ),
                    _ActionButton(
                      label: 'New sale',
                      icon: Icons.add_shopping_cart_outlined,
                      filled: false,
                      onTap: () => context.push('/quick-sale'),
                    ),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),

          // ── Tab chips ────────────────────────────────────────────────
          FilterChipRow(
            selected: _tabIndex,
            onSelect: _onTabSelect,
            chips: const [
              FilterChipData(label: 'Invoices'),
              FilterChipData(label: 'Payments'),
              FilterChipData(label: 'Returns'),
            ],
          ),
          const SizedBox(height: 12),

          // ── Invoices tab ─────────────────────────────────────────────
          if (_tabIndex == 0) ...[
            if (!_invoiceInitialized ||
                (_invoices.isEmpty && _invoiceLoading))
              const LoadingView()
            else if (_invoices.isEmpty && _invoiceError != null)
              ErrorView(
                message: 'Failed to load invoices.',
                onRetry: () {
                  setState(() {
                    _invoiceError = null;
                    _invoiceInitialized = false;
                  });
                  _loadMoreInvoices();
                },
              )
            else if (_invoices.isEmpty)
              const EmptyView(
                  message: 'No invoices yet.',
                  icon: Icons.receipt_long_outlined)
            else
              _InvoiceList(
                invoices: _invoices,
                isLoading: _invoiceLoading,
                hasMore: _invoiceHasMore,
              ),
          ],

          // ── Payments tab ─────────────────────────────────────────────
          if (_tabIndex == 1) ...[
            if (!_paymentInitialized ||
                (_payments.isEmpty && _paymentLoading))
              const LoadingView()
            else if (_payments.isEmpty && _paymentError != null)
              ErrorView(
                message: 'Failed to load payments.',
                onRetry: () {
                  setState(() {
                    _paymentError = null;
                    _paymentInitialized = false;
                  });
                  _loadMorePayments();
                },
              )
            else if (_payments.isEmpty)
              const EmptyView(
                  message: 'No payments yet.',
                  icon: Icons.payments_outlined)
            else
              _PaymentList(
                payments: _payments,
                isLoading: _paymentLoading,
                hasMore: _paymentHasMore,
              ),
          ],

          // ── Returns tab ──────────────────────────────────────────────
          if (_tabIndex == 2) ...[
            if (!_returnInitialized ||
                (_returns.isEmpty && _returnLoading))
              const LoadingView()
            else if (_returns.isEmpty && _returnError != null)
              ErrorView(
                message: 'Failed to load returns.',
                onRetry: () {
                  setState(() {
                    _returnError = null;
                    _returnInitialized = false;
                  });
                  _loadMoreReturns();
                },
              )
            else if (_returns.isEmpty)
              Column(children: [
                const EmptyView(
                  message: 'No returns recorded.',
                  icon: Icons.assignment_return_outlined,
                ),
                const SizedBox(height: 12),
                _InitiateReturnButton(customerId: id),
              ])
            else
              _ReturnsList(
                returns: _returns,
                isLoading: _returnLoading,
                hasMore: _returnHasMore,
                customerId: id,
              ),
          ],

          const SizedBox(height: 24),
        ],
      ),
    );
  }

  void _showReminderSheet(BuildContext context) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) => _ReminderSheet(customer: widget.customer),
    );
  }
}

// ── Invoice list ──────────────────────────────────────────────────────────────

class _InvoiceList extends StatelessWidget {
  const _InvoiceList({
    required this.invoices,
    required this.isLoading,
    required this.hasMore,
  });

  final List<Invoice> invoices;
  final bool isLoading;
  final bool hasMore;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Container(
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: Theme.of(context).colorScheme.outline),
          ),
          clipBehavior: Clip.antiAlias,
          child: Column(
            children: invoices.asMap().entries.map((e) {
              final i = e.key;
              final inv = e.value;
              return Column(
                children: [
                  if (i > 0)
                    Divider(height: 1, color: Theme.of(context).colorScheme.outline.withAlpha(60)),
                  _InvoiceRow(invoice: inv),
                ],
              );
            }).toList(),
          ),
        ),
        _PaginationFooter(isLoading: isLoading, hasMore: hasMore),
      ],
    );
  }
}

class _InvoiceRow extends StatelessWidget {
  const _InvoiceRow({required this.invoice});

  final Invoice invoice;

  @override
  Widget build(BuildContext context) {
    final hasBalance = invoice.outstandingAmount > 0;
    return InkWell(
      onTap: () => context.push(
        '/invoice/${invoice.id}',
        extra: invoice,
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    invoice.invoiceNumber,
                    style: GoogleFonts.instrumentSans(
                      fontSize: 13.5,
                      fontWeight: FontWeight.w500
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    '${formatDate(invoice.invoiceDate)} · ${invoice.salesOrderNumber}',
                    style: GoogleFonts.instrumentSans(
                      fontSize: 11.5
                    ),
                  ),
                ],
              ),
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  formatCurrency(invoice.grandTotal),
                  style: GoogleFonts.instrumentSans(
                    fontSize: 13.5,
                    fontWeight: FontWeight.w600
                  ),
                ),
                const SizedBox(height: 4),
                if (hasBalance)
                  Text(
                    'Due ${formatCurrency(invoice.outstandingAmount)}',
                    style: GoogleFonts.instrumentSans(
                      fontSize: 10.5,
                      fontWeight: FontWeight.w600,
                      color: AppColors.red,
                    ),
                  )
                else
                  StatusPill(label: invoice.status ?? 'PENDING'),
              ],
            ),
            const SizedBox(width: 8),
            const Icon(Icons.chevron_right,
                color: AppColors.disabled, size: 18),
          ],
        ),
      ),
    );
  }
}

// ── Payment list ──────────────────────────────────────────────────────────────

class _PaymentList extends StatelessWidget {
  const _PaymentList({
    required this.payments,
    required this.isLoading,
    required this.hasMore,
  });

  final List<PaymentHistoryItem> payments;
  final bool isLoading;
  final bool hasMore;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Container(
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: Theme.of(context).colorScheme.outline),
          ),
          clipBehavior: Clip.antiAlias,
          child: Column(
            children: payments.asMap().entries.map((e) {
              final i = e.key;
              final p = e.value;
              return Column(
                children: [
                  if (i > 0)
                    Divider(height: 1, color: Theme.of(context).colorScheme.outline.withAlpha(60)),
                  _PaymentRow(payment: p),
                ],
              );
            }).toList(),
          ),
        ),
        _PaginationFooter(isLoading: isLoading, hasMore: hasMore),
      ],
    );
  }
}

class _PaymentRow extends StatelessWidget {
  const _PaymentRow({required this.payment});

  final PaymentHistoryItem payment;

  @override
  Widget build(BuildContext context) {
    final method = payment.paymentMethod ?? 'Cash';
    final isCompleted =
        (payment.status ?? '').toUpperCase() == 'COMPLETED';
    return Padding(
      padding:
          const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      child: Row(
        children: [
          // Method icon badge
          Container(
            width: 36,
            height: 36,
            decoration: BoxDecoration(
              color: isCompleted ? AppColors.greenBg : Theme.of(context).scaffoldBackgroundColor,
              borderRadius: BorderRadius.circular(10),
            ),
            alignment: Alignment.center,
            child: Text(
              _methodEmoji(method),
              style: const TextStyle(fontSize: 16),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  method,
                  style: GoogleFonts.instrumentSans(
                    fontSize: 13.5,
                    fontWeight: FontWeight.w500
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  [
                    formatDate(payment.paymentDate),
                    if ((payment.invoiceNumber ?? '').isNotEmpty)
                      payment.invoiceNumber!,
                  ].join(' · '),
                  style: GoogleFonts.instrumentSans(
                    fontSize: 11.5
                  ),
                ),
              ],
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                formatCurrency(payment.amount),
                style: GoogleFonts.instrumentSans(
                  fontSize: 13.5,
                  fontWeight: FontWeight.w600,
                  color:
                      isCompleted ? AppColors.green : AppColors.ink,
                ),
              ),
              const SizedBox(height: 4),
              StatusPill(label: payment.status ?? 'PENDING'),
            ],
          ),
        ],
      ),
    );
  }

  String _methodEmoji(String method) {
    return switch (method.toLowerCase()) {
      'cash' => '💵',
      'bank' || 'bank transfer' => '🏦',
      'bkash' || 'mobile banking' => '📱',
      'cheque' => '📄',
      _ => '💳',
    };
  }
}

// ── Returns list ──────────────────────────────────────────────────────────────

class _ReturnsList extends StatelessWidget {
  const _ReturnsList({
    required this.returns,
    required this.isLoading,
    required this.hasMore,
    required this.customerId,
  });

  final List<SalesReturn> returns;
  final bool isLoading;
  final bool hasMore;
  final String customerId;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Container(
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: Theme.of(context).colorScheme.outline),
          ),
          clipBehavior: Clip.antiAlias,
          child: Column(
            children: returns.asMap().entries.map((e) {
              return Column(
                children: [
                  if (e.key > 0)
                    Divider(height: 1, color: Theme.of(context).colorScheme.outline.withAlpha(60)),
                  _ReturnRow(ret: e.value),
                ],
              );
            }).toList(),
          ),
        ),
        _PaginationFooter(isLoading: isLoading, hasMore: hasMore),
        const SizedBox(height: 12),
        _InitiateReturnButton(customerId: customerId),
      ],
    );
  }
}

class _ReturnRow extends StatelessWidget {
  const _ReturnRow({required this.ret});

  final SalesReturn ret;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      child: Row(
        children: [
          Container(
            width: 36,
            height: 36,
            decoration: BoxDecoration(
              color: AppColors.redBg,
              borderRadius: BorderRadius.circular(10),
            ),
            alignment: Alignment.center,
            child: const Icon(Icons.assignment_return_outlined,
                size: 18, color: AppColors.red),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  ret.returnNumber,
                  style: GoogleFonts.instrumentSans(
                    fontSize: 13.5,
                    fontWeight: FontWeight.w500
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  [
                    formatDate(ret.createdAt),
                    if ((ret.salesOrderNumber ?? '').isNotEmpty)
                      ret.salesOrderNumber!,
                  ].join(' · '),
                  style: GoogleFonts.instrumentSans(
                    fontSize: 11.5
                  ),
                ),
              ],
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                formatCurrency(ret.totalRefundAmount),
                style: GoogleFonts.instrumentSans(
                  fontSize: 13.5,
                  fontWeight: FontWeight.w600,
                  color: AppColors.red,
                ),
              ),
              const SizedBox(height: 4),
              StatusPill(label: ret.status),
            ],
          ),
        ],
      ),
    );
  }
}

class _InitiateReturnButton extends StatelessWidget {
  const _InitiateReturnButton({required this.customerId});

  final String customerId;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      child: OutlinedButton.icon(
        onPressed: () => context.push('/sales/return'),
        icon: const Icon(Icons.assignment_return_outlined, size: 16),
        label: const Text('Initiate return'),
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.ink,
          side: BorderSide(color: Theme.of(context).colorScheme.outline),
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          padding: const EdgeInsets.symmetric(vertical: 13),
          textStyle: GoogleFonts.instrumentSans(
              fontSize: 13.5, fontWeight: FontWeight.w600),
        ),
      ),
    );
  }
}

// ── Pagination footer ─────────────────────────────────────────────────────────

class _PaginationFooter extends StatelessWidget {
  const _PaginationFooter(
      {required this.isLoading, required this.hasMore});

  final bool isLoading;
  final bool hasMore;

  @override
  Widget build(BuildContext context) {
    if (isLoading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 16),
        child: Center(
          child: SizedBox(
            width: 20,
            height: 20,
            child: CircularProgressIndicator(strokeWidth: 2),
          ),
        ),
      );
    }
    if (!hasMore) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: 12),
        child: Center(
          child: Text(
            'All records loaded',
            style: GoogleFonts.instrumentSans(
                fontSize: 12, color: AppColors.muted),
          ),
        ),
      );
    }
    return const SizedBox.shrink();
  }
}

// ── Stat box ──────────────────────────────────────────────────────────────────

class _StatBox extends StatelessWidget {
  const _StatBox({
    required this.label,
    required this.value,
    this.valueColor,
    this.bg,
  });

  final String label;
  final String value;
  final Color? valueColor;
  final Color? bg;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Container(
        padding: const EdgeInsets.fromLTRB(10, 10, 10, 11),
        decoration: BoxDecoration(
          color: bg ?? Theme.of(context).scaffoldBackgroundColor,
          borderRadius: BorderRadius.circular(10),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label,
                style: GoogleFonts.instrumentSans(
                    fontSize: 10.5, color: AppColors.muted)),
            const SizedBox(height: 3),
            Text(value,
                style: GoogleFonts.instrumentSans(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: valueColor ?? AppColors.ink,
                )),
          ],
        ),
      ),
    );
  }
}

// ── Action button ─────────────────────────────────────────────────────────────

class _ActionButton extends StatelessWidget {
  const _ActionButton({
    required this.label,
    required this.icon,
    required this.filled,
    required this.onTap,
  });

  final String label;
  final IconData icon;
  final bool filled;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding:
            const EdgeInsets.symmetric(horizontal: 12, vertical: 0),
        decoration: BoxDecoration(
          color: filled ? AppColors.ink : Theme.of(context).colorScheme.surface,
          borderRadius: BorderRadius.circular(11),
          border: Border.all(
              color: filled ? AppColors.ink : Theme.of(context).colorScheme.outline),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon,
                size: 15,
                color: filled ? Colors.white : AppColors.secondary),
            const SizedBox(width: 6),
            Text(label,
                style: GoogleFonts.instrumentSans(
                  fontSize: 12.5,
                  fontWeight: FontWeight.w600,
                  color: filled ? Colors.white : AppColors.ink,
                )),
          ],
        ),
      ),
    );
  }
}

// ── Reminder sheet ────────────────────────────────────────────────────────────

class _ReminderSheet extends ConsumerStatefulWidget {
  const _ReminderSheet({required this.customer});

  final Customer customer;

  @override
  ConsumerState<_ReminderSheet> createState() => _ReminderSheetState();
}

class _ReminderSheetState extends ConsumerState<_ReminderSheet> {
  int _channelIndex = 0;
  bool _sending = false;

  static const _channels = ['SMS', 'WhatsApp', 'Email'];

  Future<void> _send() async {
    setState(() => _sending = true);
    final messenger = ScaffoldMessenger.of(context);
    final nav = Navigator.of(context);
    final errorColor = AppColors.red;
    try {
      final msg = await ref
          .read(customersRepositoryProvider)
          .sendPaymentReminder(
            customerId: widget.customer.id,
            channel: _channels[_channelIndex].toUpperCase(),
          );
      nav.pop();
      messenger.showSnackBar(SnackBar(content: Text(msg)));
    } on AppException catch (e) {
      messenger.showSnackBar(SnackBar(
        content: Text(e.message),
        backgroundColor: errorColor,
      ));
    } finally {
      if (mounted) setState(() => _sending = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.vertical(top: Radius.circular(22)),
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: EdgeInsets.only(
            left: 20,
            right: 20,
            top: 12,
            bottom: MediaQuery.viewInsetsOf(context).bottom + 16,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Center(
                child: Container(
                  width: 40,
                  height: 4,
                  decoration: BoxDecoration(
                    color: Theme.of(context).colorScheme.outline,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),
              const SizedBox(height: 20),
              Text(
                'Send payment reminder',
                style: GoogleFonts.instrumentSans(
                  fontSize: 17,
                  fontWeight: FontWeight.w700
                ),
              ),
              const SizedBox(height: 4),
              Text(
                '${widget.customer.fullName} · due ${formatCurrency(widget.customer.dueAmount)}',
                style: GoogleFonts.instrumentSans(
                    fontSize: 12.5, color: AppColors.muted),
              ),
              const SizedBox(height: 20),
              MethodGrid(
                methods: _channels,
                selected: _channelIndex,
                onSelect: (i) =>
                    setState(() => _channelIndex = i),
              ),
              const SizedBox(height: 20),
              SizedBox(
                width: double.infinity,
                height: 50,
                child: FilledButton(
                  onPressed: _sending ? null : _send,
                  style: FilledButton.styleFrom(
                    backgroundColor: AppColors.ink,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12)),
                    textStyle: GoogleFonts.instrumentSans(
                        fontSize: 15, fontWeight: FontWeight.w700),
                  ),
                  child: _sending
                      ? const SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(
                              strokeWidth: 2.5, color: Colors.white),
                        )
                      : const Text('Send reminder'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
