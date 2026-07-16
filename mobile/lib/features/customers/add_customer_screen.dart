import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/widgets/design_system.dart';
import 'customers_providers.dart';
import 'customers_repository.dart';

/// Create or edit a customer (phone unique, code auto-generated). On create you
/// can also add any number of vehicles; on edit, vehicles are managed from the
/// customer detail screen so this form stays focused on the customer fields.
class AddCustomerScreen extends ConsumerStatefulWidget {
  const AddCustomerScreen({super.key, this.customerId});

  /// When set, the screen edits this customer instead of creating a new one.
  final String? customerId;

  bool get isEdit => customerId != null;

  @override
  ConsumerState<AddCustomerScreen> createState() => _AddCustomerScreenState();
}

class _VehicleRow {
  _VehicleRow();
  final make = TextEditingController();
  final model = TextEditingController();
  final year = TextEditingController();
  final regNo = TextEditingController();

  bool get isEmpty =>
      make.text.trim().isEmpty &&
      model.text.trim().isEmpty &&
      year.text.trim().isEmpty &&
      regNo.text.trim().isEmpty;

  void dispose() {
    make.dispose();
    model.dispose();
    year.dispose();
    regNo.dispose();
  }
}

class _AddCustomerScreenState extends ConsumerState<AddCustomerScreen> {
  final _formKey = GlobalKey<FormState>();
  final _firstName = TextEditingController();
  final _lastName = TextEditingController();
  final _phone = TextEditingController();
  final _email = TextEditingController();
  final _company = TextEditingController();
  final _city = TextEditingController();
  final _notes = TextEditingController();

  static const _types = ['RETAIL', 'WHOLESALE', 'CORPORATE', 'DISTRIBUTOR'];
  String _type = 'RETAIL';

  final List<_VehicleRow> _vehicles = [];
  bool _saving = false;
  bool _loading = false;

  @override
  void initState() {
    super.initState();
    if (widget.isEdit) _loadForEdit();
  }

  Future<void> _loadForEdit() async {
    setState(() => _loading = true);
    try {
      final c = await ref
          .read(customersRepositoryProvider)
          .getById(widget.customerId!);
      if (!mounted) return;
      // firstName/lastName are exposed by the API; fall back to splitting
      // fullName if an older payload omits them.
      final parts = c.fullName.trim().split(RegExp(r'\s+'));
      _firstName.text = c.firstName ??
          (parts.isNotEmpty ? parts.first : '');
      _lastName.text = c.lastName ??
          (parts.length > 1 ? parts.sublist(1).join(' ') : '');
      _phone.text = c.phone ?? '';
      _email.text = c.email ?? '';
      _company.text = c.companyName ?? '';
      _city.text = c.city ?? '';
      setState(() {
        _type = c.customerType ?? 'RETAIL';
        _loading = false;
      });
    } on AppException catch (e) {
      if (!mounted) return;
      setState(() => _loading = false);
      _showError(e.message);
    }
  }

  @override
  void dispose() {
    _firstName.dispose();
    _lastName.dispose();
    _phone.dispose();
    _email.dispose();
    _company.dispose();
    _city.dispose();
    _notes.dispose();
    for (final v in _vehicles) {
      v.dispose();
    }
    super.dispose();
  }

  String? _n(TextEditingController c) =>
      c.text.trim().isEmpty ? null : c.text.trim();

  Future<void> _save() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;

    // Every started vehicle row needs at least make + model.
    for (final v in _vehicles) {
      if (v.isEmpty) continue;
      if (v.make.text.trim().isEmpty || v.model.text.trim().isEmpty) {
        _showError('Each vehicle needs a make and model (or clear the row).');
        return;
      }
    }

