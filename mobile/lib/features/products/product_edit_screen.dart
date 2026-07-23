import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/i18n/strings.dart';
import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/models/product.dart';
import '../../shared/widgets/design_system.dart';
import '../../shared/widgets/state_views.dart';
import 'categories_repository.dart';
import 'products_providers.dart';
import 'products_repository.dart';

/// Edit product — name, category/brand, price, codes, minimum stock, active.
///
/// The API's PUT is a full replace, so fields this form doesn't expose
/// (warranty, dimensions, tax, tags, units, cost) are round-tripped verbatim
/// from the raw GET payload.
class ProductEditScreen extends ConsumerStatefulWidget {
  const ProductEditScreen({super.key, required this.productId});

  final String productId;

  @override
  ConsumerState<ProductEditScreen> createState() => _ProductEditScreenState();
}

class _ProductEditScreenState extends ConsumerState<ProductEditScreen> {
  final _formKey = GlobalKey<FormState>();

  final _name = TextEditingController();
  final _localName = TextEditingController();
  final _description = TextEditingController();
  final _sellingPrice = TextEditingController();
  final _barcode = TextEditingController();
  final _oemNumber = TextEditingController();
  final _minimumStock = TextEditingController();

  Map<String, dynamic>? _raw;
  List<Category> _categories = [];
  List<NamedRef> _brands = [];
  String? _categoryId;
  String? _brandId;
  bool _isActive = true;

