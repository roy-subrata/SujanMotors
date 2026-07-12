import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../core/theme/app_theme.dart';
import '../../shared/models/product.dart';
import '../../shared/models/stock.dart';
import '../../shared/widgets/app_scaffold.dart';
import '../products/products_repository.dart';
import 'stock_adjustment_sheet.dart';
import 'stock_repository.dart';

const _kAccent = Color(0xFF059669);

class StockInScreen extends ConsumerStatefulWidget {
  const StockInScreen({super.key});

  @override
  ConsumerState<StockInScreen> createState() => _StockInScreenState();
}

class _StockInScreenState extends ConsumerState<StockInScreen> {
  final _scanner = MobileScannerController(
    detectionSpeed: DetectionSpeed.noDuplicates,
  );
  bool _scanning = false;
  bool _handling = false;

  final _searchCtrl = TextEditingController();
  final _scrollCtrl = ScrollController();
  Timer? _debounce;
  List<Product> _searchResults = [];
  bool _loadingSearch = false;

  bool _loadingProduct = false;
  String? _lookupError;

  @override
  void initState() {
    super.initState();
    _searchCtrl.addListener(_onSearchChanged);
  }

  @override
  void dispose() {
    _scanner.dispose();
    _searchCtrl.dispose();
    _scrollCtrl.dispose();
    _debounce?.cancel();
    super.dispose();
  }

  void _onSearchChanged() {
    final q = _searchCtrl.text.trim();
    _debounce?.cancel();
    if (_lookupError != null) setState(() => _lookupError = null);
    if (q.isEmpty) {
      setState(() {
        _searchResults = [];
        _loadingSearch = false;
      });
      return;
    }
    _debounce = Timer(const Duration(milliseconds: 380), () {
      if (mounted) _doSearch(q);
    });
  }

  Future<void> _doSearch(String q) async {
    setState(() => _loadingSearch = true);
    try {
      final res = await ref
          .read(productsRepositoryProvider)
          .search(query: q, pageSize: 20);
      if (!mounted) return;
      setState(() {
        _searchResults = res.data;
        _loadingSearch = false;
      });
    } catch (_) {
      if (mounted) setState(() => _loadingSearch = false);
    }
  }

  Future<void> _startScan() async {
    setState(() {
      _scanning = true;
      _lookupError = null;
    });
    try {
      await _scanner.start();
    } catch (_) {}
  }

  Future<void> _stopScan() async {
    await _scanner.stop();
    if (mounted) setState(() => _scanning = false);
  }

  Future<void> _onDetect(BarcodeCapture capture) async {
    if (_handling) return;
    final code = capture.barcodes
        .map((b) => b.rawValue)
        .firstWhere((v) => v != null && v.isNotEmpty, orElse: () => null);
    if (code == null) return;
    setState(() => _handling = true);
    await _scanner.stop();
    await _openSheetForCode(code);
    setState(() => _handling = false);
    if (_scanning) {
      try {
        await _scanner.start();
      } catch (_) {}
    }
  }

  Future<void> _openSheetForCode(String code) async {
    setState(() {
      _loadingProduct = true;
      _lookupError = null;
    });
    try {
      final byCode =
          await ref.read(productsRepositoryProvider).getByCode(code);
      await _openSheetForId(byCode.productId);
    } catch (_) {
      if (mounted) {
        setState(() {
          _loadingProduct = false;
          _lookupError = 'Product not found for this barcode';
        });
      }
    }
  }

