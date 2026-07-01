import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/customer.dart';
import '../../shared/models/invoice.dart';
import '../../shared/widgets/state_views.dart';
import 'customers_repository.dart';

class CustomerDetailScreen extends ConsumerWidget {
  const CustomerDetailScreen({super.key, required this.customerId});

  final String customerId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final customerAsync = ref.watch(customerDetailProvider(customerId));

    return Scaffold(
      appBar: AppBar(
        flexibleSpace: const AppBarGradient(),
        title: const Text('Customer'),
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(customerDetailProvider(customerId));
          ref.invalidate(customerPaymentSummaryProvider(customerId));
          ref.invalidate(customerOrdersProvider(customerId));
        },
        child: customerAsync.when(
          loading: () => const LoadingView(),
          error: (e, _) => ListView(children: [
            const SizedBox(height: 120),
            ErrorView(
              message:
                  e is AppException ? e.message : 'Failed to load customer.',
              onRetry: () => ref.invalidate(customerDetailProvider(customerId)),
            ),
          ]),
          data: (customer) => _Body(customer: customer),
        ),
      ),
    );
  }
}

// ── Body ─────────────────────────────────────────────────────────────────────

class _Body extends ConsumerWidget {
  const _Body({required this.customer});

  final Customer customer;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final summaryAsync =
        ref.watch(customerPaymentSummaryProvider(customer.id));
    final ordersAsync = ref.watch(customerOrdersProvider(customer.id));

