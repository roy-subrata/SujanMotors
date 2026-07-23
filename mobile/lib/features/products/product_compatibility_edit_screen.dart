import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/i18n/strings.dart';
import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/models/vehicle.dart';
import '../../shared/models/vehicle_compatibility.dart';
import '../../shared/widgets/design_system.dart';
import '../../shared/widgets/state_views.dart';
import 'products_providers.dart';
import 'products_repository.dart';

/// Manage the vehicles a product fits — add via a search picker, remove inline.
class ProductCompatibilityEditScreen extends ConsumerStatefulWidget {
  const ProductCompatibilityEditScreen({super.key, required this.productId});

  final String productId;

  @override
  ConsumerState<ProductCompatibilityEditScreen> createState() =>
      _ProductCompatibilityEditScreenState();
}

class _ProductCompatibilityEditScreenState
    extends ConsumerState<ProductCompatibilityEditScreen> {
  bool _busy = false;

  Future<void> _add() async {
    final existing =
        ref.read(compatibleVehiclesProvider(widget.productId)).value ??
            const <VehicleCompatibility>[];
    final vehicle = await showModalBottomSheet<Vehicle>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _VehiclePickerSheet(
        excludeVehicleIds: existing.map((c) => c.vehicleId).toSet(),
      ),
    );
    if (vehicle == null || !mounted) return;

    final messenger = ScaffoldMessenger.of(context);
    final errorColor = context.colors.red;
    setState(() => _busy = true);
    try {
      await ref.read(productsRepositoryProvider).addCompatibility(
            partId: widget.productId,
            vehicleId: vehicle.id,
          );
      ref.invalidate(compatibleVehiclesProvider(widget.productId));
    } on AppException catch (e) {
      messenger.showSnackBar(SnackBar(
        content: Text(e.message),
        backgroundColor: errorColor,
        behavior: SnackBarBehavior.floating,
      ));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _remove(VehicleCompatibility c) async {
    final messenger = ScaffoldMessenger.of(context);
    final errorColor = context.colors.red;
    setState(() => _busy = true);
    try {
      await ref
          .read(productsRepositoryProvider)
          .removeCompatibility(c.id);
      ref.invalidate(compatibleVehiclesProvider(widget.productId));
    } on AppException catch (e) {
      messenger.showSnackBar(SnackBar(
        content: Text(e.message),
        backgroundColor: errorColor,
        behavior: SnackBarBehavior.floating,
      ));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final async = ref.watch(compatibleVehiclesProvider(widget.productId));

    return Scaffold(
      appBar: AppBar(
        title: Text(
          s.compatibleVehicles,
          style: GoogleFonts.instrumentSans(
              fontSize: 16, fontWeight: FontWeight.w700),
        ),
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _busy ? null : _add,
        backgroundColor: context.colors.ink,
        foregroundColor: context.colors.onInk,
        icon: const Icon(Icons.add),
        label: Text(s.addVehicle,
            style: GoogleFonts.instrumentSans(fontWeight: FontWeight.w600)),
      ),
      body: async.when(
        loading: () => const LoadingView(),
        error: (e, _) => ListView(children: [
          const SizedBox(height: 120),
          ErrorView(
            message: e is AppException ? e.message : s.failedToLoadLabel,
            onRetry: () =>
                ref.invalidate(compatibleVehiclesProvider(widget.productId)),
          ),
        ]),
        data: (items) {
          if (items.isEmpty) {
            return EmptyView(
              message: s.noCompatibleVehiclesMessage,
              icon: Icons.directions_car_outlined,
            );
          }
          return ListView.separated(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 96),
            itemCount: items.length,
            separatorBuilder: (_, _) => const SizedBox(height: 8),
            itemBuilder: (context, i) {
              final c = items[i];
              return ListCard(
                child: Row(
                  children: [
                    Container(
                      width: 36,
                      height: 36,
                      decoration: BoxDecoration(
                        color: context.colors.greenBg,
                        borderRadius: BorderRadius.circular(10),
                      ),
                      alignment: Alignment.center,
                      child: Icon(Icons.directions_car_outlined,
                          size: 18, color: context.colors.green),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(c.title,
                              style: GoogleFonts.instrumentSans(
                                  fontSize: 13.5,
                                  fontWeight: FontWeight.w600)),
                          if ((c.engineType ?? '').isNotEmpty) ...[
                            const SizedBox(height: 2),
                            Text(c.engineType!,
                                style: GoogleFonts.instrumentSans(
                                    fontSize: 11.5, color: context.colors.muted)),
                          ],
                        ],
                      ),
                    ),
                    IconButton(
                      tooltip: s.remove,
                      icon: Icon(Icons.close,
                          size: 18,
                          color: Theme.of(context)
                              .colorScheme
                              .onSurface
                              .withAlpha(140)),
                      onPressed: _busy ? null : () => _remove(c),
                    ),
                  ],
                ),
              );
            },
          );
        },
      ),
    );
  }
}

