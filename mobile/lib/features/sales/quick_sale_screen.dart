import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../core/i18n/strings.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/sale.dart';
import '../../shared/widgets/state_views.dart';
import '../till_session/till_session_repository.dart';
import 'charge_screen.dart';
import 'held_sales_controller.dart';
import 'quick_sale_providers.dart';

class QuickSaleScreen extends ConsumerStatefulWidget {
  const QuickSaleScreen({super.key});

  @override
  ConsumerState<QuickSaleScreen> createState() => _QuickSaleScreenState();
}

class _QuickSaleScreenState extends ConsumerState<QuickSaleScreen> {
  final MobileScannerController _scanner = MobileScannerController(
    detectionSpeed: DetectionSpeed.noDuplicates,
  );
  bool _handling = false;

  @override
  void dispose() {
    _scanner.dispose();
    super.dispose();
  }

  Future<void> _onDetect(BarcodeCapture capture) async {
    if (_handling) return;
    final code = capture.barcodes
        .map((b) => b.rawValue)
        .firstWhere((v) => v != null && v.isNotEmpty, orElse: () => null);
    if (code == null) return;
    setState(() => _handling = true);
    await _scanner.stop();
    await ref.read(quickSaleControllerProvider.notifier).lookupByCode(code);
    if (!mounted) return;
    setState(() => _handling = false);
    if (ref.read(quickSaleControllerProvider).isScanning) {
      try {
        await _scanner.start();
      } on MobileScannerException {
        // errorBuilder handles error UI
      }
    }
  }

  void _startScan() =>
      ref.read(quickSaleControllerProvider.notifier).startScan();

  Future<void> _stopScan() async {
    await _scanner.stop();
    ref.read(quickSaleControllerProvider.notifier).stopScan();
  }

