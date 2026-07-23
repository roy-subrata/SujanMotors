import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../core/i18n/strings.dart';
import '../../core/network/app_exception.dart';
import '../products/products_repository.dart';

class ScannerScreen extends ConsumerStatefulWidget {
  const ScannerScreen({super.key});

  @override
  ConsumerState<ScannerScreen> createState() => _ScannerScreenState();
}

class _ScannerScreenState extends ConsumerState<ScannerScreen> {
  final MobileScannerController _controller = MobileScannerController(
    detectionSpeed: DetectionSpeed.noDuplicates,
  );

  bool _handling = false;
  String? _error;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _onDetect(BarcodeCapture capture) async {
    if (_handling) return;
    final code = capture.barcodes
        .map((b) => b.rawValue)
        .firstWhere((v) => v != null && v.isNotEmpty, orElse: () => null);
    if (code == null) return;

    setState(() {
      _handling = true;
      _error = null;
    });
    await _controller.stop();

    try {
      final product =
          await ref.read(productsRepositoryProvider).getByCode(code);
      if (!mounted) return;
      context.pushReplacement('/product/${product.productId}');
    } on AppException catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.message;
        _handling = false;
      });
      await _controller.start();
    }
  }

  Future<void> _retryCamera() async {
    try {
      await _controller.start();
    } on MobileScannerException {
      // The errorBuilder will rebuild with the new error; nothing else to do.
    }
  }

  /// Friendly full-screen state shown when the camera cannot start
  /// (permission denied, unsupported device, or a generic camera error).
  Widget _buildCameraError(BuildContext context, MobileScannerException error) {
    final s = S.of(context);
    final isPermission =
        error.errorCode == MobileScannerErrorCode.permissionDenied;
    final isUnsupported =
        error.errorCode == MobileScannerErrorCode.unsupported;

    final String title;
    final String message;
    if (isPermission) {
      title = s.cameraAccessNeeded;
      message = s.allowCameraAccessBody;
    } else if (isUnsupported) {
      title = s.scanningNotSupported;
      message = s.scanningNotSupportedBody;
    } else {
      title = s.cameraError;
      message = s.cameraErrorBody(error.errorDetails?.message ?? '').trim();
    }

    return Container(
      color: Colors.black,
      alignment: Alignment.center,
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            isPermission ? Icons.no_photography : Icons.error_outline,
            color: Colors.white70,
            size: 64,
          ),
          const SizedBox(height: 16),
          Text(
            title,
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 18,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            message,
            textAlign: TextAlign.center,
            style: const TextStyle(color: Colors.white70),
          ),
          if (!isUnsupported) ...[
            const SizedBox(height: 20),
            FilledButton.icon(
              onPressed: _retryCamera,
              icon: const Icon(Icons.refresh),
              label: Text(s.tryAgain),
            ),
          ],
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    return Scaffold(
      appBar: AppBar(
        // Explicit back with a fallback: the old gradient app bar hid the
        // automatic arrow (dark icon on dark gradient), leaving no way out.
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () =>
              context.canPop() ? context.pop() : context.go('/products'),
        ),
        title: Text(s.scanBarcode),
        actions: [
          IconButton(
            icon: const Icon(Icons.flash_on),
            onPressed: () => _controller.toggleTorch(),
            tooltip: s.torch,
          ),
          IconButton(
            icon: const Icon(Icons.cameraswitch),
            onPressed: () => _controller.switchCamera(),
            tooltip: s.switchCamera,
          ),
        ],
      ),
      body: Stack(
        alignment: Alignment.center,
        children: [
          MobileScanner(
            controller: _controller,
            onDetect: _onDetect,
            errorBuilder: _buildCameraError,
          ),
          // Simple viewfinder overlay.
          Container(
            width: 250,
            height: 160,
            decoration: BoxDecoration(
              border: Border.all(color: Colors.white, width: 2),
              borderRadius: BorderRadius.circular(12),
            ),
          ),
          if (_handling)
            const Positioned(
              bottom: 48,
              child: CircularProgressIndicator(),
            ),
          if (_error != null)
            Positioned(
              bottom: 32,
              left: 24,
              right: 24,
              child: Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.red.shade700,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  _error!,
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: Colors.white),
                ),
              ),
            ),
        ],
      ),
    );
  }
}