/// Search-and-pick sheet for vehicles; returns the chosen [Vehicle].
class _VehiclePickerSheet extends ConsumerStatefulWidget {
  const _VehiclePickerSheet({required this.excludeVehicleIds});

  final Set<String> excludeVehicleIds;

  @override
  ConsumerState<_VehiclePickerSheet> createState() =>
      _VehiclePickerSheetState();
}

class _VehiclePickerSheetState extends ConsumerState<_VehiclePickerSheet> {
  final _searchCtrl = TextEditingController();
  Timer? _debounce;
  List<Vehicle> _results = const [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _search('');
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _searchCtrl.dispose();
    super.dispose();
  }

  void _onChanged(String v) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 350), () => _search(v.trim()));
  }

  Future<void> _search(String query) async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final all =
          await ref.read(productsRepositoryProvider).searchVehicles(query);
      if (!mounted) return;
      setState(() {
        _results = all
            .where((v) => !widget.excludeVehicleIds.contains(v.id))
            .toList();
        _loading = false;
      });
    } on AppException catch (e) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _error = e.message;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final scheme = Theme.of(context).colorScheme;
    return DraggableScrollableSheet(
      initialChildSize: 0.75,
      minChildSize: 0.5,
      maxChildSize: 0.95,
      expand: false,
      builder: (context, scrollController) => Container(
        decoration: BoxDecoration(
          color: scheme.surface,
          borderRadius:
              const BorderRadius.vertical(top: Radius.circular(22)),
        ),
        padding: EdgeInsets.only(
            bottom: MediaQuery.viewInsetsOf(context).bottom),
        child: Column(
          children: [
            const SizedBox(height: 10),
            Container(
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: scheme.outline,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 14, 16, 10),
              child: SearchInput(
                controller: _searchCtrl,
                autofocus: true,
                hintText: s.searchVehicleHint,
                onChanged: _onChanged,
              ),
            ),
            Expanded(
              child: _loading
                  ? const LoadingView()
                  : _error != null
                      ? ErrorView(
                          message: _error!,
                          onRetry: () => _search(_searchCtrl.text.trim()))
                      : _results.isEmpty
                          ? EmptyView(
                              message: s.noVehiclesFound,
                              icon: Icons.directions_car_outlined,
                            )
                          : ListView.separated(
                              controller: scrollController,
                              padding:
                                  const EdgeInsets.fromLTRB(16, 0, 16, 16),
                              itemCount: _results.length,
                              separatorBuilder: (_, _) =>
                                  const SizedBox(height: 8),
                              itemBuilder: (context, i) {
                                final v = _results[i];
                                return ListCard(
                                  onTap: () => Navigator.of(context).pop(v),
                                  child: Row(
                                    children: [
                                      Expanded(
                                        child: Column(
                                          crossAxisAlignment:
                                              CrossAxisAlignment.start,
                                          children: [
                                            Text(v.label,
                                                style:
                                                    GoogleFonts.instrumentSans(
                                                        fontSize: 13.5,
                                                        fontWeight:
                                                            FontWeight.w600)),
                                            if ((v.engineType ?? '')
                                                .isNotEmpty) ...[
                                              const SizedBox(height: 2),
                                              Text(v.engineType!,
                                                  style: GoogleFonts
                                                      .instrumentSans(
                                                          fontSize: 11.5,
                                                          color: context
                                                              .colors.muted)),
                                            ],
                                          ],
                                        ),
                                      ),
                                      Icon(Icons.add,
                                          size: 18, color: context.colors.secondary),
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
