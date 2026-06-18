import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../shared/format.dart';
import '../../shared/models/customer.dart';
import '../../shared/widgets/app_scaffold.dart';
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
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(12),
            child: TextField(
              controller: _searchCtrl,
              textInputAction: TextInputAction.search,
              onSubmitted: controller.search,
              decoration: InputDecoration(
                hintText: 'Search name, phone or code',
                prefixIcon: const Icon(Icons.search),
                border: const OutlineInputBorder(),
                suffixIcon: _searchCtrl.text.isEmpty
                    ? null
                    : IconButton(
                        icon: const Icon(Icons.clear),
                        onPressed: () {
                          _searchCtrl.clear();
                          controller.search('');
                        },
                      ),
              ),
            ),
          ),
          Expanded(child: _buildBody(state, controller)),
        ],
      ),
    );
  }

  Widget _buildBody(CustomerListState state, CustomerListController controller) {
    if (state.isLoading) return const LoadingView();
    if (state.error != null) {
      return ErrorView(
        message: state.error!,
        onRetry: () => controller.search(state.query),
      );
    }
    if (state.isEmpty) {
      return const EmptyView(
          message: 'No customers found.', icon: Icons.person_off_outlined);
    }

    return RefreshIndicator(
      onRefresh: controller.refresh,
      child: ListView.separated(
        controller: _scrollCtrl,
        itemCount: state.items.length + (state.hasMore ? 1 : 0),
        separatorBuilder: (_, _) => const Divider(height: 1),
        itemBuilder: (context, index) {
          if (index >= state.items.length) {
            return const Padding(
              padding: EdgeInsets.all(16),
              child: Center(child: CircularProgressIndicator()),
            );
          }
          return _CustomerTile(customer: state.items[index]);
        },
      ),
    );
  }
}

class _CustomerTile extends StatelessWidget {
  const _CustomerTile({required this.customer});

  final Customer customer;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final initial = customer.fullName.trim().isEmpty
        ? '?'
        : customer.fullName.trim().characters.first.toUpperCase();
    final subtitle = [
      if (customer.companyName != null && customer.companyName!.isNotEmpty)
        customer.companyName!,
      if (customer.phone != null && customer.phone!.isNotEmpty) customer.phone!,
    ].join('  •  ');

    return ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      leading: CircleAvatar(
        backgroundColor: scheme.primaryContainer,
        child: Text(initial,
            style: TextStyle(
                color: scheme.onPrimaryContainer,
                fontWeight: FontWeight.w600)),
      ),
      title: Text(customer.fullName,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(fontWeight: FontWeight.w600)),
      subtitle: subtitle.isEmpty ? null : Text(subtitle),
      trailing: customer.hasDue
          ? _DueBadge(amount: customer.dueAmount)
          : Icon(Icons.chevron_right, color: scheme.outline),
      onTap: () => context.push('/customers/${customer.id}'),
    );
  }
}

class _DueBadge extends StatelessWidget {
  const _DueBadge({required this.amount});

  final double amount;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Column(
      mainAxisAlignment: MainAxisAlignment.center,
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        Text('Due', style: TextStyle(fontSize: 11, color: scheme.error)),
        Text(
          formatCurrency(amount),
          style: TextStyle(fontWeight: FontWeight.w700, color: scheme.error),
        ),
      ],
    );
  }
}