  void _openChargeScreen() {
    final total = ref.read(quickSaleControllerProvider).total;
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => ChargeScreen(cartTotal: total),
      ),
    );
  }

  /// Sends the cashier to open a till session, then re-checks the gate on
  /// return so they land back on the (now unblocked) cart automatically.
  Future<void> _openTillSession() async {
    await context.push('/till-session');
    if (!mounted) return;
    ref.invalidate(tillSessionRequirementProvider);
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(quickSaleControllerProvider);
    final controller = ref.read(quickSaleControllerProvider.notifier);

    if (state.result != null) {
      return Scaffold(
        appBar: _buildAppBar(context, itemCount: 0),
        body: _SuccessView(
            result: state.result!, onNewSale: controller.reset),
      );
    }

    // Till-session gate: opt-in per role via the `sales.require-till-session`
    // permission (most roles won't have it, so `requirementAsync` normally
    // resolves to a no-op `required: false` and nothing below changes). This
    // only blocks the UX early so a cashier doesn't build a whole cart before
    // hitting the server's 400 at checkout — the backend enforces the same
    // rule regardless (see SalesOrderController.CreateQuickSale's till
    // session gate), so a slow/failed check here fails OPEN rather than
    // trapping a sale the server would otherwise allow.
    final requirementAsync = ref.watch(tillSessionRequirementProvider);
    if (requirementAsync.isLoading && !requirementAsync.hasValue) {
      return Scaffold(
        appBar: _buildAppBar(context, itemCount: 0),
        body: const LoadingView(),
      );
    }
    if (requirementAsync.asData?.value.blocksSale ?? false) {
      return Scaffold(
        appBar: _buildAppBar(context, itemCount: 0),
        body: _TillSessionRequiredView(onOpenTill: _openTillSession),
      );
    }

    if (state.isScanning) {
      // System back exits scan mode instead of popping the route — when the
      // cart was opened via the bottom-nav ＋ (a go(), stack root), a pop here
      // would close the whole app.
      return PopScope(
        canPop: false,
        onPopInvokedWithResult: (didPop, _) {
          if (!didPop) _stopScan();
        },
        child: Scaffold(
          backgroundColor: Colors.black,
          body: _buildScannerOverlay(state),
        ),
      );
    }

    // When the cart is the stack root (opened via the bottom-nav ＋), system
    // back returns Home — mirroring the app bar's back arrow — instead of
    // closing the app.
    return PopScope(
      canPop: context.canPop(),
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) context.go('/');
      },
      child: Scaffold(
        appBar: _buildAppBar(context, itemCount: state.itemCount),
        body: state.isEmpty
            ? _EmptyCartView(onScan: _startScan)
            : _buildCartBody(state, controller),
      ),
    );
  }

  PreferredSizeWidget _buildAppBar(BuildContext context, {required int itemCount}) {
    return AppBar(
      leading: IconButton(
        icon: const Icon(Icons.arrow_back),
        onPressed: () => context.canPop() ? context.pop() : context.go('/'),
      ),
      title: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            S.of(context).cart,
            style: GoogleFonts.instrumentSans(
              fontSize: 17,
              fontWeight: FontWeight.w700
            ),
          ),
          if (itemCount > 0)
            Text(
              S.of(context).invDraftItems(itemCount),
              style: GoogleFonts.instrumentSans(
                fontSize: 12
              ),
            ),
        ],
      ),
      actions: [
        // Held carts — badge shows how many are parked.
        Consumer(builder: (context, ref, _) {
          final heldCount = ref.watch(heldSalesProvider).length;
          return Stack(
            alignment: Alignment.center,
            children: [
              IconButton(
                icon: const Icon(Icons.inventory_2_outlined),
                tooltip: S.of(context).heldSales,
                onPressed: _openHeldSheet,
              ),
              if (heldCount > 0)
                Positioned(
                  top: 8,
                  right: 6,
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 5),
                    constraints: const BoxConstraints(minWidth: 16),
                    height: 16,
                    decoration: BoxDecoration(
                      color: context.colors.red,
                      borderRadius: BorderRadius.circular(99),
                    ),
                    alignment: Alignment.center,
                    child: Text('$heldCount',
                        style: GoogleFonts.instrumentSans(
                            fontSize: 9.5,
                            fontWeight: FontWeight.w700,
                            color: context.colors.onInk)),
                  ),
                ),
            ],
          );
        }),
        if (itemCount > 0)
          IconButton(
            icon: const Icon(Icons.pause_circle_outline),
            tooltip: S.of(context).holdSale,
            onPressed: _holdCurrent,
          ),
        IconButton(
          icon: const Icon(Icons.qr_code_scanner),
          tooltip: S.of(context).scanBarcode,
          onPressed: _startScan,
        ),
      ],
    );
  }

  Future<void> _holdCurrent() async {
    final items = ref.read(quickSaleControllerProvider).items;
    if (items.isEmpty) return;
    final label = await showDialog<String>(
      context: context,
      builder: (ctx) => _HoldDialog(),
    );
    if (label == null || !mounted) return; // cancelled
    await ref.read(heldSalesProvider.notifier).hold(label, items);
    ref.read(quickSaleControllerProvider.notifier).reset();
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
      content: Text(S.of(context).saleHeld),
      duration: const Duration(seconds: 2),
      behavior: SnackBarBehavior.floating,
    ));
  }

  void _openHeldSheet() {
    showModalBottomSheet<void>(
      context: context,
      useSafeArea: true,
      isScrollControlled: true,
      builder: (_) => _HeldSalesSheet(
        onResume: (cart) {
          final current = ref.read(quickSaleControllerProvider).items;
          if (current.isNotEmpty) {
            // Park the current cart before loading the resumed one so nothing
            // is lost.
            ref
                .read(heldSalesProvider.notifier)
                .hold(S.of(context).autoHeld, current);
          }
          ref.read(quickSaleControllerProvider.notifier).loadItems(cart.items);
          ref.read(heldSalesProvider.notifier).remove(cart.id);
        },
      ),
    );
  }

  Widget _buildCartBody(
      QuickSaleState state, QuickSaleController controller) {
    return Stack(
      children: [
        ListView(
          padding: const EdgeInsets.fromLTRB(16, 4, 16, 160),
          children: [
            // ── Line items card ──────────────────────────────────────
            Container(
              decoration: BoxDecoration(
                color: Theme.of(context).colorScheme.surface,
                borderRadius: BorderRadius.circular(13),
                border: Border.all(color: Theme.of(context).colorScheme.outline),
              ),
              clipBehavior: Clip.antiAlias,
              child: Column(
                children: [
                  ...state.items.asMap().entries.map((e) {
                    final i = e.key;
                    final item = e.value;
                    return Column(
                      children: [
                        if (i > 0)
                          Divider(height: 1, color: Theme.of(context).colorScheme.outline.withAlpha(60)),
                        _CartItemRow(
                          key: ValueKey(
                              '${item.partId}${item.variantId}'),
                          item: item,
                          onIncrement: () {
                            final ok = controller.increment(
                                item.partId, item.variantId);
                            if (!ok) {
                              ScaffoldMessenger.of(context).showSnackBar(
                                SnackBar(
                                  content: Text(S.of(context).onlyNInStock(
                                      item.availableStock ?? 0, item.name)),
                                  backgroundColor: context.colors.red,
                                  duration: const Duration(seconds: 2),
                                  behavior: SnackBarBehavior.floating,
                                ),
                              );
                            }
                          },
                          onDecrement: () => controller.decrement(
                              item.partId, item.variantId),
                          onRemove: () =>
                              controller.remove(item.partId, item.variantId),
                          onPriceChanged: (p) => controller.updatePrice(
                              item.partId, item.variantId, p),
                        ),
                      ],
                    );
                  }),

                  // Add more row
                  InkWell(
                    onTap: _startScan,
                    child: Padding(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 14, vertical: 12),
                      child: Row(
                        children: [
                          Container(
                            width: 28,
                            height: 28,
                            decoration: BoxDecoration(
                              color: Theme.of(context).colorScheme.outline.withAlpha(60),
                              borderRadius: BorderRadius.circular(8),
                            ),
                            alignment: Alignment.center,
                            child: Icon(Icons.add,
                                size: 18, color: context.colors.secondary),
                          ),
                          const SizedBox(width: 12),
                          Text(
                            S.of(context).addMoreItems,
                            style: GoogleFonts.instrumentSans(
                              fontSize: 13.5
                            ),
                          ),
                          const Spacer(),
                          Icon(Icons.qr_code_scanner,
                              size: 18, color: context.colors.muted),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),

            // ── Totals card ──────────────────────────────────────────
            Container(
              decoration: BoxDecoration(
                color: Theme.of(context).colorScheme.surface,
                borderRadius: BorderRadius.circular(13),
                border: Border.all(color: Theme.of(context).colorScheme.outline),
              ),
              padding: const EdgeInsets.fromLTRB(13, 14, 13, 14),
              child: Column(
                children: [
                  _TotalRow(
                    label: S.of(context).subtotalItems(state.itemCount),
                    value: formatCurrency(state.total),
                    labelStyle: GoogleFonts.instrumentSans(
                        fontSize: 13.5, color: context.colors.secondary),
                    valueStyle: GoogleFonts.instrumentSans(
                        fontSize: 13.5,
                        fontWeight: FontWeight.w600,
                        color: context.colors.ink),
                  ),
                  const SizedBox(height: 8),
                  _TotalRow(
                    label: S.of(context).discount,
                    value: '—',
                    labelStyle: GoogleFonts.instrumentSans(
                        fontSize: 13.5, color: context.colors.secondary),
                    valueStyle: GoogleFonts.instrumentSans(
                        fontSize: 13.5,
                        color: context.colors.muted),
                  ),
                  Padding(
                    padding: EdgeInsets.symmetric(vertical: 10),
                    child: Divider(
                        height: 1,
                        color: Theme.of(context).colorScheme.outline.withAlpha(60)),
                  ),
                  _TotalRow(
                    label: S.of(context).total,
                    value: formatCurrency(state.total),
                    labelStyle: GoogleFonts.instrumentSans(
                        fontSize: 13.5,
                        fontWeight: FontWeight.w700,
                        color: context.colors.ink),
                    valueStyle: GoogleFonts.instrumentSans(
                        fontSize: 20,
                        fontWeight: FontWeight.w700,
                        color: context.colors.ink),
                  ),
                ],
              ),
            ),
          ],
        ),

        // Sticky bottom CTA
        Positioned(
          bottom: 0,
          left: 0,
          right: 0,
          child: _StickyBottom(
            total: state.total,
            onProceed: _openChargeScreen,
          ),
        ),
      ],
    );
  }

  Widget _buildScannerOverlay(QuickSaleState state) {
    return Stack(
      alignment: Alignment.center,
      children: [
        MobileScanner(
          controller: _scanner,
          onDetect: _onDetect,
          errorBuilder: _buildCameraError,
        ),
        Container(
          width: 260,
          height: 160,
          decoration: BoxDecoration(
            border: Border.all(color: Colors.white, width: 2.5),
            borderRadius: BorderRadius.circular(12),
          ),
        ),
        Positioned(
          top: 48,
          left: 12,
          child: _ScanIconButton(
              icon: Icons.close,
              tooltip: S.of(context).cancel,
              onTap: _stopScan),
        ),
        Positioned(
          top: 48,
          right: 12,
          child: _ScanIconButton(
            icon: Icons.flash_on,
            tooltip: S.of(context).torch,
            onTap: () => _scanner.toggleTorch(),
          ),
        ),
        Positioned(
          bottom: 80,
          child: Text(
            S.of(context).pointCameraAtBarcode,
            style: const TextStyle(color: Colors.white, fontSize: 15),
          ),
        ),
        if (_handling || state.isLookingUp)
          const Positioned(
            bottom: 36,
            child: CircularProgressIndicator(color: Colors.white),
          ),
        if (state.lookupError != null && !_handling)
          Positioned(
            bottom: 28,
            left: 24,
            right: 24,
            child: Container(
              padding: const EdgeInsets.symmetric(
                  horizontal: 16, vertical: 12),
              decoration: BoxDecoration(
                color: context.colors.red,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Text(
                state.lookupError!,
                textAlign: TextAlign.center,
                style: const TextStyle(color: Colors.white),
              ),
            ),
          ),
      ],
    );
  }

  Widget _buildCameraError(
      BuildContext context, MobileScannerException error) {
    final isPermission =
        error.errorCode == MobileScannerErrorCode.permissionDenied;
    return Container(
      color: Colors.black,
      alignment: Alignment.center,
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            isPermission ? Icons.no_photography : Icons.error_outline,
            color: Colors.white70,
            size: 64,
          ),
          const SizedBox(height: 16),
          Text(
            isPermission
                ? S.of(context).cameraAccessNeeded
                : S.of(context).cameraError,
            textAlign: TextAlign.center,
            style: const TextStyle(
                color: Colors.white,
                fontSize: 18,
                fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 8),
          Text(
            isPermission
                ? S.of(context).allowCameraAccess
                : (error.errorDetails?.message ??
                    S.of(context).couldNotStartCamera),
            textAlign: TextAlign.center,
            style: const TextStyle(color: Colors.white70),
          ),
          const SizedBox(height: 20),
          FilledButton.icon(
            onPressed: () async {
              try {
                await _scanner.start();
              } on MobileScannerException {
                // errorBuilder will rebuild
              }
            },
            icon: const Icon(Icons.refresh),
            label: Text(S.of(context).tryAgain),
          ),
        ],
      ),
    );
  }
}

// ── Empty cart view ───────────────────────────────────────────────────────────

class _EmptyCartView extends StatelessWidget {
  const _EmptyCartView({required this.onScan});

  final VoidCallback onScan;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.shopping_cart_outlined,
                size: 72, color: context.colors.disabled),
            const SizedBox(height: 16),
            Text(
              S.of(context).cartIsEmpty,
              style: GoogleFonts.instrumentSans(
                fontSize: 18,
                fontWeight: FontWeight.w700
              ),
            ),
            const SizedBox(height: 8),
            Text(
              S.of(context).scanOrAddFromProducts,
              textAlign: TextAlign.center,
              style: GoogleFonts.instrumentSans(
                  fontSize: 13.5, color: context.colors.muted),
            ),
            const SizedBox(height: 32),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: onScan,
                icon: const Icon(Icons.qr_code_scanner),
                label: Text(S.of(context).scanBarcode),
                style: FilledButton.styleFrom(
                  backgroundColor: context.colors.ink,
                  foregroundColor: context.colors.onInk,
                  shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12)),
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  textStyle: GoogleFonts.instrumentSans(
                      fontSize: 15, fontWeight: FontWeight.w600),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Till session required (blocking gate) ───────────────────────────────────

class _TillSessionRequiredView extends StatelessWidget {
  const _TillSessionRequiredView({required this.onOpenTill});

  final VoidCallback onOpenTill;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.lock_clock_outlined,
                size: 72, color: context.colors.disabled),
            const SizedBox(height: 16),
            Text(
              S.of(context).openTillToSell,
              textAlign: TextAlign.center,
              style: GoogleFonts.instrumentSans(
                fontSize: 18,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              S.of(context).tillRequiredBody,
              textAlign: TextAlign.center,
              style: GoogleFonts.instrumentSans(
                  fontSize: 13.5, color: context.colors.muted),
            ),
            const SizedBox(height: 32),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: onOpenTill,
                icon: const Icon(Icons.lock_open_outlined),
                label: Text(S.of(context).openTillSession),
                style: FilledButton.styleFrom(
                  backgroundColor: context.colors.ink,
                  foregroundColor: context.colors.onInk,
                  shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12)),
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  textStyle: GoogleFonts.instrumentSans(
                      fontSize: 15, fontWeight: FontWeight.w600),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Cart item row ─────────────────────────────────────────────────────────────

class _CartItemRow extends StatefulWidget {
  const _CartItemRow({
    super.key,
    required this.item,
    required this.onIncrement,
    required this.onDecrement,
    required this.onRemove,
    required this.onPriceChanged,
  });

  final QuickSaleItem item;
  final VoidCallback onIncrement;
  final VoidCallback onDecrement;
  final VoidCallback onRemove;
  final void Function(double) onPriceChanged;

  @override
  State<_CartItemRow> createState() => _CartItemRowState();
}

class _CartItemRowState extends State<_CartItemRow> {
  late final TextEditingController _priceCtrl;
  final FocusNode _priceFocus = FocusNode();

  @override
  void initState() {
    super.initState();
    _priceCtrl = TextEditingController(
        text: widget.item.unitPrice.toStringAsFixed(2));
    _priceFocus.addListener(() {
      if (!_priceFocus.hasFocus) _commit();
    });
  }

  @override
  void didUpdateWidget(_CartItemRow old) {
    super.didUpdateWidget(old);
    if (!_priceFocus.hasFocus &&
        old.item.unitPrice != widget.item.unitPrice) {
      _priceCtrl.text = widget.item.unitPrice.toStringAsFixed(2);
    }
  }

  @override
  void dispose() {
    _priceCtrl.dispose();
    _priceFocus.dispose();
    super.dispose();
  }

  void _commit() {
    final val = double.tryParse(_priceCtrl.text);
    if (val != null && val > 0) {
      widget.onPriceChanged(val);
    } else {
      _priceCtrl.text = widget.item.unitPrice.toStringAsFixed(2);
    }
  }

  @override
  Widget build(BuildContext context) {
    final item = widget.item;
    return Padding(
      padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          // Name + price
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.name,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.instrumentSans(
                    fontSize: 13.5,
                    fontWeight: FontWeight.w500
                  ),
                ),
                const SizedBox(height: 3),
                Row(
                  children: [
                    Text(
                      kCurrencyPrefix,
                      style: GoogleFonts.instrumentSans(
                        fontSize: 12
                      ),
                    ),
                    SizedBox(
                      width: 72,
                      child: TextField(
                        controller: _priceCtrl,
                        focusNode: _priceFocus,
                        keyboardType: const TextInputType.numberWithOptions(
                            decimal: true),
                        inputFormatters: [
                          FilteringTextInputFormatter.allow(
                              RegExp(r'[0-9.]')),
                        ],
                        onSubmitted: (_) => _commit(),
                        style: GoogleFonts.instrumentSans(
                          fontSize: 12,
                          fontWeight: FontWeight.w600
                        ),
                        decoration: InputDecoration(
                          isDense: true,
                          contentPadding: EdgeInsets.zero,
                          border: UnderlineInputBorder(
                              borderSide:
                                  BorderSide(color: Theme.of(context).colorScheme.outline)),
                          focusedBorder: UnderlineInputBorder(
                              borderSide: BorderSide(
                                  width: 1.5)),
                        ),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),

          // Qty stepper
          Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              _StepBtn(icon: Icons.remove, onTap: widget.onDecrement),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 12),
                child: Text(
                  '${item.quantity}',
                  style: GoogleFonts.instrumentSans(
                    fontSize: 15,
                    fontWeight: FontWeight.w700
                  ),
                ),
              ),
              _StepBtn(icon: Icons.add, onTap: widget.onIncrement),
            ],
          ),

          const SizedBox(width: 12),

          // Line total + delete
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                formatCurrency(item.lineTotal),
                style: GoogleFonts.instrumentSans(
                  fontSize: 13.5,
                  fontWeight: FontWeight.w600
                ),
              ),
              GestureDetector(
                onTap: widget.onRemove,
                child: Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Icon(Icons.delete_outline,
                      size: 16, color: context.colors.muted),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _StepBtn extends StatelessWidget {
  const _StepBtn({required this.icon, required this.onTap});

  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 30,
        height: 30,
        decoration: BoxDecoration(
          color: Theme.of(context).scaffoldBackgroundColor,
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: Theme.of(context).colorScheme.outline),
        ),
        alignment: Alignment.center,
        child: Icon(icon, size: 16, color: context.colors.secondary),
      ),
    );
  }
}

