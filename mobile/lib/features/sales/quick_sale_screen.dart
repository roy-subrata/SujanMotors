import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:path_provider/path_provider.dart';
import 'package:share_plus/share_plus.dart';

import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/sale.dart';
import 'charge_screen.dart';
import 'quick_sale_providers.dart';
import 'sales_repository.dart';

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
    final state = ref.read(quickSaleControllerProvider);
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => ChargeScreen(cartTotal: state.grandTotal),
      ),
    );
  }

  void _showDiscountDialog(
      QuickSaleState state, QuickSaleController controller) {
    final ctrl =
        TextEditingController(text: state.discount > 0 ? state.discount.toStringAsFixed(2) : '');
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (ctx) {
        return Padding(
          padding: EdgeInsets.fromLTRB(
              20, 20, 20, MediaQuery.of(ctx).viewInsets.bottom + 20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Discount',
                style: GoogleFonts.instrumentSans(
                  fontSize: 18,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                'Subtotal: ${formatCurrency(state.total)}',
                style: GoogleFonts.instrumentSans(
                    fontSize: 13.5, color: AppColors.muted),
              ),
              const SizedBox(height: 16),
              TextField(
                controller: ctrl,
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
                inputFormatters: [
                  FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
                ],
                autofocus: true,
                decoration: InputDecoration(
                  labelText: 'Discount amount',
                  prefixText: '৳ ',
                  prefixStyle: GoogleFonts.instrumentSans(
                      fontSize: 15, fontWeight: FontWeight.w600),
                  border: const OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () {
                        controller.setDiscount(0);
                        Navigator.pop(ctx);
                      },
                      style: OutlinedButton.styleFrom(
                        foregroundColor: AppColors.ink,
                        side: BorderSide(
                            color: Theme.of(context).colorScheme.outline),
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(10)),
                        padding: const EdgeInsets.symmetric(vertical: 12),
                      ),
                      child: const Text('Clear'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: FilledButton(
                      onPressed: () {
                        final val = double.tryParse(ctrl.text) ?? 0;
                        controller.setDiscount(val);
                        Navigator.pop(ctx);
                      },
                      style: FilledButton.styleFrom(
                        backgroundColor: AppColors.ink,
                        foregroundColor: Colors.white,
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(10)),
                        padding: const EdgeInsets.symmetric(vertical: 12),
                      ),
                      child: const Text('Apply'),
                    ),
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );
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

    if (state.isScanning) {
      return Scaffold(
        backgroundColor: Colors.black,
        body: _buildScannerOverlay(state),
      );
    }

    return Scaffold(
      appBar: _buildAppBar(context, itemCount: state.itemCount),
      body: state.isEmpty
          ? _EmptyCartView(onScan: _startScan)
          : _buildCartBody(state, controller),
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
            'Cart',
            style: GoogleFonts.instrumentSans(
              fontSize: 17,
              fontWeight: FontWeight.w700
            ),
          ),
          if (itemCount > 0)
            Text(
              'INV-draft Â· $itemCount item${itemCount == 1 ? '' : 's'}',
              style: GoogleFonts.instrumentSans(
                fontSize: 12
              ),
            ),
        ],
      ),
      actions: [
        IconButton(
          icon: const Icon(Icons.qr_code_scanner),
          tooltip: 'Scan barcode',
          onPressed: _startScan,
        ),
      ],
    );
  }

  Widget _buildCartBody(
      QuickSaleState state, QuickSaleController controller) {
    return Stack(
      children: [
        ListView(
          padding: const EdgeInsets.fromLTRB(16, 4, 16, 160),
          children: [
            // â”€â”€ Line items card â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
                                  content: Text(
                                      'Only ${item.availableStock} ${item.name} in stock'),
                                  backgroundColor: AppColors.red,
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
                            child: const Icon(Icons.add,
                                size: 18, color: AppColors.secondary),
                          ),
                          const SizedBox(width: 12),
                          Text(
                            'Add more items',
                            style: GoogleFonts.instrumentSans(
                              fontSize: 13.5
                            ),
                          ),
                          const Spacer(),
                          const Icon(Icons.qr_code_scanner,
                              size: 18, color: AppColors.muted),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),

            // â”€â”€ Totals card â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
                    label: 'Subtotal Â· ${state.itemCount} items',
                    value: formatCurrency(state.total),
                    labelStyle: GoogleFonts.instrumentSans(
                        fontSize: 13.5, color: AppColors.secondary),
                    valueStyle: GoogleFonts.instrumentSans(
                        fontSize: 13.5,
                        fontWeight: FontWeight.w600,
                        color: AppColors.ink),
                  ),
                  const SizedBox(height: 8),
                  InkWell(
                    onTap: () => _showDiscountDialog(state, controller),
                    borderRadius: BorderRadius.circular(6),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 4, vertical: 2),
                      child: _TotalRow(
                        label: 'Discount',
                        value: state.discount > 0
                            ? '- ${formatCurrency(state.discount)}'
                            : 'Tap to add',
                        labelStyle: GoogleFonts.instrumentSans(
                            fontSize: 13.5, color: AppColors.secondary),
                        valueStyle: GoogleFonts.instrumentSans(
                          fontSize: 13.5,
                          color: state.discount > 0
                              ? AppColors.red
                              : AppColors.muted,
                          fontWeight: state.discount > 0
                              ? FontWeight.w600
                              : FontWeight.normal,
                        ),
                      ),
                    ),
                  ),
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 10),
                    child: Divider(
                        height: 1,
                        color: Theme.of(context).colorScheme.outline.withAlpha(60)),
                  ),
                  _TotalRow(
                    label: 'Total',
                    value: formatCurrency(state.grandTotal),
                    labelStyle: GoogleFonts.instrumentSans(
                        fontSize: 13.5,
                        fontWeight: FontWeight.w700,
                        color: AppColors.ink),
                    valueStyle: GoogleFonts.instrumentSans(
                        fontSize: 20,
                        fontWeight: FontWeight.w700,
                        color: AppColors.ink),
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
            total: state.grandTotal,
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
              icon: Icons.close, tooltip: 'Cancel', onTap: _stopScan),
        ),
        Positioned(
          top: 48,
          right: 12,
          child: _ScanIconButton(
            icon: Icons.flash_on,
            tooltip: 'Torch',
            onTap: () => _scanner.toggleTorch(),
          ),
        ),
        const Positioned(
          bottom: 80,
          child: Text(
            'Point camera at a barcode',
            style: TextStyle(color: Colors.white, fontSize: 15),
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
                color: AppColors.red,
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
            isPermission ? 'Camera access needed' : 'Camera error',
            textAlign: TextAlign.center,
            style: const TextStyle(
                color: Colors.white,
                fontSize: 18,
                fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 8),
          Text(
            isPermission
                ? 'Allow camera access in Settings, then tap Try again.'
                : (error.errorDetails?.message ??
                    'Could not start the camera.'),
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
            label: const Text('Try again'),
          ),
        ],
      ),
    );
  }
}

