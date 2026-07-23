import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../shared/models/product.dart';
import '../../shared/models/stock.dart';
import '../../shared/widgets/app_scaffold.dart';
import '../products/products_repository.dart';
import 'stock_adjustment_sheet.dart';
import 'stock_repository.dart';
import '../../core/i18n/strings.dart';

class StockInScreen extends ConsumerStatefulWidget {
  const StockInScreen({super.key});

  @override
  ConsumerState<StockInScreen> createState() => _StockInScreenState();
}

class _StockInScreenState extends ConsumerState<StockInScreen> {
  // ── Scanner ───────────────────────────────────────────────────────────────────
  final _scanner = MobileScannerController(
    detectionSpeed: DetectionSpeed.noDuplicates,
  );
  bool _scanning = false;
  bool _handling = false;

  // ── Search ────────────────────────────────────────────────────────────────────
  final _searchCtrl = TextEditingController();
  final _scrollCtrl = ScrollController();
  Timer? _debounce;
  List<Product> _searchResults = [];
  bool _loadingSearch = false;

  // ── Product load ──────────────────────────────────────────────────────────────
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

  // ── Search ────────────────────────────────────────────────────────────────────

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

  // ── Scanner ───────────────────────────────────────────────────────────────────

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

  // ── Load product & open sheet ─────────────────────────────────────────────────

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
          _lookupError = S.of(context).productNotFoundForBarcode;
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
          _lookupError = S.of(context).failedToLoadProductDetails;
        });
      }
    }
  }

  // ── Build ─────────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    if (_scanning) {
      return AppScaffold(
        title: S.of(context).stockIn,
        showNotificationBell: false,
        body: _buildScannerOverlay(),
      );
    }

    return AppScaffold(
      title: S.of(context).stockIn,
      showNotificationBell: false,
      actions: [
        IconButton(
          icon: const Icon(Icons.qr_code_scanner),
          tooltip: S.of(context).scanBarcode,
          onPressed: _startScan,
        ),
      ],
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final hasSearch = _searchCtrl.text.trim().isNotEmpty;

    return Column(
      children: [
        // ── Search bar ────────────────────────────────────────────────────────
        Padding(
          padding: const EdgeInsets.fromLTRB(12, 12, 12, 8),
          child: TextField(
            controller: _searchCtrl,
            textInputAction: TextInputAction.search,
            decoration: InputDecoration(
              hintText: S.of(context).searchPartsHint,
              prefixIcon: const Icon(Icons.search, size: 20),
              filled: true,
              fillColor:
                  scheme.surfaceContainerHighest.withValues(alpha: 0.6),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(28),
                borderSide: BorderSide.none,
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(28),
                borderSide: BorderSide.none,
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(28),
                borderSide: BorderSide(color: scheme.primary, width: 1.5),
              ),
              isDense: true,
              contentPadding: const EdgeInsets.symmetric(vertical: 10),
              suffixIcon: hasSearch
                  ? IconButton(
                      icon: const Icon(Icons.clear, size: 18),
                      onPressed: () => _searchCtrl.clear(),
                    )
                  : null,
            ),
          ),
        ),

        // ── Loading / error banner ────────────────────────────────────────────
        if (_loadingProduct)
          const Padding(
            padding: EdgeInsets.all(24),
            child: CircularProgressIndicator(),
          )
        else if (_lookupError != null)
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 4, 20, 8),
            child: Text(
              _lookupError!,
              style: TextStyle(color: scheme.error, fontSize: 13),
              textAlign: TextAlign.center,
            ),
          ),

        Expanded(
          child: hasSearch
              ? _buildResults(scheme, theme)
              : _buildEmptyPrompt(theme, scheme),
        ),
      ],
    );
  }

  Widget _buildResults(ColorScheme scheme, ThemeData theme) {
    if (_loadingSearch) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_searchResults.isEmpty) {
      return Center(
        child: Text(
          S.of(context).noProductsFound,
          style: TextStyle(color: scheme.onSurfaceVariant),
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

  Widget _buildEmptyPrompt(ThemeData theme, ColorScheme scheme) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.move_to_inbox_outlined,
              size: 72,
              color: scheme.primary.withValues(alpha: 0.15),
            ),
            const SizedBox(height: 16),
            Text(
              S.of(context).quickStockIn,
              style: theme.textTheme.headlineSmall
                  ?.copyWith(fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            Text(
              S.of(context).quickStockInSubtitle,
              textAlign: TextAlign.center,
              style: theme.textTheme.bodyMedium
                  ?.copyWith(color: scheme.onSurfaceVariant),
            ),
            const SizedBox(height: 32),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: _startScan,
                icon: const Icon(Icons.qr_code_scanner),
                label: Text(S.of(context).scanBarcode),
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

  // ── Scanner overlay ───────────────────────────────────────────────────────────

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
            tooltip: S.of(context).cancel,
            onTap: _stopScan,
          ),
        ),
        Positioned(
          top: 12,
          right: 12,
          child: _ScanBtn(
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
                color: Colors.red.shade700,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Text(
                _lookupError!,
                textAlign: TextAlign.center,
                style: const TextStyle(color: Colors.white),
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
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final stock = product.totalStock;
    final initial = product.name.trim().isEmpty
        ? '?'
        : product.name.trim().characters.first.toUpperCase();

    return Card(
      child: ListTile(
        onTap: onTap,
        contentPadding:
            const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
        leading: CircleAvatar(
          backgroundColor: scheme.primaryContainer,
          child: Text(
            initial,
            style: TextStyle(
              color: scheme.onPrimaryContainer,
              fontWeight: FontWeight.w700,
            ),
          ),
        ),
        title: Text(
          product.name,
          style: theme.textTheme.bodyMedium
              ?.copyWith(fontWeight: FontWeight.w700),
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
        ),
        subtitle: Text(
          product.sku,
          style: theme.textTheme.bodySmall
              ?.copyWith(color: scheme.onSurfaceVariant),
        ),
        trailing: stock != null
            ? Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: stock > 0
                      ? Colors.green.shade50
                      : scheme.errorContainer.withValues(alpha: 0.5),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  '$stock ${product.unitName ?? 'pcs'}',
                  style: TextStyle(
                    color: stock > 0
                        ? Colors.green.shade700
                        : scheme.error,
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              )
            : const Icon(Icons.chevron_right),
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
