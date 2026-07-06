import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/theme/app_theme.dart';
import '../../shared/widgets/app_scaffold.dart';
import '../../shared/widgets/design_system.dart';

/// Supplier list screen â€” placeholder pending backend supplier API.
class SuppliersScreen extends StatefulWidget {
  const SuppliersScreen({super.key});

  @override
  State<SuppliersScreen> createState() => _SuppliersScreenState();
}

class _SuppliersScreenState extends State<SuppliersScreen> {
  int _filterIndex = 0;

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      title: 'Suppliers',
      showNotificationBell: true,
      floatingActionButton: FloatingActionButton(
        onPressed: () {},
        backgroundColor: AppColors.ink,
        foregroundColor: Colors.white,
        shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16)),
        child: const Icon(Icons.add),
      ),
      body: Column(
        children: [
          // Search
          const Padding(
            padding: EdgeInsets.fromLTRB(16, 12, 16, 0),
            child: SearchInput(hintText: 'Search suppliers...'),
          ),
          const SizedBox(height: 10),

          // Filter chips
          FilterChipRow(
            selected: _filterIndex,
            onSelect: (i) => setState(() => _filterIndex = i),
            chips: const [
              FilterChipData(label: 'All'),
              FilterChipData(
                label: 'We owe',
                inactiveColor: AppColors.amber,
                inactiveBg: AppColors.amberBg,
                inactiveBorder: AppColors.amberBorder,
              ),
            ],
          ),
          const SizedBox(height: 12),

          Expanded(
            child: Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.store_outlined,
                      size: 56, color: AppColors.disabled),
                  const SizedBox(height: 14),
                  Text(
                    'No suppliers yet',
                    style: GoogleFonts.instrumentSans(
                      fontSize: 15,
                      fontWeight: FontWeight.w600
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    'Suppliers will appear here',
                    style: GoogleFonts.instrumentSans(
                        fontSize: 13, color: AppColors.muted),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
