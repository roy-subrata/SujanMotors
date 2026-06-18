import 'package:flutter/material.dart';

import '../../core/network/app_exception.dart';
import '../models/paged_response.dart';
import 'state_views.dart';

/// Fetches one page of items for [page] (1-based).
typedef PageFetcher<T> = Future<PagedChunk<T>> Function(int page);

/// A reusable, server-paginated list that loads the next page from the API as
/// the user scrolls toward the bottom. Handles the initial load, load-more,
/// pull-to-refresh, and loading/error/empty states.
///
/// Pass a new [resetKey] (e.g. a filter/search signature) to reload from page 1.
/// Use this for any API-backed list so paging behaves consistently app-wide.
class PagedListView<T> extends StatefulWidget {
  const PagedListView({
    super.key,
    required this.fetch,
    required this.itemBuilder,
    this.resetKey,
    this.padding = EdgeInsets.zero,
    this.separatorBuilder,
    this.emptyBuilder,
    this.onLoaded,
  });

  final PageFetcher<T> fetch;
  final Widget Function(BuildContext context, T item) itemBuilder;

  /// Changing this reloads the list from page 1 (e.g. when a filter changes).
  final Object? resetKey;
  final EdgeInsetsGeometry padding;
  final IndexedWidgetBuilder? separatorBuilder;
  final WidgetBuilder? emptyBuilder;

  /// Fired after each successful page load with the server's total count.
  final void Function(int totalCount)? onLoaded;

  @override
  State<PagedListView<T>> createState() => _PagedListViewState<T>();
}

class _PagedListViewState<T> extends State<PagedListView<T>> {
  final _scroll = ScrollController();
  final List<T> _items = [];

  int _page = 0;
  bool _hasMore = true;
  bool _loading = false;
  bool _initialDone = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _scroll.addListener(_onScroll);
    _reload();
  }

  @override
  void didUpdateWidget(covariant PagedListView<T> old) {
    super.didUpdateWidget(old);
    if (old.resetKey != widget.resetKey) _reload();
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scroll.position.pixels >= _scroll.position.maxScrollExtent - 300) {
      _fetchNext();
    }
  }

  Future<void> _reload() async {
    setState(() {
      _items.clear();
      _page = 0;
      _hasMore = true;
      _initialDone = false;
      _error = null;
    });
    await _fetchNext();
  }

  Future<void> _fetchNext() async {
    if (_loading || !_hasMore) return;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final chunk = await widget.fetch(_page + 1);
      if (!mounted) return;
      setState(() {
        _page++;
        _items.addAll(chunk.items);
        _hasMore = chunk.hasMore;
        _loading = false;
        _initialDone = true;
      });
      widget.onLoaded?.call(chunk.totalCount);
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e is AppException ? e.message : 'Failed to load.';
        _loading = false;
        _initialDone = true;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    // Initial load.
    if (!_initialDone && _loading) return const LoadingView();

    // Hard error before anything loaded.
    if (_error != null && _items.isEmpty) {
      return ErrorView(message: _error!, onRetry: _reload);
    }

    // Empty (still allow pull-to-refresh).
    if (_items.isEmpty) {
      return RefreshIndicator(
        onRefresh: _reload,
        child: ListView(
          children: [
            const SizedBox(height: 100),
            widget.emptyBuilder?.call(context) ??
                const EmptyView(message: 'Nothing to show.'),
          ],
        ),
      );
    }

    final showTrailer = _hasMore || (_error != null);
    return RefreshIndicator(
      onRefresh: _reload,
      child: ListView.separated(
        controller: _scroll,
        padding: widget.padding,
        itemCount: _items.length + (showTrailer ? 1 : 0),
        separatorBuilder: (context, i) =>
            widget.separatorBuilder?.call(context, i) ?? const SizedBox.shrink(),
        itemBuilder: (context, index) {
          if (index < _items.length) {
            return widget.itemBuilder(context, _items[index]);
          }
          // Trailing row: a retry on error, otherwise a spinner that triggers
          // the next page as it scrolls into view.
          if (_error != null) {
            return Padding(
              padding: const EdgeInsets.all(16),
              child: Center(
                child: TextButton.icon(
                  onPressed: _fetchNext,
                  icon: const Icon(Icons.refresh),
                  label: Text(_error!),
                ),
              ),
            );
          }
          _fetchNext();
          return const Padding(
            padding: EdgeInsets.symmetric(vertical: 20),
            child: Center(child: CircularProgressIndicator()),
          );
        },
      ),
    );
  }
}