// ── Total row ─────────────────────────────────────────────────────────────────

class _TotalRow extends StatelessWidget {
  const _TotalRow({
    required this.label,
    required this.value,
    required this.labelStyle,
    required this.valueStyle,
  });

  final String label;
  final String value;
  final TextStyle labelStyle;
  final TextStyle valueStyle;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(label, style: labelStyle),
        Text(value, style: valueStyle),
      ],
    );
  }
}

// ── Sticky bottom CTA ─────────────────────────────────────────────────────────

class _StickyBottom extends StatelessWidget {
  const _StickyBottom({required this.total, required this.onProceed});

  final double total;
  final VoidCallback onProceed;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: [
            Theme.of(context).scaffoldBackgroundColor.withValues(alpha: 0),
            Theme.of(context).scaffoldBackgroundColor,
          ],
        ),
      ),
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 16),
      child: SafeArea(
        top: false,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            // Complete sale button (green)
            SizedBox(
              width: double.infinity,
              height: 52,
              child: DecoratedBox(
                decoration: BoxDecoration(
                  boxShadow: const [
                    BoxShadow(
                      color: Color(0x400D8A53),
                      blurRadius: 24,
                      offset: Offset(0, 8),
                    ),
                  ],
                  borderRadius: BorderRadius.circular(14),
                ),
                child: FilledButton(
                  onPressed: onProceed,
                  style: FilledButton.styleFrom(
                    backgroundColor: context.colors.green,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(14)),
                    padding: const EdgeInsets.symmetric(vertical: 15),
                    textStyle: GoogleFonts.instrumentSans(
                        fontSize: 15, fontWeight: FontWeight.w700),
                  ),
                  child: Text(S
                      .of(context)
                      .completeSaleWith(formatCurrency(total))),
                ),
              ),
            ),
            const SizedBox(height: 8),

            // Secondary row
            Row(
              children: [
                Expanded(
                  child: OutlinedButton(
                    onPressed: () {},
                    style: OutlinedButton.styleFrom(
                      foregroundColor: context.colors.ink,
                      side: BorderSide(color: Theme.of(context).colorScheme.outline),
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(11)),
                      padding: const EdgeInsets.symmetric(vertical: 12),
                      textStyle: GoogleFonts.instrumentSans(
                          fontSize: 13, fontWeight: FontWeight.w600),
                    ),
                    child: Text(S.of(context).holdSale),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: OutlinedButton(
                    onPressed: () {},
                    style: OutlinedButton.styleFrom(
                      foregroundColor: context.colors.ink,
                      side: BorderSide(color: Theme.of(context).colorScheme.outline),
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(11)),
                      padding: const EdgeInsets.symmetric(vertical: 12),
                      textStyle: GoogleFonts.instrumentSans(
                          fontSize: 13, fontWeight: FontWeight.w600),
                    ),
                    child: Text(S.of(context).print80mm),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

// ── Scan icon button ──────────────────────────────────────────────────────────

class _ScanIconButton extends StatelessWidget {
  const _ScanIconButton(
      {required this.icon,
      required this.tooltip,
      required this.onTap});

  final IconData icon;
  final String tooltip;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: tooltip,
      child: GestureDetector(
        onTap: onTap,
        child: Container(
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            color: Colors.black54,
            borderRadius: BorderRadius.circular(22),
          ),
          alignment: Alignment.center,
          child: Icon(icon, color: Colors.white, size: 22),
        ),
      ),
    );
  }
}

