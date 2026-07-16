import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/models/product_specification.dart';
import '../../shared/widgets/design_system.dart';
import '../../shared/widgets/state_views.dart';
import 'products_providers.dart';
import 'products_repository.dart';

/// Edit a product's simple specs — a reorderable list of Label/Value rows with
/// typeahead suggestions drawn from specs already used across the catalog, so
/// staff converge on consistent terms (keeps ecommerce facets clean later).
class ProductSpecsEditScreen extends ConsumerStatefulWidget {
  const ProductSpecsEditScreen({super.key, required this.productId});

  final String productId;

  @override
  ConsumerState<ProductSpecsEditScreen> createState() =>
      _ProductSpecsEditScreenState();
}

class _Row {
  _Row({String label = '', String value = ''})
      : label = TextEditingController(text: label),
        value = TextEditingController(text: value);

  final TextEditingController label;
  final TextEditingController value;

  void dispose() {
    label.dispose();
    value.dispose();
  }
}

class _ProductSpecsEditScreenState
    extends ConsumerState<ProductSpecsEditScreen> {
  final List<_Row> _rows = [];
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
    for (final r in _rows) {
      r.dispose();
    }
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _loadError = null;
    });
    try {
      final specs = await ref
          .read(productsRepositoryProvider)
          .getSpecifications(widget.productId);
      if (!mounted) return;
      setState(() {
        _rows
          ..clear()
          ..addAll(
              specs.map((s) => _Row(label: s.label, value: s.value)));
        if (_rows.isEmpty) _rows.add(_Row());
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

  void _addRow() => setState(() => _rows.add(_Row()));

  void _removeRow(int i) {
    setState(() {
      _rows.removeAt(i).dispose();
      if (_rows.isEmpty) _rows.add(_Row());
    });
  }

  Future<void> _save() async {
    final specs = <ProductSpecification>[];
    for (final r in _rows) {
      final label = r.label.text.trim();
      if (label.isEmpty) continue;
      specs.add(ProductSpecification(label: label, value: r.value.text.trim()));
    }

    final messenger = ScaffoldMessenger.of(context);
    final router = GoRouter.of(context);
    setState(() => _saving = true);
    try {
      await ref
          .read(productsRepositoryProvider)
          .updateSpecifications(widget.productId, specs);
      ref.invalidate(productSpecificationsProvider(widget.productId));
      messenger.showSnackBar(const SnackBar(
        content: Text('Specifications saved'),
        duration: Duration(seconds: 2),
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

  Future<List<String>> _suggest(String field, String query,
      {String? labelKey}) async {
    try {
      return await ref.read(productsRepositoryProvider).specificationSuggestions(
            field: field,
            query: query,
            labelKey: labelKey,
          );
    } on AppException {
      return const [];
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(
          'Specifications',
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
                      child: ReorderableListView.builder(
                        padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
                        itemCount: _rows.length,
                        onReorderItem: (oldIndex, newIndex) {
                          setState(() {
                            final row = _rows.removeAt(oldIndex);
                            _rows.insert(newIndex, row);
                          });
                        },
                        footer: Padding(
                          padding: const EdgeInsets.only(top: 8),
                          child: OutlinedButton.icon(
                            onPressed: _addRow,
                            icon: const Icon(Icons.add, size: 18),
                            label: const Text('Add specification'),
                            style: OutlinedButton.styleFrom(
                              foregroundColor: context.colors.ink,
                              side: BorderSide(
                                  color:
                                      Theme.of(context).colorScheme.outline),
                              minimumSize: const Size.fromHeight(46),
                              shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(12)),
                              textStyle: GoogleFonts.instrumentSans(
                                  fontSize: 13.5,
                                  fontWeight: FontWeight.w600),
                            ),
                          ),
                        ),
                        itemBuilder: (context, i) => _SpecRowEditor(
                          key: ObjectKey(_rows[i]),
                          index: i,
                          row: _rows[i],
                          onRemove: () => _removeRow(i),
                          labelSuggest: (q) => _suggest('label', q),
                          valueSuggest: (q) => _suggest('value', q,
                              labelKey: _rows[i].label.text.trim()),
                        ),
                      ),
                    ),
                    PrimaryCtaBar(
                      label: 'Save specifications',
                      isLoading: _saving,
                      onTap: _save,
                    ),
                  ],
                ),
    );
  }
}

class _SpecRowEditor extends StatelessWidget {
  const _SpecRowEditor({
    super.key,
    required this.index,
    required this.row,
    required this.onRemove,
    required this.labelSuggest,
    required this.valueSuggest,
  });

  final int index;
  final _Row row;
  final VoidCallback onRemove;
  final Future<List<String>> Function(String query) labelSuggest;
  final Future<List<String>> Function(String query) valueSuggest;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          ReorderableDragStartListener(
            index: index,
            child: Padding(
              padding: const EdgeInsets.only(right: 6),
              child: Icon(Icons.drag_indicator,
                  size: 20, color: scheme.onSurface.withAlpha(90)),
            ),
          ),
          Expanded(
            flex: 4,
            child: _SuggestField(
              controller: row.label,
              hint: 'Label',
              suggest: labelSuggest,
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            flex: 5,
            child: _SuggestField(
              controller: row.value,
              hint: 'Value',
              suggest: valueSuggest,
            ),
          ),
          IconButton(
            tooltip: 'Remove',
            icon: Icon(Icons.close, size: 18, color: scheme.onSurface.withAlpha(140)),
            onPressed: onRemove,
          ),
        ],
      ),
    );
  }
}

/// A text field with an Autocomplete overlay backed by an async suggestion
/// source. Keeps its own controller in sync with the row's controller.
class _SuggestField extends StatelessWidget {
  const _SuggestField({
    required this.controller,
    required this.hint,
    required this.suggest,
  });

  final TextEditingController controller;
  final String hint;
  final Future<List<String>> Function(String query) suggest;

  @override
  Widget build(BuildContext context) {
    return RawAutocomplete<String>(
      textEditingController: controller,
      focusNode: FocusNode(),
      optionsBuilder: (value) async {
        if (value.text.trim().isEmpty) return const Iterable<String>.empty();
        final options = await suggest(value.text.trim());
        // Drop an exact match — no point suggesting what's already typed.
        return options.where(
            (o) => o.toLowerCase() != value.text.trim().toLowerCase());
      },
      fieldViewBuilder:
          (context, textController, focusNode, onFieldSubmitted) {
        return TextField(
          controller: textController,
          focusNode: focusNode,
          textCapitalization: TextCapitalization.words,
          style: GoogleFonts.instrumentSans(fontSize: 13.5),
          decoration: InputDecoration(
            hintText: hint,
            isDense: true,
            contentPadding:
                const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
          ),
        );
      },
      optionsViewBuilder: (context, onSelected, options) {
        final scheme = Theme.of(context).colorScheme;
        return Align(
          alignment: Alignment.topLeft,
          child: Material(
            elevation: 4,
            borderRadius: BorderRadius.circular(10),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxHeight: 200, maxWidth: 260),
              child: ListView.builder(
                padding: EdgeInsets.zero,
                shrinkWrap: true,
                itemCount: options.length,
                itemBuilder: (context, i) {
                  final option = options.elementAt(i);
                  return InkWell(
                    onTap: () => onSelected(option),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 12, vertical: 10),
                      child: Text(
                        option,
                        style: GoogleFonts.instrumentSans(
                            fontSize: 13, color: scheme.onSurface),
                      ),
                    ),
                  );
                },
              ),
            ),
          ),
        );
      },
    );
  }
}