  bool _loading = true;
  String? _loadError;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _name.dispose();
    _localName.dispose();
    _description.dispose();
    _sellingPrice.dispose();
    _barcode.dispose();
    _oemNumber.dispose();
    _minimumStock.dispose();
    super.dispose();
  }

  static Map<String, dynamic>? _m(dynamic v) =>
      v is Map ? Map<String, dynamic>.from(v) : null;

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _loadError = null;
    });
    try {
      final repo = ref.read(productsRepositoryProvider);
      final results = await Future.wait([
        repo.getRawById(widget.productId),
        ref
            .read(categoriesRepositoryProvider)
            .search(page: 1, pageSize: 100)
            .then((r) => r.data),
        repo.brands(),
      ]);
      if (!mounted) return;

      final raw = results[0] as Map<String, dynamic>;
      final categories = List<Category>.from(results[1] as List);
      final brands = List<NamedRef>.from(results[2] as List);

      // Ensure the current selections are present in the dropdown lists even
      // when they fall outside the first page of options.
      final rawCategory = _m(raw['category']);
      if (rawCategory != null &&
          !categories.any((c) => c.id == rawCategory['id'])) {
        categories.insert(0, Category.fromJson(rawCategory));
      }
      final rawBrand = _m(raw['brand']);
      if (rawBrand != null && !brands.any((b) => b.id == rawBrand['id'])) {
        brands.insert(0, NamedRef.fromJson(rawBrand));
      }

      _name.text = (raw['name'] ?? '') as String;
      _localName.text = (raw['localName'] ?? '') as String? ?? '';
      _description.text = (raw['description'] ?? '') as String? ?? '';
      final price = _m(raw['pricing'])?['sellingPrice'];
      _sellingPrice.text = price is num ? _trimZeros(price) : '';
      _barcode.text = (raw['barcode'] ?? '') as String? ?? '';
      _oemNumber.text = (raw['oemNumber'] ?? '') as String? ?? '';
      final minStock = raw['minimumStock'];
      _minimumStock.text = minStock is num ? '${minStock.toInt()}' : '0';

      setState(() {
        _raw = raw;
        _categories = categories;
        _brands = brands;
        _categoryId = rawCategory?['id'] as String?;
        _brandId = rawBrand?['id'] as String?;
        _isActive = raw['isActive'] != false;
        _loading = false;
      });
    } on AppException catch (e) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _loadError = e.message;
      });
    }
  }

  static String _trimZeros(num v) =>
      v == v.truncate() ? '${v.toInt()}' : '$v';

  String? _null(TextEditingController c) =>
      c.text.trim().isEmpty ? null : c.text.trim();

  Future<void> _save() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    final raw = _raw!;
    final messenger = ScaffoldMessenger.of(context);
    final router = GoRouter.of(context);

    setState(() => _saving = true);
    final dimensions = _m(raw['dimensions']);
    final warranty = _m(raw['warranty']);
    final payload = <String, dynamic>{
      'id': widget.productId,
      'name': _name.text.trim(),
      'description': _description.text.trim(),
      'richDescription': raw['richDescription'],
      'categoryId': _categoryId,
      'brandId': _brandId,
      'baseUnitId': _m(raw['baseUnit'])?['id'],
      'unitId': _m(raw['unit'])?['id'],
      'costPrice': _m(raw['pricing'])?['costPrice'] ?? 0,
      'sellingPrice': double.parse(_sellingPrice.text.trim()),
      'minimumStock': int.tryParse(_minimumStock.text.trim()) ?? 0,
      'isActive': _isActive,
      'hasWarranty': warranty?['hasWarranty'] ?? false,
      'warrantyPeriodMonths': warranty?['periodMonths'],
      'warrantyType': warranty?['type'],
      'warrantyTerms': warranty?['terms'],
      'warrantyCertificateTemplate': warranty?['certificateTemplate'],
      'oemNumber': _null(_oemNumber),
      'localName': _null(_localName),
      'barcode': _null(_barcode),
      'tags': raw['tags'],
      'productType': raw['productType'] ?? 'PHYSICAL',
      'isPerishable': raw['isPerishable'] ?? false,
      'weightKg': dimensions?['weightKg'],
      'widthCm': dimensions?['widthCm'],
      'heightCm': dimensions?['heightCm'],
      'depthCm': dimensions?['depthCm'],
      'taxCode': raw['taxCode'],
    };

    try {
      await ref
          .read(productsRepositoryProvider)
          .updateProduct(widget.productId, payload);
      ref.invalidate(productDetailProvider(widget.productId));
      ref.invalidate(lowStockCountProvider);
      messenger.showSnackBar(SnackBar(
        content: Text(S.of(context).productUpdated),
        duration: const Duration(seconds: 2),
        behavior: SnackBarBehavior.floating,
      ));
      router.pop();
    } on AppException catch (e) {
      if (!mounted) return;
      setState(() => _saving = false);
      messenger.showSnackBar(SnackBar(
        content: Text(e.message),
        backgroundColor: context.colors.red,
        behavior: SnackBarBehavior.floating,
      ));
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    return Scaffold(
      appBar: AppBar(
        title: Text(
          s.editProduct,
          style: GoogleFonts.instrumentSans(
              fontSize: 16, fontWeight: FontWeight.w700),
        ),
      ),
      body: _loading
          ? const LoadingView()
          : _loadError != null
              ? ListView(children: [
                  const SizedBox(height: 120),
                  ErrorView(message: _loadError!, onRetry: _load),
                ])
              : Column(
                  children: [
                    Expanded(
                      child: Form(
                        key: _formKey,
                        child: ListView(
                          padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
                          children: [
                            _Field(
                              label: s.nameRequired,
                              child: TextFormField(
                                controller: _name,
                                textCapitalization:
                                    TextCapitalization.sentences,
                                validator: (v) =>
                                    (v ?? '').trim().isEmpty
                                        ? s.nameRequiredValidation
                                        : null,
                              ),
                            ),
                            _Field(
                              label: s.localName,
                              child: TextFormField(controller: _localName),
                            ),
                            Row(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Expanded(
                                  child: _Field(
                                    label: s.categoryRequired,
                                    child: _dropdown(
                                      value: _categoryId,
                                      items: [
                                        for (final c in _categories)
                                          (c.id, c.name),
                                      ],
                                      onChanged: (v) =>
                                          setState(() => _categoryId = v),
                                      validator: (v) => v == null
                                          ? s.categoryRequiredValidation
                                          : null,
                                    ),
                                  ),
                                ),
                                const SizedBox(width: 10),
                                Expanded(
                                  child: _Field(
                                    label: s.brand,
                                    child: _dropdown(
                                      value: _brandId,
                                      items: [
                                        (null, s.noBrand),
                                        for (final b in _brands)
                                          (b.id, b.name),
                                      ],
                                      onChanged: (v) =>
                                          setState(() => _brandId = v),
                                    ),
                                  ),
                                ),
                              ],
                            ),
                            Row(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Expanded(
                                  child: _Field(
                                    label: s.sellingPriceRequired,
                                    child: TextFormField(
                                      controller: _sellingPrice,
                                      keyboardType: const TextInputType
                                          .numberWithOptions(decimal: true),
                                      validator: (v) {
                                        final parsed =
                                            double.tryParse((v ?? '').trim());
                                        if (parsed == null || parsed < 0) {
                                          return s.enterValidPrice;
                                        }
                                        return null;
                                      },
                                    ),
                                  ),
                                ),
                                const SizedBox(width: 10),
                                Expanded(
                                  child: _Field(
                                    label: s.minimumStock,
                                    child: TextFormField(
                                      controller: _minimumStock,
                                      keyboardType: TextInputType.number,
                                      validator: (v) {
                                        final t = (v ?? '').trim();
                                        if (t.isEmpty) return null;
                                        final parsed = int.tryParse(t);
                                        if (parsed == null || parsed < 0) {
                                          return s.wholeNumber;
                                        }
                                        return null;
                                      },
                                    ),
                                  ),
                                ),
                              ],
                            ),
                            _Field(
                              label: s.barcode,
                              child: TextFormField(controller: _barcode),
                            ),
                            _Field(
                              label: s.oemNumber,
                              child: TextFormField(controller: _oemNumber),
                            ),
                            _Field(
                              label: s.descriptionLabel,
                              child: TextFormField(
                                controller: _description,
                                maxLines: 3,
                                textCapitalization:
                                    TextCapitalization.sentences,
                              ),
                            ),
                            // Active toggle
                            Container(
                              padding: const EdgeInsets.symmetric(
                                  horizontal: 14, vertical: 4),
                              decoration: BoxDecoration(
                                color: Theme.of(context).colorScheme.surface,
                                borderRadius: BorderRadius.circular(11),
                                border: Border.all(
                                    color:
                                        Theme.of(context).colorScheme.outline),
                              ),
                              child: Row(
                                children: [
                                  Expanded(
                                    child: Text(
                                      s.activeLabel,
                                      style: GoogleFonts.instrumentSans(
                                        fontSize: 13.5,
                                        fontWeight: FontWeight.w500,
                                      ),
                                    ),
                                  ),
                                  Switch(
                                    value: _isActive,
                                    onChanged: (v) =>
                                        setState(() => _isActive = v),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                    PrimaryCtaBar(
                      label: s.saveChanges,
                      isLoading: _saving,
                      onTap: _save,
                    ),
                  ],
                ),
    );
  }

  Widget _dropdown({
    required String? value,
    required List<(String?, String)> items,
    required ValueChanged<String?> onChanged,
    String? Function(String?)? validator,
  }) {
    return DropdownButtonFormField<String?>(
      initialValue: value,
      isExpanded: true,
      validator: validator,
      items: [
        for (final (id, label) in items)
          DropdownMenuItem(
            value: id,
            child: Text(
              label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: GoogleFonts.instrumentSans(fontSize: 13.5),
            ),
          ),
      ],
      onChanged: _saving ? null : onChanged,
    );
  }
}

/// Muted label above the input, matching the design's field styling.
class _Field extends StatelessWidget {
  const _Field({required this.label, required this.child});

  final String label;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: GoogleFonts.instrumentSans(
              fontSize: 11.5,
              fontWeight: FontWeight.w600,
              color: context.colors.secondary,
            ),
          ),
          const SizedBox(height: 6),
          child,
        ],
      ),
    );
  }
}