// ── Success view ──────────────────────────────────────────────────────────────

class _SuccessView extends StatelessWidget {
  const _SuccessView({required this.result, required this.onNewSale});

  final QuickSaleResult result;
  final VoidCallback onNewSale;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 80,
              height: 80,
              decoration: BoxDecoration(
                  color: context.colors.greenBg, shape: BoxShape.circle),
              child: Icon(Icons.check_circle_rounded,
                  size: 52, color: context.colors.green),
            ),
            const SizedBox(height: 20),
            Text(
              S.of(context).saleComplete,
              style: GoogleFonts.instrumentSans(
                fontSize: 22,
                fontWeight: FontWeight.w800
              ),
            ),
            const SizedBox(height: 20),
            _InfoRow(
                label: S.of(context).invoice, value: result.invoiceNumber),
            const SizedBox(height: 6),
            _InfoRow(
                label: S.of(context).total,
                value: formatCurrency(result.grandTotal)),
            if (result.hasDue) ...[
              const SizedBox(height: 6),
              _InfoRow(
                  label: S.of(context).paid,
                  value: formatCurrency(result.paidAmount)),
              const SizedBox(height: 4),
              _InfoRow(
                label: S.of(context).due,
                value: formatCurrency(result.dueAmount),
                valueColor: context.colors.red,
              ),
            ],
            const SizedBox(height: 40),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: onNewSale,
                icon: const Icon(Icons.add_shopping_cart),
                label: Text(S.of(context).newSale),
                style: FilledButton.styleFrom(
                  backgroundColor: context.colors.ink,
                  foregroundColor: context.colors.onInk,
                  shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12)),
                  padding: const EdgeInsets.symmetric(vertical: 15),
                  textStyle: GoogleFonts.instrumentSans(
                      fontSize: 15, fontWeight: FontWeight.w700),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow(
      {required this.label, required this.value, this.valueColor});

  final String label;
  final String value;
  final Color? valueColor;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Text(
          '$label: ',
          style: GoogleFonts.instrumentSans(
              fontSize: 15, color: context.colors.muted),
        ),
        Text(
          value,
          style: GoogleFonts.instrumentSans(
            fontSize: 15,
            fontWeight: FontWeight.w700,
            color: valueColor ?? context.colors.ink,
          ),
        ),
      ],
    );
  }
}

