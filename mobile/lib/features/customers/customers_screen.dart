import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/customer.dart';
import '../../shared/widgets/app_scaffold.dart';
import '../../shared/widgets/design_system.dart';
import '../../shared/widgets/state_views.dart';
import 'customers_providers.dart';

class CustomersScreen extends ConsumerStatefulWidget {
  const CustomersScreen({super.key});

  @override
  ConsumerState<CustomersScreen> createState() => _CustomersScreenState();
}

class _CustomersScreenState extends ConsumerState<CustomersScreen> {
  final _searchCtrl = TextEditingController();
  final _scrollCtrl = ScrollController();
  int _filterIndex = 0;

  @override
  void initState() {
    super.initState();
    _scrollCtrl.addListener(_onScroll);
    _searchCtrl.addListener(() => setState(() {}));
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(customerListControllerProvider.notifier).search('');
    });
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    _scrollCtrl.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scrollCtrl.position.pixels >=
        _scrollCtrl.position.maxScrollExtent - 300) {
      ref.read(customerListControllerProvider.notifier).loadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(customerListControllerProvider);
    final controller = ref.read(customerListControllerProvider.notifier);

    return AppScaffold(
      title: 'Customers',
      showBottomNav: true,
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
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
            child: SearchInput(
              controller: _searchCtrl,
              hintText: 'Search name, phone or code...',
              onChanged: controller.search,
            ),
          ),
          const SizedBox(height: 10),

          // Filter chips
          FilterChipRow(
            selected: _filterIndex,
            onSelect: (i) => setState(() => _filterIndex = i),
            chips: const [
              FilterChipData(label: 'All'),
              FilterChipData(
                label: 'With due',
                inactiveColor: AppColors.red,
                inactiveBg: AppColors.redBg,
                inactiveBorder: AppColors.redBorder,
              ),
              FilterChipData(label: 'Workshops'),
            ],
          ),
          const SizedBox(height: 8),

          Expanded(child: _buildBody(state, controller)),
        ],
      ),
    );
  }

  Widget _buildBody(
      CustomerListState state, CustomerListController controller) {
    if (state.isLoading) return const LoadingView();
    if (state.error != null) {
      return ErrorView(
        message: state.error!,
        onRetry: () => controller.search(state.query),
      );
    }

    final filtered = _filterIndex == 1
        ? state.items.where((c) => c.hasDue).toList()
        : state.items;

    if (filtered.isEmpty && !state.isLoading) {
      return const EmptyView(
          message: 'No customers found.', icon: Icons.person_off_outlined);
    }

    return RefreshIndicator(
      onRefresh: controller.refresh,
      child: ListView.builder(
        controller: _scrollCtrl,
        padding: const EdgeInsets.fromLTRB(16, 0, 16, 90),
        itemCount: filtered.length + (state.hasMore ? 1 : 0),
        itemBuilder: (context, index) {
          if (index >= filtered.length) {
            return const Padding(
              padding: EdgeInsets.all(16),
              child: Center(child: CircularProgressIndicator()),
            );
          }
          return _CustomerCard(customer: filtered[index]);
        },
      ),
    );
  }
}

// â”€â”€ Customer card â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

class _CustomerCard extends StatelessWidget {
  const _CustomerCard({required this.customer});

  final Customer customer;

  @override
  Widget build(BuildContext context) {
    final phone = customer.phone ?? '';

    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Material(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(13),
        child: InkWell(
          borderRadius: BorderRadius.circular(13),
          onTap: () => context.push('/customers/${customer.id}'),
          child: Container(
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(13),
              border: Border.all(color: Theme.of(context).colorScheme.outline),
            ),
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
            child: Row(
              children: [
                // Circular initials avatar
                InitialsAvatar(name: customer.fullName, radius: 20),
                const SizedBox(width: 12),

                // Name + subtitle
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        customer.fullName,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: GoogleFonts.instrumentSans(
                          fontSize: 13.5,
                          fontWeight: FontWeight.w500
                        ),
                      ),
                      if (phone.isNotEmpty) ...[
                        const SizedBox(height: 2),
                        Text(
                          phone,
                          style: GoogleFonts.instrumentSans(
                            fontSize: 11.5
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
                const SizedBox(width: 12),

                // Due amount or chevron
                if (customer.hasDue)
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        formatCurrency(customer.dueAmount),
                        style: GoogleFonts.instrumentSans(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                          color: AppColors.red,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        'Due',
                        style: GoogleFonts.instrumentSans(
                          fontSize: 10.5
                        ),
                      ),
                    ],
                  )
                else
                  const Icon(Icons.chevron_right,
                      color: AppColors.disabled, size: 20),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