// â”€â”€ Empty cart view â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
                size: 72, color: AppColors.disabled),
            const SizedBox(height: 16),
            Text(
              'Cart is empty',
              style: GoogleFonts.instrumentSans(
                fontSize: 18,
                fontWeight: FontWeight.w700
              ),
            ),
            const SizedBox(height: 8),
            Text(
              'Scan a barcode or add items from Products',
              textAlign: TextAlign.center,
              style: GoogleFonts.instrumentSans(
                  fontSize: 13.5, color: AppColors.muted),
            ),
            const SizedBox(height: 32),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: onScan,
                icon: const Icon(Icons.qr_code_scanner),
                label: const Text('Scan Barcode'),
                style: FilledButton.styleFrom(
                  backgroundColor: AppColors.ink,
                  foregroundColor: Colors.white,
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

// â”€â”€ Cart item row â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
      child: Column(
        children: [
          // Line 1: Name (left) + line total (right)
          Row(
            children: [
              Expanded(
                child: Text(
                  item.name,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.instrumentSans(
                    fontSize: 13.5,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ),
              const SizedBox(width: 8),
              Text(
                formatCurrency(item.lineTotal),
                style: GoogleFonts.instrumentSans(
                  fontSize: 13.5,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
          const SizedBox(height: 6),
          // Line 2: Price input (left) + qty stepper (center) + delete (right)
          Row(
            children: [
              // Price input
              Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    '৳',
                    style: GoogleFonts.instrumentSans(
                      fontSize: 12,
                      color: AppColors.muted,
                    ),
                  ),
                  const SizedBox(width: 2),
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
                        fontWeight: FontWeight.w600,
                      ),
                      decoration: InputDecoration(
                        isDense: true,
                        contentPadding: const EdgeInsets.symmetric(
                            horizontal: 0, vertical: 4),
                        border: UnderlineInputBorder(
                            borderSide: BorderSide(
                                color: Theme.of(context).colorScheme.outline)),
                        focusedBorder: const UnderlineInputBorder(
                            borderSide: BorderSide(width: 1.5)),
                      ),
                    ),
                  ),
                ],
              ),

              const Spacer(),

              // Qty stepper
              Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  _StepBtn(icon: Icons.remove, onTap: widget.onDecrement),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 10),
                    child: Text(
                      '${item.quantity}',
                      style: GoogleFonts.instrumentSans(
                        fontSize: 14,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  _StepBtn(icon: Icons.add, onTap: widget.onIncrement),
                ],
              ),

              const Spacer(),

              // Delete
              GestureDetector(
                onTap: widget.onRemove,
                child: const Icon(Icons.delete_outline,
                    size: 18, color: AppColors.muted),
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
        child: Icon(icon, size: 16, color: AppColors.secondary),
      ),
    );
  }
}

// â”€â”€ Total row â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

// â”€â”€ Sticky bottom CTA â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
                    backgroundColor: AppColors.green,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(14)),
                    padding: const EdgeInsets.symmetric(vertical: 15),
                    textStyle: GoogleFonts.instrumentSans(
                        fontSize: 15, fontWeight: FontWeight.w700),
                  ),
                  child: Text(
                      'Complete Sale . '),
                ),
              ),
            ),

          ],
        ),
      ),
    );
  }
}