    final messenger = ScaffoldMessenger.of(context);
    final router = GoRouter.of(context);
    setState(() => _saving = true);
    try {
      final repo = ref.read(customersRepositoryProvider);

      if (widget.isEdit) {
        await repo.updateCustomer(
          id: widget.customerId!,
          firstName: _firstName.text.trim(),
          lastName: _lastName.text.trim(),
          phone: _n(_phone),
          email: _n(_email),
          companyName: _n(_company),
          city: _n(_city),
          customerType: _type,
          notes: _n(_notes),
        );
        ref.invalidate(customerDetailProvider(widget.customerId!));
      } else {
        final customerId = await repo.createCustomer(
          firstName: _firstName.text.trim(),
          lastName: _lastName.text.trim(),
          phone: _n(_phone),
          email: _n(_email),
          companyName: _n(_company),
          city: _n(_city),
          customerType: _type,
          notes: _n(_notes),
        );

        for (final v in _vehicles) {
          if (v.isEmpty) continue;
          await repo.addVehicle(
            customerId: customerId,
            make: v.make.text.trim(),
            model: v.model.text.trim(),
            year: int.tryParse(v.year.text.trim()),
            registrationNo: _n(v.regNo),
          );
        }
      }

      ref.read(customerListControllerProvider.notifier).search('');
      messenger.showSnackBar(SnackBar(
        content: Text(widget.isEdit ? 'Customer updated' : 'Customer added'),
        duration: const Duration(seconds: 2),
        behavior: SnackBarBehavior.floating,
      ));
      router.pop();
    } on AppException catch (e) {
      if (!mounted) return;
      setState(() => _saving = false);
      _showError(e.message);
    }
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
      content: Text(message),
      backgroundColor: context.colors.red,
      behavior: SnackBarBehavior.floating,
    ));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.isEdit ? 'Edit customer' : 'Add customer',
            style: GoogleFonts.instrumentSans(
                fontSize: 16, fontWeight: FontWeight.w700)),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : Column(
        children: [
          Expanded(
            child: Form(
              key: _formKey,
              child: ListView(
                padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
                children: [
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: _Field(
                          label: 'First name *',
                          child: TextFormField(
                            controller: _firstName,
                            textCapitalization: TextCapitalization.words,
                            validator: (v) => (v ?? '').trim().isEmpty
                                ? 'Required'
                                : null,
                          ),
                        ),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: _Field(
                          label: 'Last name *',
                          child: TextFormField(
                            controller: _lastName,
                            textCapitalization: TextCapitalization.words,
                            validator: (v) => (v ?? '').trim().isEmpty
                                ? 'Required'
                                : null,
                          ),
                        ),
                      ),
                    ],
                  ),
                  _Field(
                    label: 'Phone * (must be unique)',
                    child: TextFormField(
                      controller: _phone,
                      keyboardType: TextInputType.phone,
                      validator: (v) =>
                          (v ?? '').trim().isEmpty ? 'Phone is required' : null,
                    ),
                  ),
                  _Field(
                    label: 'Customer type',
                    child: DropdownButtonFormField<String>(
                      initialValue: _type,
                      isExpanded: true,
                      items: [
                        for (final t in _types)
                          DropdownMenuItem(
                            value: t,
                            child: Text(
                              '${t[0]}${t.substring(1).toLowerCase()}',
                              style:
                                  GoogleFonts.instrumentSans(fontSize: 13.5),
                            ),
                          ),
                      ],
                      onChanged: (v) => setState(() => _type = v ?? 'RETAIL'),
                    ),
                  ),
                  _Field(
                    label: 'Company',
                    child: TextFormField(
                      controller: _company,
                      textCapitalization: TextCapitalization.words,
                    ),
                  ),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: _Field(
                          label: 'Email',
                          child: TextFormField(
                            controller: _email,
                            keyboardType: TextInputType.emailAddress,
                          ),
                        ),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: _Field(
                          label: 'City',
                          child: TextFormField(
                            controller: _city,
                            textCapitalization: TextCapitalization.words,
                          ),
                        ),
                      ),
                    ],
                  ),
                  _Field(
                    label: 'Notes',
                    child: TextFormField(
                      controller: _notes,
                      maxLines: 2,
                      textCapitalization: TextCapitalization.sentences,
                    ),
                  ),

                  // Vehicles are added at creation; on edit they're managed
                  // from the customer detail screen.
                  if (!widget.isEdit) ...[
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        Expanded(
                          child: Text('Vehicles',
                              style: GoogleFonts.instrumentSans(
                                  fontSize: 14,
                                  fontWeight: FontWeight.w700)),
                        ),
                        Text('optional',
                            style: GoogleFonts.instrumentSans(
                                fontSize: 12, color: context.colors.muted)),
                      ],
                    ),
                    const SizedBox(height: 8),
                    for (final (i, v) in _vehicles.indexed) ...[
                      _VehicleEditor(
                        key: ObjectKey(v),
                        row: v,
                        index: i + 1,
                        onRemove: () =>
                            setState(() => _vehicles.removeAt(i).dispose()),
                      ),
                      const SizedBox(height: 8),
                    ],
                    OutlinedButton.icon(
                      onPressed: () =>
                          setState(() => _vehicles.add(_VehicleRow())),
                      icon: const Icon(Icons.add, size: 18),
                      label: const Text('Add vehicle'),
                      style: OutlinedButton.styleFrom(
                        foregroundColor: context.colors.ink,
                        side: BorderSide(
                            color: Theme.of(context).colorScheme.outline),
                        minimumSize: const Size.fromHeight(46),
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12)),
                        textStyle: GoogleFonts.instrumentSans(
                            fontSize: 13.5, fontWeight: FontWeight.w600),
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ),
          PrimaryCtaBar(
            label: widget.isEdit ? 'Save changes' : 'Save customer',
            isLoading: _saving,
            onTap: _save,
          ),
        ],
      ),
    );
  }
}

