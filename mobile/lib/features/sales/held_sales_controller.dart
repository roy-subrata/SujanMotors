import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../shared/models/sale.dart';

/// A parked cart the cashier can resume later.
class HeldCart {
  const HeldCart({
    required this.id,
    required this.label,
    required this.createdAt,
    required this.items,
  });

  final String id;
  final String label;
  final DateTime createdAt;
  final List<QuickSaleItem> items;

  int get itemCount => items.fold(0, (s, i) => s + i.quantity);
  double get total => items.fold(0.0, (s, i) => s + i.lineTotal);

  Map<String, dynamic> toJson() => {
        'id': id,
        'label': label,
        'createdAt': createdAt.toIso8601String(),
        'items': items.map((i) => i.toJson()).toList(),
      };

  factory HeldCart.fromJson(Map<String, dynamic> json) => HeldCart(
        id: json['id'] as String,
        label: (json['label'] ?? '') as String,
        createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ??
            DateTime.now(),
        items: ((json['items'] as List?) ?? [])
            .whereType<Map>()
            .map((m) => QuickSaleItem.fromJson(Map<String, dynamic>.from(m)))
            .toList(),
      );
}

/// Owns the list of held carts, persisted locally so they survive app restarts.
class HeldSalesController extends Notifier<List<HeldCart>> {
  static const _key = 'held_carts';
  static const _storage = FlutterSecureStorage();

  @override
  List<HeldCart> build() {
    Future.microtask(_load);
    return const [];
  }

  Future<void> _load() async {
    final raw = await _storage.read(key: _key);
    if (raw == null || raw.isEmpty) return;
    try {
      final list = jsonDecode(raw) as List;
      state = list
          .whereType<Map>()
          .map((m) => HeldCart.fromJson(Map<String, dynamic>.from(m)))
          .toList();
    } catch (_) {
      // Corrupt payload — drop it rather than crash the POS.
      state = const [];
    }
  }

  Future<void> _persist() async {
    await _storage.write(
      key: _key,
      value: jsonEncode(state.map((c) => c.toJson()).toList()),
    );
  }

  /// Parks [items] under [label]; newest first.
  Future<void> hold(String label, List<QuickSaleItem> items) async {
    if (items.isEmpty) return;
    final cart = HeldCart(
      id: DateTime.now().microsecondsSinceEpoch.toString(),
      label: label.trim().isEmpty ? 'Held sale' : label.trim(),
      createdAt: DateTime.now(),
      items: items,
    );
    state = [cart, ...state];
    await _persist();
  }

  Future<void> remove(String id) async {
    state = state.where((c) => c.id != id).toList();
    await _persist();
  }
}

final heldSalesProvider =
    NotifierProvider<HeldSalesController, List<HeldCart>>(
        HeldSalesController.new);