// â”€â”€ Scan icon button â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

// â”€â”€ Success view â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
                  color: AppColors.greenBg, shape: BoxShape.circle),
              child: const Icon(Icons.check_circle_rounded,
                  size: 52, color: AppColors.green),
            ),
            const SizedBox(height: 20),
            Text(
              'Sale Complete!',
              style: GoogleFonts.instrumentSans(
                fontSize: 22,
                fontWeight: FontWeight.w800
              ),
            ),
            const SizedBox(height: 20),
            _InfoRow(label: 'Invoice', value: result.invoiceNumber),
            const SizedBox(height: 6),
            _InfoRow(
                label: 'Total',
                value: formatCurrency(result.grandTotal)),
            if (result.hasDue) ...[
              const SizedBox(height: 6),
              _InfoRow(
                  label: 'Paid',
                  value: formatCurrency(result.paidAmount)),
              const SizedBox(height: 4),
              _InfoRow(
                label: 'Due',
                value: formatCurrency(result.dueAmount),
                valueColor: AppColors.red,
              ),
            ],
            const SizedBox(height: 40),
            if (result.customerPhone != null &&
                result.customerPhone!.isNotEmpty) ...[
              _SendReceiptButtons(result: result),
              const SizedBox(height: 12),
            ],
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: onNewSale,
                icon: const Icon(Icons.add_shopping_cart),
                label: const Text('New Sale'),
                style: FilledButton.styleFrom(
                  backgroundColor: AppColors.ink,
                  foregroundColor: Colors.white,
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
              fontSize: 15, color: AppColors.muted),
        ),
        Text(
          value,
          style: GoogleFonts.instrumentSans(
            fontSize: 15,
            fontWeight: FontWeight.w700,
            color: valueColor ?? AppColors.ink,
          ),
        ),
      ],
    );
  }
}

class _SendReceiptButtons extends ConsumerWidget {
  const _SendReceiptButtons({required this.result});