    return ListView(
      padding: EdgeInsets.zero,
      children: [
        // Hero gradient header — flows seamlessly from the AppBar gradient
        _HeroSection(customer: customer),

        Padding(
          padding: const EdgeInsets.fromLTRB(16, 20, 16, 24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _ContactCard(customer: customer),
              const SizedBox(height: 20),

              _SectionHeader(
                icon: Icons.account_balance_wallet_outlined,
                title: 'Payment & due',
              ),
              const SizedBox(height: 8),

              summaryAsync.when(
                loading: () => const Padding(
                  padding: EdgeInsets.symmetric(vertical: 24),
                  child: LoadingView(),
                ),
                error: (e, _) => ErrorView(
                  message: e is AppException
                      ? e.message
                      : 'Failed to load payments.',
                  onRetry: () => ref
                      .invalidate(customerPaymentSummaryProvider(customer.id)),
                ),
                data: (summary) => _PaymentSummaryCard(
                  customer: customer,
                  summary: summary,
                ),
              ),

              const SizedBox(height: 20),

              // 2-column action cards
              Row(
                children: [
                  Expanded(
                    child: _ActionCard(
                      icon: Icons.shopping_bag_outlined,
                      label: 'Invoices',
                      subtitle: ordersAsync.maybeWhen(
                        data: (orders) => '${orders.length} order(s)',
                        orElse: () => 'Parts buying history',
                      ),
                      color: const Color(0xFF4F46E5),
                      bg: const Color(0xFFEEF2FF),
                      onTap: () =>
                          context.push('/customers/${customer.id}/orders'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: _ActionCard(
                      icon: Icons.history,
                      label: 'Payments',
                      subtitle: summaryAsync.maybeWhen(
                        data: (s) => '${s.history.length} payment(s)',
                        orElse: () => 'Completed & pending',
                      ),
                      color: const Color(0xFF0F766E),
                      bg: const Color(0xFFECFDF5),
                      onTap: () =>
                          context.push('/customers/${customer.id}/payments'),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              // Full-width statement card
              _ActionCard(
                icon: Icons.description_outlined,
                label: 'Account Statement',
                subtitle: 'Full transaction history & PDF',
                color: const Color(0xFF7C3AED),
                bg: const Color(0xFFF5F3FF),
                onTap: () =>
                    context.push('/customers/${customer.id}/statement'),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

// ── Hero gradient header ──────────────────────────────────────────────────────

class _HeroSection extends StatelessWidget {
  const _HeroSection({required this.customer});

  final Customer customer;

  static const _palette = [
    Color(0xFFC7D2FE), Color(0xFF99F6E4), Color(0xFFFDE68A),
    Color(0xFFFECDD3), Color(0xFFBBF7D0), Color(0xFFE9D5FF),
    Color(0xFFFED7AA), Color(0xFFA5F3FC),
  ];

  static const _paletteText = [
    Color(0xFF3730A3), Color(0xFF0F766E), Color(0xFF92400E),
    Color(0xFF9F1239), Color(0xFF166534), Color(0xFF6B21A8),
    Color(0xFF9A3412), Color(0xFF155E75),
  ];

  Color get _bgColor =>
      _palette[customer.fullName.hashCode.abs() % _palette.length];

  Color get _fgColor =>
      _paletteText[customer.fullName.hashCode.abs() % _paletteText.length];

  String _initials() {
    final parts = customer.fullName
        .trim()
        .split(RegExp(r'\s+'))
        .where((p) => p.isNotEmpty)
        .toList();
    if (parts.isEmpty) return '?';
    if (parts.length == 1) return parts.first[0].toUpperCase();
    return '${parts.first[0]}${parts.last[0]}'.toUpperCase();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Container(
      width: double.infinity,
      decoration: const BoxDecoration(gradient: AppGradients.brand),
      padding: const EdgeInsets.fromLTRB(16, 28, 16, 28),
      child: Column(
        children: [
          // Avatar with white ring for separation from gradient bg
          Container(
            padding: const EdgeInsets.all(3),
            decoration: const BoxDecoration(
              color: Colors.white,
              shape: BoxShape.circle,
            ),
            child: CircleAvatar(
              radius: 44,
              backgroundColor: _bgColor,
              child: Text(
                _initials(),
                style: TextStyle(
                  fontSize: 34,
                  fontWeight: FontWeight.w900,
                  color: _fgColor,
                ),
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Name
          Text(
            customer.fullName,
            textAlign: TextAlign.center,
            style: theme.textTheme.headlineSmall?.copyWith(
              color: Colors.white,
              fontWeight: FontWeight.w800,
            ),
          ),

          // Company
          if ((customer.companyName ?? '').isNotEmpty) ...[
            const SizedBox(height: 4),
            Text(
              customer.companyName!,
              textAlign: TextAlign.center,
              style: const TextStyle(color: Colors.white70, fontSize: 14),
            ),
          ],

          const SizedBox(height: 16),

          // Identity chips: code · type · status
          Wrap(
            spacing: 8,
            runSpacing: 8,
            alignment: WrapAlignment.center,
            children: [
              _HeroChip(label: customer.customerCode),
              if ((customer.customerType ?? '').isNotEmpty)
                _HeroChip(label: customer.customerType!),
              if (customer.status != null) ...[
                _HeroChip(
                  label: customer.status!,
                  bg: customer.status!.toUpperCase() == 'ACTIVE'
                      ? Colors.green.shade100
                      : Colors.white24,
                  color: customer.status!.toUpperCase() == 'ACTIVE'
                      ? Colors.green.shade800
                      : Colors.white,
                ),
              ],
            ],
          ),
        ],
      ),
    );
  }
}

class _HeroChip extends StatelessWidget {
  const _HeroChip({required this.label, this.bg, this.color});

  final String label;
  final Color? bg;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 5),
      decoration: BoxDecoration(
        color: bg ?? Colors.white.withValues(alpha: 0.2),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        label,
        style: TextStyle(
          fontSize: 12,
          color: color ?? Colors.white,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

// ── Contact card ──────────────────────────────────────────────────────────────

class _ContactCard extends StatelessWidget {
  const _ContactCard({required this.customer});

  final Customer customer;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    final rows = <(IconData, String, String)>[
      if ((customer.phone ?? '').isNotEmpty)
        (Icons.phone_outlined, 'Phone', customer.phone!),
      if ((customer.email ?? '').isNotEmpty)
        (Icons.email_outlined, 'Email', customer.email!),
      if ((customer.city ?? '').isNotEmpty)
        (Icons.location_on_outlined, 'City', customer.city!),
    ];

    if (rows.isEmpty) return const SizedBox.shrink();

    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: scheme.outlineVariant.withValues(alpha: 0.5)),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        child: Column(
          children: rows.map((row) {
            final (icon, label, value) = row;
            return Padding(
              padding: const EdgeInsets.symmetric(vertical: 8),
              child: Row(
                children: [
                  Container(
                    width: 36,
                    height: 36,
                    decoration: BoxDecoration(
                      color: scheme.primaryContainer.withValues(alpha: 0.5),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Icon(icon, size: 18, color: scheme.primary),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          label,
                          style: theme.textTheme.labelSmall
                              ?.copyWith(color: scheme.onSurfaceVariant),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          value,
                          style: theme.textTheme.bodyMedium
                              ?.copyWith(fontWeight: FontWeight.w500),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            );
          }).toList(),
        ),
      ),
    );
  }
}

// ── Action card (Invoices / Payments) ─────────────────────────────────────────

class _ActionCard extends StatelessWidget {
  const _ActionCard({
    required this.icon,
    required this.label,
    required this.subtitle,
    required this.color,
    required this.bg,
    required this.onTap,
  });

  final IconData icon;
  final String label;
  final String subtitle;
  final Color color;
  final Color bg;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 0,
      margin: EdgeInsets.zero,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(
          color: color.withValues(alpha: 0.25),
        ),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  color: bg,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(icon, color: color, size: 22),
              ),
              const SizedBox(height: 12),
              Text(
                label,
                style: TextStyle(
                  fontWeight: FontWeight.w700,
                  fontSize: 15,
                  color: color,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                subtitle,
                style: TextStyle(fontSize: 12, color: Colors.grey.shade600),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Payment summary card ──────────────────────────────────────────────────────

class _PaymentSummaryCard extends ConsumerStatefulWidget {
  const _PaymentSummaryCard({required this.customer, required this.summary});

  final Customer customer;
  final CustomerPaymentSummary summary;

  @override
  ConsumerState<_PaymentSummaryCard> createState() =>
      _PaymentSummaryCardState();
}

class _PaymentSummaryCardState extends ConsumerState<_PaymentSummaryCard> {
  bool _sending = false;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final s = widget.summary;
    final due = s.amountDue;
    final hasDue = due > 0;
    final dueColor = hasDue ? scheme.error : Colors.green.shade700;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Headline due amount
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: (hasDue ? scheme.errorContainer : Colors.green.shade50)
                    .withValues(alpha: 0.6),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Outstanding due', style: theme.textTheme.bodySmall),
                  const SizedBox(height: 4),
                  Text(
                    formatCurrency(due),
                    style: theme.textTheme.headlineMedium?.copyWith(
                        color: dueColor, fontWeight: FontWeight.bold),
                  ),
                  if (s.overdueInvoices > 0)
                    Text(
                      '${s.overdueInvoices} overdue invoice(s)',
                      style: TextStyle(color: scheme.error, fontSize: 12),
                    ),
                ],
              ),
            ),
            const SizedBox(height: 16),

            // Metric grid
            Wrap(
              spacing: 24,
              runSpacing: 16,
              children: [
                _Metric(label: 'Total paid', value: formatCurrency(s.totalPaid)),
                _Metric(
                    label: 'Total invoiced',
                    value: formatCurrency(s.totalInvoiceAmount)),
                _Metric(
                    label: 'Advance credit',
                    value: formatCurrency(s.availableAdvance)),
                _Metric(
                    label: 'Unpaid invoices', value: '${s.unpaidInvoices}'),
                if (s.lastPaymentDate != null)
                  _Metric(
                      label: 'Last payment',
                      value:
                          '${formatCurrency(s.lastPaymentAmount)}\n${formatDate(s.lastPaymentDate!)}'),
              ],
            ),
            const SizedBox(height: 16),

            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: () => _openRecordPaymentSheet(context),
                icon: const Icon(Icons.payments_outlined),
                label: const Text('Record Payment'),
              ),
            ),
            const SizedBox(height: 8),
            SizedBox(
              width: double.infinity,
              child: OutlinedButton.icon(
                onPressed: _sending ? null : _openReminderSheet,
                icon: _sending
                    ? const SizedBox(
                        width: 16,
                        height: 16,
                        child: CircularProgressIndicator(strokeWidth: 2))
                    : const Icon(Icons.notifications_active_outlined),
                label: Text(hasDue
                    ? 'Send payment reminder'
                    : 'Send a reminder'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _openRecordPaymentSheet(BuildContext context) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (sheetCtx) => _RecordPaymentSheet(customer: widget.customer),
    );
  }

  Future<void> _openReminderSheet() async {
    final customer = widget.customer;
    final channel = await showModalBottomSheet<String>(
      context: context,
      showDragHandle: true,
      builder: (sheetCtx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
              child: Text('Send reminder via',
                  style: Theme.of(sheetCtx).textTheme.titleMedium),
            ),
            ListTile(
              leading: const Icon(Icons.sms_outlined),
              title: const Text('SMS'),
              subtitle: Text(customer.phone ?? 'No phone number'),
              enabled: (customer.phone ?? '').isNotEmpty,
              onTap: () => Navigator.pop(sheetCtx, 'SMS'),
            ),
            ListTile(
              leading: const Icon(Icons.chat_outlined),
              title: const Text('WhatsApp'),
              subtitle: Text(customer.phone ?? 'No phone number'),
              enabled: (customer.phone ?? '').isNotEmpty,
              onTap: () => Navigator.pop(sheetCtx, 'WHATSAPP'),
            ),
            ListTile(
              leading: const Icon(Icons.email_outlined),
              title: const Text('Email'),
              subtitle: Text(customer.email ?? 'No email address'),
              enabled: (customer.email ?? '').isNotEmpty,
              onTap: () => Navigator.pop(sheetCtx, 'EMAIL'),
            ),
            const SizedBox(height: 8),
          ],
        ),
      ),
    );

    if (channel == null || !mounted) return;
    await _sendReminder(channel);
  }

  Future<void> _sendReminder(String channel) async {
    setState(() => _sending = true);
    final messenger = ScaffoldMessenger.of(context);
    final errorColor = Theme.of(context).colorScheme.error;
    try {
      final msg = await ref
          .read(customersRepositoryProvider)
          .sendPaymentReminder(
            customerId: widget.customer.id,
            channel: channel,
          );
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
}

// ── Shared helpers ────────────────────────────────────────────────────────────

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({required this.icon, required this.title});

  final IconData icon;
  final String title;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      children: [
        Icon(icon, size: 20, color: theme.colorScheme.primary),
        const SizedBox(width: 8),
        Text(title, style: theme.textTheme.titleMedium),
      ],
    );
  }
}

class _Metric extends StatelessWidget {
  const _Metric({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(label, style: theme.textTheme.bodySmall),
        const SizedBox(height: 2),
        Text(value,
            style: theme.textTheme.titleSmall
                ?.copyWith(fontWeight: FontWeight.bold)),
      ],
    );
  }
}

// ── Record Payment bottom sheet ───────────────────────────────────────────────

class _RecordPaymentSheet extends ConsumerStatefulWidget {
  const _RecordPaymentSheet({required this.customer});

  final Customer customer;

  @override
  ConsumerState<_RecordPaymentSheet> createState() =>
      _RecordPaymentSheetState();
}

class _RecordPaymentSheetState extends ConsumerState<_RecordPaymentSheet> {
  final _formKey = GlobalKey<FormState>();
  final _amountCtrl = TextEditingController();
  final _transactionCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();

  String _method = 'CASH';
  DateTime _date = DateTime.now();
  bool _submitting = false;

  bool _isAdvance = false;
  Invoice? _selectedInvoice;
  bool _invoiceError = false;
  final List<Invoice> _loadedInvoices = [];
  bool _loadingInvoices = false;
  bool _invoicesLoaded = false;

  @override
  void initState() {
    super.initState();
    if (widget.customer.dueAmount > 0) {
      _amountCtrl.text =
          widget.customer.dueAmount.toStringAsFixed(2);
    }
    _loadInvoices();
  }

  @override
  void dispose() {
    _amountCtrl.dispose();
    _transactionCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadInvoices() async {
    if (_invoicesLoaded || _loadingInvoices) return;
    setState(() => _loadingInvoices = true);
    try {
      final page = await ref.read(customersRepositoryProvider).invoicesPage(
            customerId: widget.customer.id,
            pageSize: 50,
          );
      if (!mounted) return;
      setState(() {
        _loadedInvoices
            .addAll(page.items.where((i) => i.outstandingAmount > 0));
        _invoicesLoaded = true;
        _loadingInvoices = false;
      });
    } catch (_) {
      if (mounted) {
        setState(() {
          _invoicesLoaded = true;
          _loadingInvoices = false;
        });
      }
    }
  }

  Future<void> _pickInvoice() async {
    if (_loadingInvoices) return;
    final selected = await showModalBottomSheet<Invoice>(
      context: context,
      showDragHandle: true,
      isScrollControlled: true,
      builder: (ctx) => _InvoicePickerSheet(invoices: _loadedInvoices),
    );
    if (!mounted || selected == null) return;
    setState(() {
      _selectedInvoice = selected;
      _invoiceError = false;
      _amountCtrl.text = selected.outstandingAmount.toStringAsFixed(2);
    });
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    if (!_isAdvance && _selectedInvoice == null) {
      setState(() => _invoiceError = true);
      return;
    }
    // Dismiss keyboard before the async call — prevents FocusScopeNode
    // disposal errors when the sheet is popped while a field has focus.
    FocusScope.of(context).unfocus();
    setState(() => _submitting = true);
    // Capture context-derived values before any await.
    final messenger = ScaffoldMessenger.of(context);
    final nav = Navigator.of(context);
    final errorColor = Theme.of(context).colorScheme.error;
    try {
      await ref.read(customersRepositoryProvider).recordPayment(
            customerId: widget.customer.id,
            amount: double.parse(
                _amountCtrl.text.trim().replaceAll(',', '')),
            paymentMethod: _method,
            transactionNumber: _transactionCtrl.text.trim().isEmpty
                ? null
                : _transactionCtrl.text.trim(),
            paymentDate: _date,
            notes: _notesCtrl.text.trim().isEmpty
                ? null
                : _notesCtrl.text.trim(),
            invoiceId: _isAdvance ? null : _selectedInvoice?.id,
            isAdvance: _isAdvance,
          );
      ref.invalidate(customerDetailProvider(widget.customer.id));
      ref.invalidate(customerPaymentSummaryProvider(widget.customer.id));
      if (!mounted) return;
      nav.pop();
      messenger.showSnackBar(
        const SnackBar(content: Text('Payment recorded successfully')),
      );
    } on AppException catch (e) {
      if (!mounted) return;
      messenger.showSnackBar(SnackBar(
        content: Text(e.message),
        backgroundColor: errorColor,
      ));
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime(2020),
      lastDate: DateTime.now(),
    );
    if (!mounted || picked == null) return;
    setState(() => _date = picked);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return Padding(
      padding:
          EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(context).bottom),
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(20, 4, 20, 28),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                'Record Payment',
                style: theme.textTheme.titleLarge
                    ?.copyWith(fontWeight: FontWeight.w700),
              ),
              const SizedBox(height: 2),
              Text(
                widget.customer.fullName,
                style: theme.textTheme.bodyMedium
                    ?.copyWith(color: scheme.onSurfaceVariant),
              ),
              const SizedBox(height: 20),

              // Payment type toggle
              Text(
                'Payment Type',
                style: theme.textTheme.labelMedium
                    ?.copyWith(color: scheme.onSurfaceVariant),
              ),
              const SizedBox(height: 8),
              SegmentedButton<bool>(
                segments: const [
                  ButtonSegment(
                      value: false, label: Text('Against Invoice')),
                  ButtonSegment(
                      value: true, label: Text('Advance Payment')),
                ],
                selected: {_isAdvance},
                onSelectionChanged: (s) => setState(() {
                  _isAdvance = s.first;
                  if (_isAdvance) {
                    _selectedInvoice = null;
                    _invoiceError = false;
                  }
                }),
              ),
              const SizedBox(height: 16),

              // Invoice picker (hidden when advance)
              if (!_isAdvance) ...[
                GestureDetector(
                  onTap: _loadingInvoices ? null : _pickInvoice,
                  child: InputDecorator(
                    decoration: InputDecoration(
                      labelText: 'Invoice',
                      border: const OutlineInputBorder(),
                      errorText: _invoiceError
                          ? 'Select an invoice or switch to Advance Payment'
                          : null,
                      suffixIcon: _loadingInvoices
                          ? const Padding(
                              padding: EdgeInsets.all(14),
                              child: SizedBox.square(
                                dimension: 16,
                                child: CircularProgressIndicator(
                                    strokeWidth: 2),
                              ))
                          : const Icon(Icons.chevron_right),
                    ),
                    child: _selectedInvoice == null
                        ? Text(
                            _invoicesLoaded && _loadedInvoices.isEmpty
                                ? 'No open invoices'
                                : 'Select invoice (optional)',
                            style: TextStyle(color: scheme.onSurfaceVariant),
                          )
                        : Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Text(
                                _selectedInvoice!.invoiceNumber,
                                style: const TextStyle(
                                    fontWeight: FontWeight.w600),
                              ),
                              Text(
                                '${formatCurrency(_selectedInvoice!.outstandingAmount)} outstanding',
                                style: TextStyle(
                                    fontSize: 12,
                                    color: scheme.onSurfaceVariant),
                              ),
                            ],
                          ),
                  ),
                ),
                const SizedBox(height: 16),
              ],

              // Amount
              TextFormField(
                controller: _amountCtrl,
                keyboardType: const TextInputType.numberWithOptions(
                    decimal: true),
                style: const TextStyle(
                    fontSize: 22, fontWeight: FontWeight.w700),
                decoration: const InputDecoration(
                  labelText: 'Amount',
                  prefixText: '৳ ',
                  border: OutlineInputBorder(),
                ),
                validator: (v) {
                  if (v == null || v.isEmpty) return 'Enter an amount';
                  final n =
                      double.tryParse(v.trim().replaceAll(',', ''));
                  if (n == null || n <= 0) {
                    return 'Enter a valid amount';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // Payment method
              Text(
                'Payment Method',
                style: theme.textTheme.labelMedium
                    ?.copyWith(color: scheme.onSurfaceVariant),
              ),
              const SizedBox(height: 8),
              SegmentedButton<String>(
                segments: const [
                  ButtonSegment(value: 'CASH', label: Text('Cash')),
                  ButtonSegment(value: 'BKASH', label: Text('bKash')),
                  ButtonSegment(
                      value: 'BANK_TRANSFER', label: Text('Bank')),
                  ButtonSegment(
                      value: 'CHEQUE', label: Text('Cheque')),
                ],
                selected: {_method},
                onSelectionChanged: (s) =>
                    setState(() => _method = s.first),
              ),
              const SizedBox(height: 16),

              // Transaction number — hidden for cash
              if (_method != 'CASH') ...[
                TextFormField(
                  controller: _transactionCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Transaction / Reference Number',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 16),
              ],

              // Payment date
              GestureDetector(
                onTap: _pickDate,
                child: InputDecorator(
                  decoration: const InputDecoration(
                    labelText: 'Payment Date',
                    border: OutlineInputBorder(),
                    suffixIcon:
                        Icon(Icons.calendar_today_outlined, size: 18),
                  ),
                  child: Text(formatDate(_date)),
                ),
              ),
              const SizedBox(height: 16),

              // Notes
              TextFormField(
                controller: _notesCtrl,
                maxLines: 2,
                decoration: const InputDecoration(
                  labelText: 'Notes (optional)',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 24),

              // Submit
              SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  onPressed: _submitting ? null : _submit,
                  icon: _submitting
                      ? const SizedBox(
                          width: 16,
                          height: 16,
                          child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white))
                      : const Icon(Icons.check_circle_outlined),
                  label: Text(
                      _submitting ? 'Recording...' : 'Confirm Payment'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Invoice picker sheet ──────────────────────────────────────────────────────

class _InvoicePickerSheet extends StatelessWidget {
  const _InvoicePickerSheet({required this.invoices});

  final List<Invoice> invoices;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return SafeArea(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
            child: Text('Select Invoice', style: theme.textTheme.titleMedium),
          ),
          if (invoices.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  Icon(Icons.receipt_long_outlined,
                      size: 48, color: scheme.onSurfaceVariant),
                  const SizedBox(height: 12),
                  Center(
                    child: Text(
                      'No open invoices',
                      style: theme.textTheme.titleSmall,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Center(
                    child: Text(
                      'All invoices are paid in full.',
                      style: TextStyle(color: scheme.onSurfaceVariant),
                    ),
                  ),
                ],
              ),
            )
          else
            Flexible(
              child: ListView.separated(
                shrinkWrap: true,
                itemCount: invoices.length,
                separatorBuilder: (_, sep) => const Divider(height: 1),
                itemBuilder: (ctx, i) {
                  final inv = invoices[i];
                  return ListTile(
                    title: Text(inv.invoiceNumber,
                        style: const TextStyle(fontWeight: FontWeight.w600)),
                    subtitle: Text(
                        '${formatDate(inv.invoiceDate)} · ${formatCurrency(inv.outstandingAmount)} due'),
                    trailing: inv.isOverdue
                        ? Chip(
                            label: const Text('Overdue',
                                style: TextStyle(fontSize: 11)),
                            backgroundColor: scheme.errorContainer,
                            side: BorderSide.none,
                            materialTapTargetSize:
                                MaterialTapTargetSize.shrinkWrap,
                            padding:
                                const EdgeInsets.symmetric(horizontal: 4),
                          )
                        : null,
                    onTap: () => Navigator.of(ctx).pop(inv),
                  );
                },
              ),
            ),
          const SizedBox(height: 8),
        ],
      ),
    );
  }
}