  Future<void> _openSheetForId(String productId) async {
    setState(() {
      _loadingProduct = true;
      _lookupError = null;
      _scanning = false;
    });
    try {
      final results = await Future.wait([
        ref.read(productsRepositoryProvider).getById(productId),
        ref.read(stockRepositoryProvider).levelsForPart(productId),
      ]);

      final product = results[0] as Product;
      final levels = results[1] as List<StockLevel>;

      if (!mounted) return;
      setState(() => _loadingProduct = false);

      await showModalBottomSheet<void>(
        context: context,
        isScrollControlled: true,
        useSafeArea: true,
        backgroundColor: Colors.transparent,
        builder: (_) => StockAdjustmentSheet(
          product: product,
          stockLevels: levels,
        ),
      );
    } catch (_) {
      if (mounted) {
        setState(() {
          _loadingProduct = false;
          _lookupError = 'Failed to load product details';
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_scanning) {
      return AppScaffold(
        title: 'Stock In',
        showNotificationBell: false,
        body: _buildScannerOverlay(),
      );
    }

    return AppScaffold(
      title: 'Stock In',
      showNotificationBell: false,
      actions: [
        IconButton(
          icon: const Icon(Icons.qr_code_scanner),
          tooltip: 'Scan barcode',
          onPressed: _startScan,
        ),
      ],
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    final scheme = Theme.of(context).colorScheme;
    final hasSearch = _searchCtrl.text.trim().isNotEmpty;

    return Column(
      children: [
        // ── Search bar ────────────────────────────────────────────────────
        Padding(
          padding: const EdgeInsets.fromLTRB(12, 12, 12, 8),
          child: TextField(
            controller: _searchCtrl,
            textInputAction: TextInputAction.search,
            style: GoogleFonts.instrumentSans(fontSize: 14, color: AppColors.ink),
            decoration: InputDecoration(
              hintText: 'Search parts, SKU or brand...',
              hintStyle: GoogleFonts.instrumentSans(color: AppColors.muted, fontSize: 14),
              prefixIcon: const Icon(Icons.search, size: 20, color: AppColors.muted),
              filled: true,
              fillColor: scheme.surface,
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(11),
                borderSide: BorderSide(color: scheme.outline),
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(11),
                borderSide: BorderSide(color: scheme.outline),
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(11),
                borderSide: const BorderSide(color: Color(0xFF4F46E5), width: 1.5),
              ),
              isDense: true,
              contentPadding: const EdgeInsets.symmetric(vertical: 10, horizontal: 14),
              suffixIcon: hasSearch
                  ? IconButton(
                      icon: const Icon(Icons.clear, size: 18),
                      onPressed: () => _searchCtrl.clear(),
                    )
                  : null,
            ),
          ),
        ),

        // ── Loading / error banner ────────────────────────────────────────
        if (_loadingProduct)
          const Padding(
            padding: EdgeInsets.all(24),
            child: CircularProgressIndicator(),
          )
        else if (_lookupError != null)
          Container(
            margin: const EdgeInsets.fromLTRB(16, 4, 16, 8),
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
            decoration: BoxDecoration(
              color: AppColors.redBg,
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: AppColors.redBorder),
            ),
            child: Row(
              children: [
                const Icon(Icons.error_outline, size: 18, color: AppColors.red),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    _lookupError!,
                    style: GoogleFonts.instrumentSans(color: AppColors.red, fontSize: 13),
                  ),
                ),
              ],
            ),
          ),

        Expanded(
          child: hasSearch
              ? _buildResults(scheme)
              : _buildEmptyPrompt(),
        ),
      ],
    );
  }