// ── Hold dialog ───────────────────────────────────────────────────────────────

/// Prompts for an optional label when parking a cart. Returns the label (may be
/// empty for the default) or null when cancelled.
class _HoldDialog extends StatefulWidget {
  @override
  State<_HoldDialog> createState() => _HoldDialogState();
}

class _HoldDialogState extends State<_HoldDialog> {
  final _ctrl = TextEditingController();

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(S.of(context).holdSale),
      content: TextField(
        controller: _ctrl,
        autofocus: true,
        textCapitalization: TextCapitalization.words,
        decoration: InputDecoration(
          labelText: S.of(context).labelOptional,
          hintText: S.of(context).holdLabelHint,
        ),
        onSubmitted: (v) => Navigator.of(context).pop(v),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: Text(S.of(context).cancel),
        ),
        FilledButton(
          onPressed: () => Navigator.of(context).pop(_ctrl.text),
          child: Text(S.of(context).hold),
        ),
      ],
    );
  }
}

// ── Held sales sheet ──────────────────────────────────────────────────────────

class _HeldSalesSheet extends ConsumerWidget {
  const _HeldSalesSheet({required this.onResume});

  final void Function(HeldCart) onResume;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final held = ref.watch(heldSalesProvider);
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 14, 16, 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(S.of(context).heldSales,
                style: GoogleFonts.instrumentSans(
                    fontSize: 16, fontWeight: FontWeight.w700)),
            const SizedBox(height: 12),
            if (held.isEmpty)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 24),
                child: Center(
                  child: Text(S.of(context).noHeldSales,
                      style: GoogleFonts.instrumentSans(
                          fontSize: 13.5, color: context.colors.muted)),
                ),
              )
            else
              Flexible(
                child: ListView.separated(
                  shrinkWrap: true,
                  itemCount: held.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 8),
                  itemBuilder: (context, i) {
                    final cart = held[i];
                    return Container(
                      decoration: BoxDecoration(
                        color: Theme.of(context).colorScheme.surface,
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(
                            color: Theme.of(context).colorScheme.outline),
                      ),
                      padding: const EdgeInsets.fromLTRB(14, 10, 6, 10),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(cart.label,
                                    style: GoogleFonts.instrumentSans(
                                        fontSize: 13.5,
                                        fontWeight: FontWeight.w600)),
                                const SizedBox(height: 2),
                                Text(
                                  '${S.of(context).itemsCount(cart.itemCount)}'
                                  ' · ${formatCurrency(cart.total)}'
                                  ' · ${formatRelative(cart.createdAt, s: S.of(context))}',
                                  style: GoogleFonts.instrumentSans(
                                      fontSize: 11.5, color: context.colors.muted),
                                ),
                              ],
                            ),
                          ),
                          IconButton(
                            tooltip: S.of(context).delete,
                            icon: Icon(Icons.delete_outline,
                                size: 20, color: context.colors.red),
                            onPressed: () => ref
                                .read(heldSalesProvider.notifier)
                                .remove(cart.id),
                          ),
                          FilledButton(
                            onPressed: () {
                              Navigator.of(context).pop();
                              onResume(cart);
                            },
                            style: FilledButton.styleFrom(
                              padding: const EdgeInsets.symmetric(
                                  horizontal: 14, vertical: 8),
                            ),
                            child: Text(S.of(context).resume),
                          ),
                        ],
                      ),
                    );
                  },
                ),
              ),
          ],
        ),
      ),
    );
  }
}