class _VehicleEditor extends StatelessWidget {
  const _VehicleEditor({
    super.key,
    required this.row,
    required this.index,
    required this.onRemove,
  });

  final _VehicleRow row;
  final int index;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Container(
      decoration: BoxDecoration(
        color: scheme.surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: scheme.outline),
      ),
      padding: const EdgeInsets.fromLTRB(12, 8, 6, 12),
      child: Column(
        children: [
          Row(
            children: [
              Text('Vehicle $index',
                  style: GoogleFonts.instrumentSans(
                      fontSize: 12.5,
                      fontWeight: FontWeight.w600,
                      color: context.colors.secondary)),
              const Spacer(),
              IconButton(
                tooltip: 'Remove',
                visualDensity: VisualDensity.compact,
                icon: Icon(Icons.close,
                    size: 18, color: scheme.onSurface.withAlpha(140)),
                onPressed: onRemove,
              ),
            ],
          ),
          Row(
            children: [
              Expanded(
                child: TextFormField(
                  controller: row.make,
                  textCapitalization: TextCapitalization.words,
                  style: GoogleFonts.instrumentSans(fontSize: 13.5),
                  decoration: const InputDecoration(
                      labelText: 'Make', isDense: true),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: TextFormField(
                  controller: row.model,
                  textCapitalization: TextCapitalization.words,
                  style: GoogleFonts.instrumentSans(fontSize: 13.5),
                  decoration: const InputDecoration(
                      labelText: 'Model', isDense: true),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              SizedBox(
                width: 90,
                child: TextFormField(
                  controller: row.year,
                  keyboardType: TextInputType.number,
                  style: GoogleFonts.instrumentSans(fontSize: 13.5),
                  decoration: const InputDecoration(
                      labelText: 'Year', isDense: true),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: TextFormField(
                  controller: row.regNo,
                  textCapitalization: TextCapitalization.characters,
                  style: GoogleFonts.instrumentSans(fontSize: 13.5),
                  decoration: const InputDecoration(
                      labelText: 'Reg. no.', isDense: true),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

/// Muted label above the input.
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