  final QuickSaleResult result;

  String get _receiptMessage {
    final buf = StringBuffer()
      ..writeln('Sujan Motors - Invoice')
      ..writeln('Invoice: ${result.invoiceNumber}')
      ..writeln(
          'Date: ${DateTime.now().day}/${DateTime.now().month}/${DateTime.now().year}')
      ..writeln()
      ..writeln('Total: ${formatCurrency(result.grandTotal)}')
      ..writeln('Paid: ${formatCurrency(result.paidAmount)}');
    if (result.hasDue) {
      buf.writeln('Due: ${formatCurrency(result.dueAmount)}');
    }
    if (result.technicianName != null &&
        result.technicianName!.isNotEmpty) {
      buf.writeln('Technician: ${result.technicianName}');
    }
    buf
      ..writeln()
      ..writeln('Thank you for your purchase!');
    return buf.toString();
  }

  Future<File> _downloadPdf(WidgetRef ref) async {
    final repo = ref.read(salesRepositoryProvider);
    final bytes = await repo.downloadInvoicePdf(result.invoiceId);
    final dir = await getTemporaryDirectory();
    final file = File('${dir.path}/invoice-${result.invoiceNumber}.pdf');
    await file.writeAsBytes(bytes);
    return file;
  }

  Future<void> _sharePdf(BuildContext context, WidgetRef ref) async {
    try {
      final file = await _downloadPdf(ref);
      await Share.shareXFiles(
        [XFile(file.path)],
        text: 'Invoice ${result.invoiceNumber}',
      );
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Failed to share invoice: $e'),
            backgroundColor: AppColors.red,
          ),
        );
      }
    }
  }

  Future<void> _sharePdfVia(BuildContext context, WidgetRef ref,
      {String? phoneNumber}) async {
    try {
      final file = await _downloadPdf(ref);
      await Share.shareXFiles(
        [XFile(file.path)],
        text: phoneNumber != null
            ? 'Invoice ${result.invoiceNumber}\n$_receiptMessage'
            : 'Invoice ${result.invoiceNumber}',
      );
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Failed to share invoice: $e'),
            backgroundColor: AppColors.red,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Column(
      children: [
        Row(
          children: [
            Expanded(
              child: OutlinedButton.icon(
                onPressed: () => _sharePdfVia(context, ref),
                icon: const Icon(Icons.sms_outlined, size: 18),
                label: Text(
                  'SMS',
                  style: GoogleFonts.instrumentSans(
                      fontSize: 13, fontWeight: FontWeight.w600),
                ),
                style: OutlinedButton.styleFrom(
                  foregroundColor: AppColors.green,
                  side: const BorderSide(color: AppColors.green),
                  shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(11)),
                  padding: const EdgeInsets.symmetric(vertical: 12),
                ),
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: OutlinedButton.icon(
                onPressed: () => _sharePdfVia(context, ref),
                icon: const Icon(Icons.chat_bubble_outline, size: 18),
                label: Text(
                  'WhatsApp',
                  style: GoogleFonts.instrumentSans(
                      fontSize: 13, fontWeight: FontWeight.w600),
                ),
                style: OutlinedButton.styleFrom(
                  foregroundColor: AppColors.green,
                  side: const BorderSide(color: AppColors.green),
                  shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(11)),
                  padding: const EdgeInsets.symmetric(vertical: 12),
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 8),
        SizedBox(
          width: double.infinity,
          child: OutlinedButton.icon(
            onPressed: () => _sharePdf(context, ref),
            icon: const Icon(Icons.picture_as_pdf_outlined, size: 18),
            label: Text(
              'Share Invoice PDF',
              style: GoogleFonts.instrumentSans(
                  fontSize: 13, fontWeight: FontWeight.w600),
            ),
            style: OutlinedButton.styleFrom(
              foregroundColor: AppColors.ink,
              side:
                  BorderSide(color: Theme.of(context).colorScheme.outline),
              shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(11)),
              padding: const EdgeInsets.symmetric(vertical: 12),
            ),
          ),
        ),
      ],
    );
  }
}