  Widget _buildResults(ColorScheme scheme) {
    if (_loadingSearch) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_searchResults.isEmpty) {
      return Center(
        child: Text(
          'No products found',
          style: GoogleFonts.instrumentSans(color: AppColors.muted, fontSize: 13),
        ),
      );
    }
    return ListView.separated(
      controller: _scrollCtrl,
      padding: const EdgeInsets.fromLTRB(12, 4, 12, 16),
      itemCount: _searchResults.length,
      separatorBuilder: (_, sep) => const SizedBox(height: 6),
      itemBuilder: (_, i) => _ProductTile(
        product: _searchResults[i],
        onTap: () => _openSheetForId(_searchResults[i].id),
      ),
    );
  }

  Widget _buildEmptyPrompt() {
    final scheme = Theme.of(context).colorScheme;
    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 72,
              height: 72,
              decoration: BoxDecoration(
                color: _kAccent.withAlpha(18),
                borderRadius: BorderRadius.circular(20),
                border: Border.all(color: _kAccent.withAlpha(30)),
              ),
              child: Icon(
                Icons.move_to_inbox_outlined,
                size: 34,
                color: _kAccent,
              ),
            ),
            const SizedBox(height: 16),
            Text(
              'Quick Stock In',
              style: GoogleFonts.instrumentSans(
                fontSize: 17,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              'Scan a barcode or search for a product to record received stock or adjust inventory counts.',
              textAlign: TextAlign.center,
              style: GoogleFonts.instrumentSans(
                fontSize: 13,
                color: scheme.onSurface.withAlpha(160),
              ),
            ),
            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: _startScan,
                icon: const Icon(Icons.qr_code_scanner),
                label: const Text('Scan Barcode'),
                style: FilledButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  // ── Scanner overlay ───────────────────────────────────────────────────────

  Widget _buildScannerOverlay() {
    return Stack(
      alignment: Alignment.center,
      children: [
        MobileScanner(
          controller: _scanner,
          onDetect: _onDetect,
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
          top: 12,
          left: 12,
          child: _ScanBtn(
            icon: Icons.close,
            tooltip: 'Cancel',
            onTap: _stopScan,
          ),
        ),
        Positioned(
          top: 12,
          right: 12,
          child: _ScanBtn(
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
        if (_handling || _loadingProduct)
          const Positioned(
            bottom: 36,
            child: CircularProgressIndicator(color: Colors.white),
          ),
        if (_lookupError != null && !_handling)
          Positioned(
            bottom: 28,
            left: 24,
            right: 24,
            child: Container(
              padding:
                  const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              decoration: BoxDecoration(
                color: AppColors.red,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Text(
                _lookupError!,
                textAlign: TextAlign.center,
                style: GoogleFonts.instrumentSans(color: Colors.white, fontSize: 13),
              ),
            ),
          ),
      ],
    );
  }
}

// ── Product search tile ───────────────────────────────────────────────────────

class _ProductTile extends StatelessWidget {
  const _ProductTile({required this.product, required this.onTap});

  final Product product;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final stock = product.totalStock;
    final initial = product.name.trim().isEmpty
        ? '?'
        : product.name.trim().characters.first.toUpperCase();
    final inStock = stock != null && stock > 0;

    return Material(
      color: scheme.surface,
      borderRadius: BorderRadius.circular(13),
      child: InkWell(
        borderRadius: BorderRadius.circular(13),
        onTap: onTap,
        child: Container(
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(13),
            border: Border.all(color: scheme.outline),
          ),
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
          child: Row(
            children: [
              CircleAvatar(
                radius: 20,
                backgroundColor: _kAccent.withAlpha(25),
                child: Text(
                  initial,
                  style: GoogleFonts.instrumentSans(
                    color: _kAccent,
                    fontWeight: FontWeight.w700,
                    fontSize: 14,
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      product.name,
                      style: GoogleFonts.instrumentSans(
                        fontSize: 13.5,
                        fontWeight: FontWeight.w600,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 2),
                    Text(
                      product.sku,
                      style: GoogleFonts.instrumentSans(
                        fontSize: 11.5,
                        color: AppColors.muted,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              if (stock != null)
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: inStock
                        ? AppColors.greenBg
                        : AppColors.redBg,
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(
                      color: inStock
                          ? AppColors.green.withAlpha(40)
                          : AppColors.redBorder,
                    ),
                  ),
                  child: Text(
                    '$stock ${product.unitName ?? 'pcs'}',
                    style: GoogleFonts.instrumentSans(
                      color: inStock ? AppColors.green : AppColors.red,
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                )
              else
                const Icon(Icons.chevron_right, color: AppColors.disabled),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Scan overlay icon button ──────────────────────────────────────────────────

class _ScanBtn extends StatelessWidget {
  const _ScanBtn(
      {required this.icon, required this.tooltip, required this.onTap});

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
