import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';

/// App strings for the supported languages (English / Bengali).
///
/// Usage: `S.of(context).products`. New screens add a getter here with both
/// translations — screens not yet migrated simply keep their English literals
/// until they are.
class S {
  const S(this.locale);

  final Locale locale;

  static S of(BuildContext context) =>
      Localizations.of<S>(context, S) ?? const S(Locale('en'));

  static const delegate = _SDelegate();

  static const supportedLocales = [Locale('en'), Locale('bn')];

  bool get _bn => locale.languageCode == 'bn';

  String _t(String en, String bn) => _bn ? bn : en;

  // ── Navigation chrome ───────────────────────────────────────────────────
  String get home => _t('Home', 'হোম');
  String get dashboard => _t('Dashboard', 'ড্যাশবোর্ড');
  String get products => _t('Products', 'পণ্য');
  String get customers => _t('Customers', 'গ্রাহক');
  String get sales => _t('Sales', 'বিক্রয়');
  String get suppliers => _t('Suppliers', 'সরবরাহকারী');
  String get cashBook => _t('Cash Book', 'ক্যাশ বুক');
  String get tillSession => _t('Till Session', 'টিল সেশন');
  String get stockIn => _t('Stock In', 'স্টক ইন');
  String get newStockIn => _t('New Stock In', 'নতুন স্টক ইন');
  String get notifications => _t('Notifications', 'নোটিফিকেশন');
  String get logOut => _t('Log out', 'লগ আউট');
  String get language => _t('Language', 'ভাষা');

  // ── Common actions ──────────────────────────────────────────────────────
  String get cart => _t('Cart', 'কার্ট');
  String get scanBarcode => _t('Scan barcode', 'বারকোড স্ক্যান');
  String get search => _t('Search', 'খুঁজুন');
  String get switchToLight => _t('Switch to light', 'লাইট মোডে যান');
  String get switchToDark => _t('Switch to dark', 'ডার্ক মোডে যান');

  // ── Dashboard ───────────────────────────────────────────────────────────
  String get totalRevenue => _t('Total Revenue', 'মোট আয়');
  String ordersCount(int n) => _t('$n orders', '$nটি অর্ডার');
  String get cash => _t('Cash', 'নগদ');
  String get credit => _t('Credit', 'বাকি');
  String get grossProfit => _t('Gross Profit', 'মোট লাভ');
  String get netProfit => _t('Net Profit', 'নিট লাভ');
  String get customerDue => _t('Customer Due', 'গ্রাহক বকেয়া');
  String get overdue => _t('Overdue', 'মেয়াদোত্তীর্ণ');
  String get cashFlow => _t('Cash Flow', 'নগদ প্রবাহ');
  String get opening => _t('Opening', 'প্রারম্ভিক');
  String get flowIn => _t('In', 'জমা');
  String get flowOut => _t('Out', 'খরচ');
  String get closing => _t('Closing', 'সমাপনী');
  String get quickActions => _t('Quick Actions', 'দ্রুত অ্যাকশন');
  String get newSale => _t('New Sale', 'নতুন বিক্রয়');
  String get topProducts => _t('Top Products', 'শীর্ষ পণ্য');
  String get topCustomers => _t('Top Customers', 'শীর্ষ গ্রাহক');
  String get lowStockAlert => _t('Low Stock Alert', 'কম স্টক সতর্কতা');
  String itemsBelowReorderLevel(int n) => _t(
      '$n item${n == 1 ? '' : 's'} below reorder level',
      '$nটি পণ্য রিঅর্ডার লেভেলের নিচে');
  String get due => _t('Due', 'বকেয়া');

  // ── Products ────────────────────────────────────────────────────────────
  String get hideCostPrices => _t('Hide cost prices', 'কস্ট প্রাইস লুকান');
  String get revealCostPrices => _t('Reveal cost prices', 'কস্ট প্রাইস দেখুন');
  String get searchProductsHint =>
      _t('Search name, SKU, brand...', 'নাম, SKU, ব্র্যান্ড খুঁজুন...');
  String get noProductsFound =>
      _t('No products found.', 'কোনো পণ্য পাওয়া যায়নি।');
  String get all => _t('All', 'সব');
  String get more => _t('More', 'আরও');
  String get lowStock => _t('Low stock', 'কম স্টক');
  String get allCategories => _t('All Categories', 'সব ক্যাটাগরি');
  String get searchCategoriesHint =>
      _t('Search categories...', 'ক্যাটাগরি খুঁজুন...');
  String get noCategoriesFound =>
      _t('No categories found.', 'কোনো ক্যাটাগরি পাওয়া যায়নি।');
  String get outOfStock => _t('Out of stock', 'স্টক নেই');
  String stockLeft(int n) => _t('$n left', '$nটি বাকি');
  String inStock(int n) => _t('$n in stock', '$nটি স্টকে');

  // ── Common ──────────────────────────────────────────────────────────────
  String get cancel => _t('Cancel', 'বাতিল');
  String get delete => _t('Delete', 'মুছুন');
  String get add => _t('Add', 'যোগ করুন');
  String get edit => _t('Edit', 'সম্পাদনা');
  String get save => _t('Save', 'সংরক্ষণ');
  String get yes => _t('Yes', 'হ্যাঁ');
  String get no => _t('No', 'না');
  String get retry => _t('Retry', 'আবার চেষ্টা');
  String get pcs => _t('pcs', 'পিস');

  // ── Product detail ──────────────────────────────────────────────────────
  String get productDetail => _t('Product Detail', 'পণ্যের বিবরণ');
  String get editProduct => _t('Edit product', 'পণ্য সম্পাদনা');
  String get failedToLoadProduct =>
      _t('Failed to load product.', 'পণ্য লোড করা যায়নি।');
  String get couldNotOpenCamera =>
      _t('Could not open the camera.', 'ক্যামেরা খোলা যায়নি।');
  String get couldNotOpenGallery =>
      _t('Could not open the photo gallery.', 'ফটো গ্যালারি খোলা যায়নি।');
  String get imageAdded => _t('Image added', 'ছবি যোগ হয়েছে');
  String get takePhoto => _t('Take photo', 'ছবি তুলুন');
  String get chooseFromGallery =>
      _t('Choose from gallery', 'গ্যালারি থেকে বাছুন');
  String get setAsPrimary => _t('Set as primary', 'প্রাইমারি করুন');
  String get deleteImage => _t('Delete image', 'ছবি মুছুন');
  String get deleteImageQuestion => _t('Delete image?', 'ছবি মুছবেন?');
  String get deleteImageBody => _t('This removes the image from the product.',
      'এটি পণ্য থেকে ছবিটি সরিয়ে দেবে।');
  String get costLabel => _t('cost', 'ক্রয়মূল্য');
  String get marginLabel => _t('margin', 'মার্জিন');
  String get warranty => _t('Warranty', 'ওয়ারেন্টি');
  String get totalStock => _t('Total stock', 'মোট স্টক');
  String get reserved => _t('Reserved', 'রিজার্ভড');
  String get reorderAt => _t('Reorder at', 'রিঅর্ডার লেভেল');
  String get overview => _t('Overview', 'সারসংক্ষেপ');
  String get specifications => _t('Specifications', 'স্পেসিফিকেশন');
  String get compatibility => _t('Compatibility', 'সামঞ্জস্য');
  String get stockAndLots => _t('Stock & lots', 'স্টক ও লট');
  String get details => _t('Details', 'বিবরণ');
  String get brand => _t('Brand', 'ব্র্যান্ড');
  String get category => _t('Category', 'ক্যাটাগরি');
  String get partNo => _t('Part no.', 'পার্ট নম্বর');
  String get oemNo => _t('OEM no.', 'OEM নম্বর');
  String get barcode => _t('Barcode', 'বারকোড');
  String get type => _t('Type', 'ধরন');
  String get variants => _t('Variants', 'ভ্যারিয়েন্ট');
  String get notes => _t('Notes', 'নোট');
  String get noSpecificationsYet =>
      _t('No specifications yet.', 'এখনও কোনো স্পেসিফিকেশন নেই।');
  String get technicalSpecification =>
      _t('Technical specification', 'টেকনিক্যাল স্পেসিফিকেশন');
  String get compatibleVehicles =>
      _t('Compatible vehicles', 'সামঞ্জস্যপূর্ণ গাড়ি');
  String get noCompatibleVehiclesYet => _t(
      'No compatible vehicles yet.', 'এখনও কোনো সামঞ্জস্যপূর্ণ গাড়ি নেই।');
  String get notCompatible => _t('Not compatible', 'সামঞ্জস্যপূর্ণ নয়');
  String get stockByWarehouseLots =>
      _t('Stock by warehouse · lots', 'গুদাম অনুযায়ী স্টক · লট');
  String get warehouse => _t('Warehouse', 'গুদাম');
  String get recvShort => _t('Recv', 'প্রাপ্তি');
  String get expShort => _t('exp', 'মেয়াদ');
  String get storageLocations => _t('Storage locations', 'সংরক্ষণ স্থান');
  String get primary => _t('Primary', 'প্রাইমারি');
  String sectionShelf(String section, String shelf) =>
      _t('Section $section · Shelf $shelf', 'সেকশন $section · শেলফ $shelf');
  String get noStockLotsYet => _t('No stock lots for this product yet.',
      'এই পণ্যের এখনও কোনো স্টক লট নেই।');
  String get addToCart => _t('Add to cart', 'কার্টে যোগ করুন');
  String isOutOfStock(String name) =>
      _t('$name is out of stock', '$name স্টকে নেই');
  String addedToSale(String name) =>
      _t('$name added to sale', '$name বিক্রয়ে যোগ হয়েছে');
  String get goToSale => _t('Go to Sale', 'বিক্রয়ে যান');

  // ── Quick sale / cart ───────────────────────────────────────────────────
  String itemsCount(int n) =>
      _t('$n item${n == 1 ? '' : 's'}', '$nটি আইটেম');
  String invDraftItems(int n) =>
      _t('INV-draft · ${itemsCount(n)}', 'INV-খসড়া · ${itemsCount(n)}');
  String get heldSales => _t('Held sales', 'হোল্ড করা বিক্রয়');
  String get holdSale => _t('Hold sale', 'বিক্রয় হোল্ড করুন');
  String get saleHeld => _t('Sale held', 'বিক্রয় হোল্ড হয়েছে');
  String get autoHeld => _t('Auto-held', 'স্বয়ংক্রিয় হোল্ড');
  String onlyNInStock(int n, String name) =>
      _t('Only $n $name in stock', 'স্টকে মাত্র $nটি $name আছে');
  String get addMoreItems => _t('Add more items', 'আরও আইটেম যোগ করুন');
  String subtotalItems(int n) =>
      _t('Subtotal · $n items', 'সাবটোটাল · $nটি আইটেম');
  String get discount => _t('Discount', 'ডিসকাউন্ট');
  String get total => _t('Total', 'মোট');
  String get torch => _t('Torch', 'টর্চ');
  String get pointCameraAtBarcode =>
      _t('Point camera at a barcode', 'বারকোডের দিকে ক্যামেরা ধরুন');
  String get cameraAccessNeeded =>
      _t('Camera access needed', 'ক্যামেরার অনুমতি প্রয়োজন');
  String get cameraError => _t('Camera error', 'ক্যামেরা ত্রুটি');
  String get allowCameraAccess =>
      _t('Allow camera access in Settings, then tap Try again.',
          'সেটিংসে ক্যামেরার অনুমতি দিন, তারপর "আবার চেষ্টা" চাপুন।');
  String get couldNotStartCamera =>
      _t('Could not start the camera.', 'ক্যামেরা চালু করা যায়নি।');
  String get tryAgain => _t('Try again', 'আবার চেষ্টা');
  String get cartIsEmpty => _t('Cart is empty', 'কার্ট খালি');
  String get scanOrAddFromProducts =>
      _t('Scan a barcode or add items from Products',
          'বারকোড স্ক্যান করুন বা পণ্য থেকে আইটেম যোগ করুন');
  String get openTillToSell => _t('Open a till session to start selling',
      'বিক্রি শুরু করতে টিল সেশন খুলুন');
  String get tillRequiredBody => _t(
      'Your role requires an open till session before taking sales. '
          'Count the cash drawer and open one to continue.',
      'বিক্রয় নিতে হলে আপনার রোলে একটি খোলা টিল সেশন প্রয়োজন। '
          'ক্যাশ ড্রয়ার গুনে একটি সেশন খুলে এগিয়ে যান।');
  String get openTillSession => _t('Open Till Session', 'টিল সেশন খুলুন');
  String completeSaleWith(String amount) =>
      _t('✓  Complete Sale · $amount', '✓  বিক্রয় সম্পন্ন করুন · $amount');
  String get print80mm => _t('Print · 80mm', 'প্রিন্ট · ৮০মিমি');
  String get saleComplete => _t('Sale Complete!', 'বিক্রয় সম্পন্ন!');
  String get invoice => _t('Invoice', 'ইনভয়েস');
  String get paid => _t('Paid', 'পরিশোধিত');
  String get labelOptional => _t('Label (optional)', 'লেবেল (ঐচ্ছিক)');
  String get holdLabelHint =>
      _t('e.g. customer name or table', 'যেমন: গ্রাহকের নাম');
  String get hold => _t('Hold', 'হোল্ড');
  String get noHeldSales =>
      _t('No held sales.', 'কোনো হোল্ড করা বিক্রয় নেই।');
  String get resume => _t('Resume', 'চালু করুন');

  // ── Checkout / charge ───────────────────────────────────────────────────
  String get checkout => _t('Checkout', 'চেকআউট');
  String get nothingToCharge =>
      _t('Nothing to charge.', 'চার্জ করার কিছু নেই।');
  String get walkInMustPayFull => _t(
      'Select a customer to leave a balance — Walk-in must pay in full.',
      'বাকি রাখতে একজন গ্রাহক নির্বাচন করুন — ওয়াক-ইনকে সম্পূর্ণ পরিশোধ করতে হবে।');
  String get walkInNoBalance => _t(
      'Walk-in customers can\'t carry a balance — select a registered customer.',
      'ওয়াক-ইন গ্রাহক বাকি রাখতে পারেন না — নিবন্ধিত গ্রাহক নির্বাচন করুন।');
  String addReferenceFor(String method) =>
      _t('Add a reference for the $method payment.',
          '$method পেমেন্টের জন্য একটি রেফারেন্স দিন।');
  String confirmPaid(String amount) =>
      _t('Confirm · $amount paid', 'নিশ্চিত করুন · $amount পরিশোধ');
  String get confirmSale => _t('Confirm Sale', 'বিক্রয় নিশ্চিত করুন');
  String get cartTotal => _t('Cart Total', 'কার্ট মোট');
  String get grandTotalLabel => _t('Grand Total', 'সর্বমোট');
  String get customer => _t('Customer', 'গ্রাহক');
  String get walkInSelectCustomer =>
      _t('Walk-in / Select customer', 'ওয়াক-ইন / গ্রাহক নির্বাচন');
  String get walkInNoAccount =>
      _t('Walk-in (no account)', 'ওয়াক-ইন (অ্যাকাউন্ট নেই)');
  String get outstandingDue => _t('Outstanding Due', 'বকেয়া পাওনা');
  String get vehicle => _t('Vehicle', 'গাড়ি');
  String get noVehiclesOnFile => _t('No vehicles on file for this customer.',
      'এই গ্রাহকের কোনো গাড়ি নিবন্ধিত নেই।');
  String get noVehicle => _t('No vehicle', 'গাড়ি নেই');
  String get advanceCredit => _t('Advance credit', 'অগ্রিম ক্রেডিট');
  String availableAmount(String amount) =>
      _t('Available $amount', 'উপলব্ধ $amount');
  String get appliedToThisSale =>
      _t('Applied to this sale', 'এই বিক্রয়ে প্রযুক্ত');
  String get payment => _t('Payment', 'পেমেন্ট');
  String paymentMethodName(String method) => switch (method) {
        'Cash' => _t('Cash', 'নগদ'),
        'Card' => _t('Card', 'কার্ড'),
        'bKash' => _t('bKash', 'বিকাশ'),
        'Bank' => _t('Bank', 'ব্যাংক'),
        'Cheque' => _t('Cheque', 'চেক'),
        _ => method,
      };
  String get amountPaidNow => _t('Amount paid now', 'এখন পরিশোধিত পরিমাণ');
  String get full => _t('Full', 'পুরো');
  String referenceFor(String method) =>
      _t('$method reference', '$method রেফারেন্স');
  String get txnRefHint =>
      _t('Txn / cheque / card no.', 'লেনদেন / চেক / কার্ড নম্বর');
  String get changeLabel => _t('Change', 'ফেরত');
  String remainingAddedToBalance(String amount) =>
      _t('Remaining $amount added to the customer\'s balance.',
          'বাকি $amount গ্রাহকের ব্যালেন্সে যোগ হয়েছে।');
  String get paidInFull => _t('Paid in full', 'সম্পূর্ণ পরিশোধিত');

  // ── Sales list ──────────────────────────────────────────────────────────
  String get searchInvoiceCustomerHint =>
      _t('Search invoice, customer...', 'ইনভয়েস, গ্রাহক খুঁজুন...');
  String get allStatuses => _t('All statuses', 'সব স্ট্যাটাস');
  String get statusPaid => _t('Paid', 'পরিশোধিত');
  String get statusPartial => _t('Partial', 'আংশিক');
  String get statusCancelled => _t('Cancelled', 'বাতিল হয়েছে');
  String get returns => _t('Returns', 'রিটার্ন');
  String get returnLabel => _t('Return', 'রিটার্ন');
  String get allTime => _t('All time', 'সব সময়');
  String get today => _t('Today', 'আজ');
  String get yesterday => _t('Yesterday', 'গতকাল');
  String get last7Days => _t('Last 7 days', 'গত ৭ দিন');
  String get thisMonth => _t('This month', 'এই মাস');
  String get lastMonth => _t('Last month', 'গত মাস');
  String get customRange => _t('Custom range…', 'কাস্টম রেঞ্জ…');
  String get filterByDateRange =>
      _t('Filter by date range', 'তারিখ অনুযায়ী ফিল্টার');
  String get noSalesFound =>
      _t('No sales found.', 'কোনো বিক্রয় পাওয়া যায়নি।');
  String get noReturnsFound =>
      _t('No returns found.', 'কোনো রিটার্ন পাওয়া যায়নি।');
  String fromOrder(String number) => _t('from $number', '$number থেকে');

  // ── Sale return ─────────────────────────────────────────────────────────
  String get saleReturn => _t('Sale Return', 'বিক্রয় রিটার্ন');
  String returnReasonName(String reason) => switch (reason) {
        'Wrong part' => _t('Wrong part', 'ভুল পার্টস'),
        'Defective' => _t('Defective', 'ত্রুটিপূর্ণ'),
        'Customer changed mind' =>
          _t('Customer changed mind', 'গ্রাহকের মত পরিবর্তন'),
        'Damaged in transit' =>
          _t('Damaged in transit', 'পরিবহনে ক্ষতিগ্রস্ত'),
        'Other' => _t('Other', 'অন্যান্য'),
        _ => reason,
      };
  String refundTypeName(String type) => switch (type) {
        'Cash refund' => _t('Cash refund', 'নগদ ফেরত'),
        'Store credit' => _t('Store credit', 'স্টোর ক্রেডিট'),
        _ => type,
      };
  String get noInvoiceLoaded =>
      _t('No invoice loaded.', 'কোনো ইনভয়েস লোড হয়নি।');
  String get selectAtLeastOneReturn => _t('Select at least one item to return.',
      'রিটার্নের জন্য অন্তত একটি আইটেম নির্বাচন করুন।');
  String returnSubmitted(String number, String amount) =>
      _t('$number submitted · $amount', '$number জমা হয়েছে · $amount');
  String get failedToSubmitReturn =>
      _t('Failed to submit return. Please try again.',
          'রিটার্ন জমা দেওয়া যায়নি। আবার চেষ্টা করুন।');
  String get openInvoiceFirst =>
      _t('Open an invoice first', 'আগে একটি ইনভয়েস খুলুন');
  String get returnHowTo => _t(
      'Go to Customers → select a customer → Invoices tab → tap an invoice → "Initiate return".',
      'গ্রাহক → একজন গ্রাহক নির্বাচন → ইনভয়েস ট্যাব → একটি ইনভয়েসে চাপুন → "রিটার্ন শুরু করুন"।');
  String get goBack => _t('Go back', 'ফিরে যান');
  String get failedToLoadInvoice =>
      _t('Failed to load invoice.', 'ইনভয়েস লোড করা যায়নি।');
  String get selectItemsToReturn =>
      _t('Select items to return', 'রিটার্নের আইটেম নির্বাচন করুন');
  String get noItemsOnInvoice =>
      _t('No items found on this invoice.', 'এই ইনভয়েসে কোনো আইটেম নেই।');
  String get reasonForReturn => _t('Reason for return', 'রিটার্নের কারণ');
  String get refundMethod => _t('Refund method', 'ফেরতের মাধ্যম');
  String get itemsSelected => _t('Items selected', 'নির্বাচিত আইটেম');
  String get refundTotal => _t('Refund total', 'মোট ফেরত');
  String get confirmReturn =>
      _t('↩  Confirm return', '↩  রিটার্ন নিশ্চিত করুন');

  // ── Customers ───────────────────────────────────────────────────────────
  String get searchCustomerHint =>
      _t('Search name, phone or code...', 'নাম, ফোন বা কোড খুঁজুন...');
  String get withDue => _t('With due', 'বকেয়া আছে');
  String get retail => _t('Retail', 'খুচরা');
  String get wholesale => _t('Wholesale', 'পাইকারি');
  String get corporate => _t('Corporate', 'কর্পোরেট');
  String get distributor => _t('Distributor', 'ডিস্ট্রিবিউটর');
  String get noCustomersFound =>
      _t('No customers found.', 'কোনো গ্রাহক পাওয়া যায়নি।');
  String get editCustomer => _t('Edit customer', 'গ্রাহক সম্পাদনা');
  String get failedToLoadCustomer =>
      _t('Failed to load customer.', 'গ্রাহক লোড করা যায়নি।');
  String sinceDate(String date) => _t('since $date', '$date থেকে');
  String get advance => _t('Advance', 'অগ্রিম');
  String get invoices => _t('Invoices', 'ইনভয়েস');
  String get payments => _t('Payments', 'পেমেন্ট');
  String get receivePayment => _t('Receive payment', 'পেমেন্ট গ্রহণ');
  String get sendReminder => _t('Send reminder', 'রিমাইন্ডার পাঠান');
  String get statement => _t('Statement', 'স্টেটমেন্ট');
  String get failedToLoadInvoices =>
      _t('Failed to load invoices.', 'ইনভয়েস লোড করা যায়নি।');
  String get noInvoicesYet =>
      _t('No invoices yet.', 'এখনও কোনো ইনভয়েস নেই।');
  String get failedToLoadPayments =>
      _t('Failed to load payments.', 'পেমেন্ট লোড করা যায়নি।');
  String get noPaymentsYet =>
      _t('No payments yet.', 'এখনও কোনো পেমেন্ট নেই।');
  String get failedToLoadReturns =>
      _t('Failed to load returns.', 'রিটার্ন লোড করা যায়নি।');
  String get noReturnsRecorded =>
      _t('No returns recorded.', 'কোনো রিটার্ন নেই।');
  String get initiateReturn => _t('Initiate return', 'রিটার্ন শুরু করুন');
  String get allRecordsLoaded =>
      _t('All records loaded', 'সব রেকর্ড লোড হয়েছে');
  String get netOwed => _t('Net owed', 'নিট পাওনা');
  String get netCredit => _t('Net credit', 'নিট ক্রেডিট');
  String get sendPaymentReminder =>
      _t('Send payment reminder', 'পেমেন্ট রিমাইন্ডার পাঠান');
  String get dueLower => _t('due', 'বকেয়া');

  // ── Receive payment ─────────────────────────────────────────────────────
  String get customerHasBalance =>
      _t('Customer has a balance', 'গ্রাহকের বকেয়া আছে');
  String owesApplyToInvoices(String name, String amount) => _t(
      '$name owes $amount. Apply this payment to their invoices instead of '
          'banking it as advance credit?',
      '$name-এর বকেয়া $amount। অগ্রিম ক্রেডিট হিসেবে না রেখে এই পেমেন্ট কি '
          'ইনভয়েসে প্রয়োগ করবেন?');
  String get payInvoice => _t('Pay invoice', 'ইনভয়েস পরিশোধ');
  String get bankAsAdvance => _t('Bank as advance', 'অগ্রিম হিসেবে রাখুন');
  String get advanceCreditRecorded =>
      _t('Advance credit recorded', 'অগ্রিম ক্রেডিট রেকর্ড হয়েছে');
  String get paymentRecorded => _t(
      'Payment recorded successfully', 'পেমেন্ট সফলভাবে রেকর্ড হয়েছে');
  String get failedToLoad => _t('Failed to load.', 'লোড করা যায়নি।');
  String totalDue(String amount) =>
      _t('Total due $amount', 'মোট বকেয়া $amount');
  String advanceCreditAmount(String amount) =>
      _t('Advance credit $amount', 'অগ্রিম ক্রেডিট $amount');
  String netOwedAmount(String amount) =>
      _t('Net owed $amount', 'নিট পাওনা $amount');
  String netCreditAmount(String amount) =>
      _t('Net credit $amount', 'নিট ক্রেডিট $amount');
  String get againstInvoice => _t('Against invoice', 'ইনভয়েসের বিপরীতে');
  String get amountReceived => _t('Amount received', 'প্রাপ্ত পরিমাণ');
  String get enterAnAmount => _t('Enter an amount', 'পরিমাণ লিখুন');
  String get enterValidAmount =>
      _t('Enter a valid amount', 'সঠিক পরিমাণ লিখুন');
  String fullWithAmount(String amount) =>
      _t('Full · $amount', 'পুরো · $amount');
  String get paymentMethod => _t('Payment method', 'পেমেন্ট মাধ্যম');
  String get applyToInvoices =>
      _t('Apply to invoices', 'ইনভয়েসে প্রয়োগ করুন');
  String get noOpenInvoices =>
      _t('No open invoices', 'কোনো খোলা ইনভয়েস নেই');
  String get selectAnInvoice =>
      _t('Select an invoice', 'একটি ইনভয়েস নির্বাচন করুন');
  String get advanceCreditNote => _t(
      'Recorded as advance credit on the customer\'s account — apply it to '
          'invoices later.',
      'গ্রাহকের অ্যাকাউন্টে অগ্রিম ক্রেডিট হিসেবে রেকর্ড হবে — পরে ইনভয়েসে '
          'প্রয়োগ করুন।');
  String get notesOptional => _t('Notes (optional)', 'নোট (ঐচ্ছিক)');
  String get confirmAdvance =>
      _t('Confirm advance', 'অগ্রিম নিশ্চিত করুন');
  String get confirmPayment =>
      _t('Confirm payment', 'পেমেন্ট নিশ্চিত করুন');

  // ── Invoice detail ──────────────────────────────────────────────────────
  String get invoiceDate => _t('Invoice date', 'ইনভয়েসের তারিখ');
  String get dueDate => _t('Due date', 'শেষ তারিখ');
  String get items => _t('Items', 'আইটেম');
  String get noItemsFound =>
      _t('No items found.', 'কোনো আইটেম পাওয়া যায়নি।');
  String get amountPaid => _t('Amount Paid', 'পরিশোধিত পরিমাণ');
  String get outstanding => _t('Outstanding', 'বকেয়া');

  // ── Statuses / history lists ────────────────────────────────────────────
  /// Localized label for a backend status code such as "PARTIALLY_PAID".
  String statusName(String code) => switch (code.toUpperCase()) {
        'DRAFT' => _t('Draft', 'খসড়া'),
        'ISSUED' => _t('Issued', 'ইস্যুকৃত'),
        'PARTIALLY_PAID' => _t('Partially paid', 'আংশিক পরিশোধিত'),
        'PAID' => _t('Paid', 'পরিশোধিত'),
        'OVERDUE' => _t('Overdue', 'মেয়াদোত্তীর্ণ'),
        'CANCELLED' => _t('Cancelled', 'বাতিল'),
        'COMPLETED' => _t('Completed', 'সম্পন্ন'),
        'PENDING' => _t('Pending', 'অপেক্ষমাণ'),
        'FAILED' => _t('Failed', 'ব্যর্থ'),
        'REFUNDED' => _t('Refunded', 'ফেরত দেওয়া'),
        _ => _humanize(code),
      };

  static String _humanize(String s) {
    if (s.isEmpty) return s;
    final lower = s.replaceAll('_', ' ').toLowerCase();
    return lower[0].toUpperCase() + lower.substring(1);
  }

  String get searchInvoiceNumber =>
      _t('Search invoice number', 'ইনভয়েস নম্বর খুঁজুন');
  String get filterByStatus =>
      _t('Filter by status', 'স্ট্যাটাস অনুযায়ী ফিল্টার');
  String get status => _t('Status', 'স্ট্যাটাস');
  String invoicesCount(int n) => _t('$n invoice(s)', '$nটি ইনভয়েস');
  String get noMatchingInvoices =>
      _t('No matching invoices.', 'মিল থাকা কোনো ইনভয়েস নেই।');
  String get couldNotLoadItems =>
      _t('Could not load items.', 'আইটেম লোড করা যায়নি।');
  String get paymentHistory => _t('Payment history', 'পেমেন্ট ইতিহাস');
  String paymentsCount(int n) => _t('$n payment(s)', '$nটি পেমেন্ট');
  String get noPaymentsRecorded =>
      _t('No payments recorded.', 'কোনো পেমেন্ট রেকর্ড নেই।');
  String noStatusPayments(String label) =>
      _t('No $label payments.', 'কোনো $label পেমেন্ট নেই।');

  // ── Statement ───────────────────────────────────────────────────────────
  String get last3Months => _t('Last 3 Months', 'গত ৩ মাস');
  String get thisYear => _t('This Year', 'এই বছর');
  String get noTransactionsInPeriod =>
      _t('No transactions in this period.', 'এই সময়ে কোনো লেনদেন নেই।');
  String get print => _t('Print', 'প্রিন্ট');
  String get generatePdfShare =>
      _t('Generate PDF & share', 'PDF তৈরি করে শেয়ার করুন');
  String get accountStatement =>
      _t('Account Statement', 'অ্যাকাউন্ট স্টেটমেন্ট');
  String purchasedPaidSummary(
          String name, String purchased, String paid) =>
      _t('$name · Purchased $purchased · Paid $paid',
          '$name · ক্রয় $purchased · পরিশোধ $paid');

  // ── Add / edit customer ─────────────────────────────────────────────────
  String get addCustomer => _t('Add customer', 'গ্রাহক যোগ করুন');
  String get vehicleNeedsMakeModel => _t(
      'Each vehicle needs a make and model (or clear the row).',
      'প্রতিটি গাড়ির নির্মাতা ও মডেল লাগবে (নয়তো সারিটি খালি করুন)।');
  String get customerUpdated =>
      _t('Customer updated', 'গ্রাহক আপডেট হয়েছে');
  String get customerAdded => _t('Customer added', 'গ্রাহক যোগ হয়েছে');
  String get firstNameRequired => _t('First name *', 'নামের প্রথম অংশ *');
  String get lastNameRequired => _t('Last name *', 'নামের শেষ অংশ *');
  String get required => _t('Required', 'আবশ্যক');
  String get phoneUniqueLabel =>
      _t('Phone * (must be unique)', 'ফোন * (অনন্য হতে হবে)');
  String get phoneRequired => _t('Phone is required', 'ফোন নম্বর আবশ্যক');
  String get customerType => _t('Customer type', 'গ্রাহকের ধরন');
  String customerTypeName(String code) => switch (code.toUpperCase()) {
        'RETAIL' => retail,
        'WHOLESALE' => wholesale,
        'CORPORATE' => corporate,
        'DISTRIBUTOR' => distributor,
        _ => _humanize(code),
      };
  String get company => _t('Company', 'কোম্পানি');
  String get email => _t('Email', 'ইমেইল');
  String get city => _t('City', 'শহর');
  String get vehicles => _t('Vehicles', 'গাড়িসমূহ');
  String get optionalLower => _t('optional', 'ঐচ্ছিক');
  String get addVehicle => _t('Add vehicle', 'গাড়ি যোগ করুন');
  String get saveChanges => _t('Save changes', 'পরিবর্তন সংরক্ষণ');
  String get saveCustomer => _t('Save customer', 'গ্রাহক সংরক্ষণ');
  String vehicleN(int n) => _t('Vehicle $n', 'গাড়ি $n');
  String get remove => _t('Remove', 'সরান');
  String get make => _t('Make', 'নির্মাতা');
  String get model => _t('Model', 'মডেল');
  String get year => _t('Year', 'বছর');
  String get regNo => _t('Reg. no.', 'রেজি. নম্বর');

  // ── Suppliers ───────────────────────────────────────────────────────────
  String weOweAmount(String amount) =>
      _t('We owe $amount', 'আমরা দেনা $amount');
  String get noOutstandingPayable =>
      _t('No outstanding payable', 'কোনো বকেয়া দেনা নেই');
  String get paySupplier =>
      _t('Pay supplier', 'সরবরাহকারীকে পরিশোধ');
  String get searchSupplierHint =>
      _t('Search supplier...', 'সরবরাহকারী খুঁজুন...');
  String get weOwe => _t('We owe', 'আমরা দেনা');
  String get noSuppliersFound =>
      _t('No suppliers found.', 'কোনো সরবরাহকারী পাওয়া যায়নি।');
  String get weOweLower => _t('we owe', 'দেনা');
  String get clearLabel => _t('clear', 'পরিশোধিত');
  String get noActiveProvider => _t(
      'No active payment provider is configured. Add one on the web app first.',
      'কোনো সক্রিয় পেমেন্ট প্রোভাইডার নেই। আগে ওয়েব অ্যাপে একটি যোগ করুন।');
  String paymentOfRecorded(String amount) =>
      _t('Payment of $amount recorded', '$amount পেমেন্ট রেকর্ড হয়েছে');
  String get failedToLoadSupplier =>
      _t('Failed to load supplier.', 'সরবরাহকারী লোড করা যায়নি।');
  String get amountToPay => _t('Amount to pay', 'পরিশোধের পরিমাণ');
  String get method => _t('Method', 'মাধ্যম');
  String get reference => _t('Reference', 'রেফারেন্স');
  String get bankChequeTrxHint => _t('Bank / cheque / TRX no. (optional)',
      'ব্যাংক / চেক / TRX নম্বর (ঐচ্ছিক)');
  String get againstPurchaseBills =>
      _t('Against purchase bills', 'ক্রয় বিলের বিপরীতে');
  String get couldNotLoadBills =>
      _t('Could not load purchase bills', 'ক্রয় বিল লোড করা যায়নি');
  String get noOpenBillsAdvance => _t(
      'No open purchase bills — the payment will be saved as an advance.',
      'কোনো খোলা ক্রয় বিল নেই — পেমেন্টটি অগ্রিম হিসেবে সংরক্ষিত হবে।');
  String get remainingPayable =>
      _t('Remaining payable', 'অবশিষ্ট দেনা');
  String get noteOptional => _t('Note (optional)', 'নোট (ঐচ্ছিক)');
  String confirmPaymentWith(String amount) =>
      _t('Confirm payment · $amount', 'পেমেন্ট নিশ্চিত করুন · $amount');

  // ── Supplier statement ──────────────────────────────────────────────────
  String get supplierStatement =>
      _t('Supplier Statement', 'সরবরাহকারী স্টেটমেন্ট');
  String supplierStatementFor(String name) =>
      _t('Supplier statement — $name', 'সরবরাহকারী স্টেটমেন্ট — $name');
  String get periodLabel => _t('Period', 'সময়কাল');
  String get payableLower => _t('payable', 'দেনা');
  String get purchaseLower => _t('purchase', 'ক্রয়');
  String get paidLowerWord => _t('paid', 'পরিশোধ');
  String get balanceLower => _t('balance', 'ব্যালেন্স');
  String get entryHeader => _t('ENTRY', 'এন্ট্রি');
  String get purchaseHeader => _t('PURCHASE', 'ক্রয়');
  String get paidHeader => _t('PAID', 'পরিশোধ');
  String get balanceHeader => _t('BALANCE', 'ব্যালেন্স');
  String ledgerTypeName(String type) => switch (type.toUpperCase()) {
        'PURCHASE' => _t('purchase', 'ক্রয়'),
        'PAYMENT' => _t('payment', 'পেমেন্ট'),
        'REFUND' => _t('refund', 'ফেরত'),
        'ADVANCE' => _t('advance', 'অগ্রিম'),
        'CANCELLATION' => _t('cancelled', 'বাতিল'),
        _ => type.toLowerCase(),
      };
  String get shareStatement =>
      _t('Share statement', 'স্টেটমেন্ট শেয়ার করুন');

  // ── Cash book ───────────────────────────────────────────────────────────
  String get failedToLoadCashBook =>
      _t('Failed to load cash book.', 'ক্যাশ বুক লোড করা যায়নি।');
  String get previousDay => _t('Previous day', 'আগের দিন');
  String get nextDay => _t('Next day', 'পরের দিন');
  String get byPaymentMethod =>
      _t('By payment method', 'পেমেন্ট মাধ্যম অনুযায়ী');
  String transactionsCount(int n) =>
      _t('Transactions ($n)', 'লেনদেন ($n)');
  String get noCashMovement => _t(
      'No cash movement on this day.', 'এই দিনে কোনো নগদ লেনদেন নেই।');
  String get closingBalance =>
      _t('Closing balance', 'সমাপনী ব্যালেন্স');
  String openingAmount(String amount) =>
      _t('Opening $amount', 'প্রারম্ভিক $amount');
  String get cashIn => _t('Cash in', 'নগদ জমা');
  String get cashOut => _t('Cash out', 'নগদ খরচ');
  String get net => _t('Net', 'নিট');
  String onCredit(String amount) =>
      _t('+$amount on credit', '+$amount বাকিতে');
  String get balShort => _t('Bal', 'ব্যাল');
  /// Localized label for a payment-method code like "MOBILE_BANKING".
  String cashMethodName(String raw) => switch (raw.toUpperCase()) {
        '' => _t('Other', 'অন্যান্য'),
        'CASH' => _t('Cash', 'নগদ'),
        'CARD' => _t('Card', 'কার্ড'),
        'MOBILE_BANKING' => _t('Mobile banking', 'মোবাইল ব্যাংকিং'),
        'BKASH' => _t('bKash', 'বিকাশ'),
        'BANK_TRANSFER' => _t('Bank transfer', 'ব্যাংক ট্রান্সফার'),
        'CHEQUE' || 'CHECK' => _t('Cheque', 'চেক'),
        _ => _humanize(raw),
      };
  String get addCashEntry =>
      _t('Add cash entry', 'নগদ এন্ট্রি যোগ করুন');
  String cashCategoryName(String code) => switch (code.toUpperCase()) {
        'OWNER_DEPOSIT' => _t('Owner deposit', 'মালিকের জমা'),
        'GENERAL' => _t('Expense', 'খরচ'),
        'UTILITIES' => _t('Utilities', 'ইউটিলিটি'),
        'TRANSPORTATION' => _t('Transport', 'পরিবহন'),
        'OTHER' => _t('Other', 'অন্যান্য'),
        _ => _humanize(code),
      };
  String get pickACategory =>
      _t('Pick a category.', 'একটি ক্যাটাগরি বাছুন।');
  String get addShortDescription =>
      _t('Add a short description.', 'সংক্ষিপ্ত বিবরণ দিন।');
  String get amount => _t('Amount', 'পরিমাণ');
  String get descriptionNote =>
      _t('Description / note', 'বিবরণ / নোট');
  String get saveEntry => _t('Save entry', 'এন্ট্রি সংরক্ষণ');

  // ── Till session ────────────────────────────────────────────────────────
  String get cashDrop => _t('Cash drop', 'ক্যাশ ড্রপ');
  String get failedToLoadTillSession =>
      _t('Failed to load till session.', 'টিল সেশন লোড করা যায়নি।');
  String get openATillSession =>
      _t('Open a till session', 'একটি টিল সেশন খুলুন');
  String get openTillSubtitle => _t(
      'Count the cash drawer and start a shift before taking any cash sales.',
      'নগদ বিক্রয় নেওয়ার আগে ক্যাশ ড্রয়ার গুনে শিফট শুরু করুন।');
  String get terminalLabel => _t('Terminal label', 'টার্মিনাল লেবেল');
  String get enterTerminalLabel =>
      _t('Enter a terminal label.', 'টার্মিনাল লেবেল লিখুন।');
  String get enterValidOpeningFloat =>
      _t('Enter a valid opening float.', 'সঠিক প্রারম্ভিক ফ্লোট লিখুন।');
  String get openingFloat => _t('Opening float', 'প্রারম্ভিক ফ্লোট');
  String suggestedFloatHint(String cashier) => _t(
      "Suggested from this terminal's last closed session (counted by "
          '$cashier) — edit if the count differs.',
      'এই টার্মিনালের সর্বশেষ বন্ধ সেশন থেকে প্রস্তাবিত ($cashier গুনেছেন) — '
          'গণনা ভিন্ন হলে বদলান।');
  String get shiftLabelOptional =>
      _t('Shift label (optional)', 'শিফট লেবেল (ঐচ্ছিক)');
  String suggestedShiftHint(String hours) => _t(
      'From your assigned shift ($hours) — edit if this shift differs.',
      'আপনার নির্ধারিত শিফট থেকে ($hours) — শিফট ভিন্ন হলে বদলান।');
  String get openTill => _t('Open Till', 'টিল খুলুন');
  String cashDropsCount(int n) =>
      _t('Cash drops ($n)', 'ক্যাশ ড্রপ ($n)');
  String get noCashDropsYet =>
      _t('No cash drops recorded yet.', 'এখনও কোনো ক্যাশ ড্রপ নেই।');
  String runningTotal(String amount) =>
      _t('Running total: $amount', 'চলমান মোট: $amount');
  String get closeTill => _t('Close Till', 'টিল বন্ধ করুন');
  String get openBadge => _t('OPEN', 'খোলা');
  String openedAtLine(String day, String time) =>
      _t('Opened $day, $time', 'খোলা হয়েছে $day, $time');
  String get cashier => _t('Cashier', 'ক্যাশিয়ার');
  String get cashDrops => _t('Cash drops', 'ক্যাশ ড্রপ');
  String get recordCashDrop =>
      _t('Record cash drop', 'ক্যাশ ড্রপ রেকর্ড করুন');
  String get cashDropSubtitle => _t(
      'Removes cash from the drawer (e.g. a safe drop) without closing the till.',
      'টিল বন্ধ না করে ড্রয়ার থেকে নগদ সরায় (যেমন সেফ ড্রপ)।');
  String get saveCashDrop =>
      _t('Save cash drop', 'ক্যাশ ড্রপ সংরক্ষণ');
  String get enterCountedAmount => _t('Enter the counted drawer amount.',
      'গণনা করা ড্রয়ারের পরিমাণ লিখুন।');
  String get closeTillQuestion =>
      _t('Close this till session?', 'এই টিল সেশন বন্ধ করবেন?');
  String get closeTillWarning => _t(
      'This freezes cash reconciliation for the shift and cannot be undone. '
          'Make sure the counted amount is accurate before continuing.',
      'এটি শিফটের নগদ হিসাব চূড়ান্ত করবে এবং আর বদলানো যাবে না। এগোনোর আগে '
          'গণনা সঠিক কিনা নিশ্চিত হোন।');
  String closeTillTitled(String terminal) =>
      _t('Close till · $terminal', 'টিল বন্ধ · $terminal');
  String get countDrawerWarning => _t(
      'Count the drawer cash carefully — this cannot be reopened once closed.',
      'ড্রয়ারের নগদ সাবধানে গুনুন — বন্ধ হলে আর খোলা যাবে না।');
  String get countedDrawerAmount =>
      _t('Counted drawer amount', 'গণনা করা ড্রয়ারের পরিমাণ');
  String get reviewAndClose =>
      _t('Review & close', 'পর্যালোচনা ও বন্ধ করুন');
  String get shiftReport => _t('Shift Report', 'শিফট রিপোর্ট');
  String tillClosedTitled(String terminal) =>
      _t('Till closed · $terminal', 'টিল বন্ধ হয়েছে · $terminal');
  String get cashSales => _t('Cash sales', 'নগদ বিক্রয়');
  String get cashRefunds => _t('Cash refunds', 'নগদ ফেরত');
  String get expectedInDrawer =>
      _t('Expected in drawer', 'ড্রয়ারে প্রত্যাশিত');
  String get countedAmount =>
      _t('Counted amount', 'গণনা করা পরিমাণ');
  String get balanced => _t('Balanced', 'মিলেছে');
  String get shortLabel => _t('Short', 'ঘাটতি');
  String get overLabel => _t('Over', 'উদ্বৃত্ত');
  String get shareShiftReportPdf =>
      _t('Share Shift Report PDF', 'শিফট রিপোর্ট PDF শেয়ার করুন');
  String get openNewTill => _t('Open New Till', 'নতুন টিল খুলুন');

  // ── Notifications ───────────────────────────────────────────────────────
  String get markAllRead => _t('Mark all read', 'সব পঠিত করুন');
  String get clearAll => _t('Clear all', 'সব মুছুন');
  String get noNotificationsYet => _t(
      'No notifications yet.\nNew sales will appear here live.',
      'এখনও কোনো নোটিফিকেশন নেই।\nনতুন বিক্রয় এখানে লাইভ দেখা যাবে।');
  String get invoiceNotFoundYet => _t('Invoice not found for this sale yet.',
      'এই বিক্রয়ের ইনভয়েস এখনও পাওয়া যায়নি।');
  String get connecting => _t('Connecting…', 'সংযোগ হচ্ছে…');
  String get offlinePaused => _t('Offline — live notifications paused',
      'অফলাইন — লাইভ নোটিফিকেশন বন্ধ');
  String get walkIn => _t('Walk-in', 'ওয়াক-ইন');
  String newSaleTitled(String number) =>
      _t('New sale · $number', 'নতুন বিক্রয় · $number');

  // ── Stock in ────────────────────────────────────────────────────────────
  String get quickStockAdjust =>
      _t('Quick stock adjust', 'দ্রুত স্টক সমন্বয়');
  String get searchPoHint =>
      _t('Search PO no, supplier...', 'PO নম্বর, সরবরাহকারী খুঁজুন...');
  String get pending => _t('Pending', 'অপেক্ষমাণ');
  String get received => _t('Received', 'গৃহীত');
  String get noStockInOrders => _t(
      'No stock-in orders found.', 'কোনো স্টক-ইন অর্ডার পাওয়া যায়নি।');
  /// Localized pill label for a purchase-order status code.
  String poStatusName(String status) => switch (status) {
        'DELIVERED' => received,
        'DRAFT' => _t('Draft', 'খসড়া'),
        'SUBMITTED' || 'CONFIRMED' => pending,
        'PARTIAL' => statusPartial,
        'CANCELLED' => statusCancelled,
        _ => status,
      };
  String get submittingOrder =>
      _t('Submitting order...', 'অর্ডার জমা হচ্ছে...');
  String get confirmingOrder =>
      _t('Confirming order...', 'অর্ডার নিশ্চিত হচ্ছে...');
  String get postingGrn =>
      _t('Posting goods receipt...', 'পণ্য প্রাপ্তি পোস্ট হচ্ছে...');
  String get verifying => _t('Verifying...', 'যাচাই হচ্ছে...');
  String get acceptingStock =>
      _t('Accepting stock...', 'স্টক গ্রহণ হচ্ছে...');
  String poReceivedStockPosted(String po) =>
      _t('$po received — stock posted.', '$po গৃহীত — স্টক পোস্ট হয়েছে।');
  String get purchaseOrder => _t('Purchase order', 'ক্রয় অর্ডার');
  String get qtyShort => _t('Qty', 'পরিমাণ');
  String nReceived(int n) => _t('$n received', '$nটি গৃহীত');
  String get receiveStock => _t('Receive stock', 'স্টক গ্রহণ করুন');
  String get receiveIntoWarehouse =>
      _t('Receive into warehouse', 'কোন গুদামে গ্রহণ করবেন');
  String get selectASupplier =>
      _t('Select a supplier.', 'একজন সরবরাহকারী নির্বাচন করুন।');
  String get addAtLeastOneItem =>
      _t('Add at least one item.', 'অন্তত একটি আইটেম যোগ করুন।');
  String get selectWarehouseToReceive => _t(
      'Select a warehouse to receive into.',
      'গ্রহণের জন্য একটি গুদাম নির্বাচন করুন।');
  String get everyItemNeedsCost => _t(
      'Every item needs a unit cost to receive stock.',
      'স্টক গ্রহণে প্রতিটি আইটেমের একক খরচ লাগবে।');
  String get creatingOrder =>
      _t('Creating order...', 'অর্ডার তৈরি হচ্ছে...');
  String poSavedAsDraft(String po) =>
      _t('$po saved as draft.', '$po খসড়া হিসেবে সংরক্ষিত।');
  String get supplier => _t('Supplier', 'সরবরাহকারী');
  String get selectSupplier =>
      _t('Select supplier', 'সরবরাহকারী নির্বাচন');
  String get selectWarehouse => _t('Select warehouse', 'গুদাম নির্বাচন');
  String get referenceBillNo =>
      _t('Reference / bill no.', 'রেফারেন্স / বিল নম্বর');
  String get date => _t('Date', 'তারিখ');
  String get addItem => _t('Add item', 'আইটেম যোগ করুন');
  String get saveAsDraft => _t('Save as draft', 'খসড়া সংরক্ষণ');
  String get lotShort => _t('Lot', 'লট');
  String get quantityAtLeast1 =>
      _t('Quantity must be at least 1.', 'পরিমাণ কমপক্ষে ১ হতে হবে।');
  String get quantity => _t('Quantity', 'পরিমাণ');
  String get unitCost => _t('Unit cost', 'একক খরচ');
  String get lotBatchOptional =>
      _t('Lot / batch (optional)', 'লট / ব্যাচ (ঐচ্ছিক)');
  String get expiryOptional =>
      _t('Expiry (optional)', 'মেয়াদ (ঐচ্ছিক)');
  String get done => _t('Done', 'সম্পন্ন');
  String get productNotFoundForBarcode => _t(
      'Product not found for this barcode',
      'এই বারকোডের কোনো পণ্য পাওয়া যায়নি');
  String get failedToLoadProductDetails => _t(
      'Failed to load product details', 'পণ্যের বিবরণ লোড করা যায়নি');
  String get searchPartsHint => _t('Search parts, SKU or brand...',
      'পার্টস, SKU বা ব্র্যান্ড খুঁজুন...');
  String get quickStockIn => _t('Quick Stock In', 'দ্রুত স্টক ইন');
  String get quickStockInSubtitle => _t(
      'Scan a barcode or search for a product to record received stock or '
          'adjust inventory counts.',
      'প্রাপ্ত স্টক রেকর্ড বা ইনভেন্টরি গণনা সমন্বয় করতে বারকোড স্ক্যান করুন '
          'বা পণ্য খুঁজুন।');

  // ── Stock adjustment sheet ──────────────────────────────────────────────
  String adjustReasonName(String code) => switch (code.toUpperCase()) {
        'RETURN' => _t('Customer return', 'গ্রাহক ফেরত'),
        'FOUND' => _t('Found in stock', 'স্টকে পাওয়া গেছে'),
        'SALE' => _t('Sale (manual)', 'বিক্রয় (ম্যানুয়াল)'),
        'INTERNAL_USE' => _t('Internal use', 'অভ্যন্তরীণ ব্যবহার'),
        'DAMAGED' => _t('Damaged goods', 'ক্ষতিগ্রস্ত পণ্য'),
        'EXPIRED' => _t('Expired', 'মেয়াদোত্তীর্ণ'),
        'LOST' => _t('Lost / stolen', 'হারানো / চুরি'),
        'SAMPLE' => _t('Sample / demo', 'নমুনা / ডেমো'),
        'COUNT_CORRECTION' => _t('Count correction', 'গণনা সংশোধন'),
        _ => _humanize(code),
      };
  String get selectAWarehouse =>
      _t('Select a warehouse', 'একটি গুদাম নির্বাচন করুন');
  String get selectDestinationWarehouse =>
      _t('Select a destination warehouse', 'গন্তব্য গুদাম নির্বাচন করুন');
  String get sourceDestinationDiffer =>
      _t('Source and destination must differ', 'উৎস ও গন্তব্য ভিন্ন হতে হবে');
  String get unitsLabel => _t('units', 'ইউনিট');
  String qtyTransferred(String qtyUnits) =>
      _t('$qtyUnits transferred', '$qtyUnits স্থানান্তরিত');
  String recordedAsReceived(String qtyUnits) =>
      _t('$qtyUnits recorded as received', '$qtyUnits প্রাপ্তি রেকর্ড হয়েছে');
  String recordedAsOut(String qtyUnits) =>
      _t('$qtyUnits recorded as out', '$qtyUnits বহির্গমন রেকর্ড হয়েছে');
  String get adjustmentSaved =>
      _t('Adjustment saved', 'সমন্বয় সংরক্ষিত');
  String get recordStockIn =>
      _t('Record Stock In', 'স্টক ইন রেকর্ড করুন');
  String get recordStockOut =>
      _t('Record Stock Out', 'স্টক আউট রেকর্ড করুন');
  String get transferStock => _t('Transfer Stock', 'স্টক স্থানান্তর');
  String get saveAdjustment =>
      _t('Save Adjustment', 'সমন্বয় সংরক্ষণ');
  String get modeIn => _t('In', 'ইন');
  String get modeOut => _t('Out', 'আউট');
  String get modeTransfer => _t('Transfer', 'স্থানান্তর');
  String get modeAdjust => _t('Adjust', 'সমন্বয়');
  String get grnHint => _t(
      'Recording a supplier purchase? Use Stock In so it creates a priced lot.',
      'সরবরাহকারীর ক্রয় রেকর্ড করছেন? মূল্যসহ লট তৈরি করতে স্টক ইন ব্যবহার করুন।');
  String get openStockIn => _t('Open Stock In', 'স্টক ইন খুলুন');
  String get variant => _t('Variant', 'ভ্যারিয়েন্ট');
  String get selectVariant =>
      _t('Select variant', 'ভ্যারিয়েন্ট নির্বাচন');
  String get selectAVariant =>
      _t('Select a variant', 'একটি ভ্যারিয়েন্ট নির্বাচন করুন');
  String get fromWarehouse => _t('From warehouse', 'উৎস গুদাম');
  String get noWarehouseFound => _t(
      'No warehouse found — initialize stock from the admin panel first.',
      'কোনো গুদাম নেই — আগে অ্যাডমিন প্যানেল থেকে স্টক চালু করুন।');
  String availableHere(String qtyUnits) =>
      _t('Available here: $qtyUnits', 'এখানে উপলব্ধ: $qtyUnits');
  String get toWarehouse => _t('To warehouse', 'গন্তব্য গুদাম');
  String get selectDestinationHint =>
      _t('Select destination', 'গন্তব্য নির্বাচন');
  String get selectADestination =>
      _t('Select a destination', 'গন্তব্য নির্বাচন করুন');
  String get qtyReceived => _t('Qty received', 'প্রাপ্ত পরিমাণ');
  String get qtyGoingOut => _t('Qty going out', 'বহির্গমন পরিমাণ');
  String get qtyToTransfer =>
      _t('Qty to transfer', 'স্থানান্তরের পরিমাণ');
  String get countDifference =>
      _t('Count difference', 'গণনার পার্থক্য');
  String get addPlus => _t('+ Add', '+ যোগ');
  String get removeMinus => _t('− Remove', '− বাদ');
  String get enterQuantityGtZero => _t(
      'Enter a quantity greater than zero', 'শূন্যের বেশি পরিমাণ লিখুন');
  String get reasonLabel => _t('Reason', 'কারণ');
  String get selectAReason =>
      _t('Select a reason', 'একটি কারণ নির্বাচন করুন');
  String get stockNotesHint => _t('Supplier invoice no., batch, delivery ref…',
      'সরবরাহকারীর ইনভয়েস নম্বর, ব্যাচ, ডেলিভারি রেফ…');

  // ── Auth / login ───────────────────────────────────────────────────────
  String get brandName => _t('Sujan Motors', 'সুজান মোটরস');
  String get brandSubtitle =>
      _t('Auto Parts POS & Inventory', 'অটো পার্টস POS ও ইনভেন্টরি');
  String get loginFailed =>
      _t('Login failed. Please try again.', 'লগইন ব্যর্থ হয়েছে। আবার চেষ্টা করুন।');
  String get usernameOrPhone =>
      _t('Username or phone', 'ব্যবহারকারী নাম বা ফোন');
  String get enterUsernameHint =>
      _t('Enter your username', 'আপনার ব্যবহারকারী নাম লিখুন');
  String get usernameRequired =>
      _t('Username is required', 'ব্যবহারকারী নাম আবশ্যক');
  String get passwordLabel => _t('Password', 'পাসওয়ার্ড');
  String get forgotPassword =>
      _t('Forgot password?', 'পাসওয়ার্ড ভুলে গেছেন?');
  String get enterPasswordHint =>
      _t('Enter your password', 'আপনার পাসওয়ার্ড লিখুন');
  String get showLabel => _t('Show', 'দেখুন');
  String get hideLabel => _t('Hide', 'লুকান');
  String get passwordRequired =>
      _t('Password is required', 'পাসওয়ার্ড আবশ্যক');
  String get signIn => _t('Sign in', 'সাইন ইন');
  String get usePinInstead =>
      _t('Use PIN instead', 'PIN ব্যবহার করুন');
  String get storeFooter =>
      _t('Store: Main Branch · v2.4', 'দোকান: মেইন ব্রাঞ্চ · v2.4');

  // ── Scanner ───────────────────────────────────────────────────────────
  String get scanningNotSupported =>
      _t('Scanning not supported', 'স্ক্যানিং সমর্থিত নয়');
  String get scanningNotSupportedBody => _t(
      'This device does not support barcode scanning.',
      'এই ডিভাইসে বারকোড স্ক্যানিং সমর্থিত নয়।');
  String cameraErrorBody(String detail) => _t(
      'The camera could not be started. $detail',
      'ক্যামেরা চালু করা যায়নি। $detail');
  String get allowCameraAccessBody => _t(
      'Allow camera access to scan barcodes. If you previously denied it, '
          'enable the Camera permission for this app in your device Settings, '
          'then tap Try again.',
      'বারকোড স্ক্যান করতে ক্যামেরার অনুমতি দিন। আগে অস্বীকার করে থাকলে '
          'আপনার ডিভাইসের সেটিংসে এই অ্যাপের জন্য ক্যামেরা অনুমতি চালু করুন, '
          'তারপর "আবার চেষ্টা" চাপুন।');
  String get switchCamera =>
      _t('Switch camera', 'ক্যামেরা পরিবর্তন');

  // ── Product edit ──────────────────────────────────────────────────────
  String get productUpdated =>
      _t('Product updated', 'পণ্য আপডেট হয়েছে');
  String get nameRequired => _t('Name *', 'নাম *');
  String get nameRequiredValidation =>
      _t('Name is required', 'নাম আবশ্যক');
  String get localName => _t('Local name', 'স্থানীয় নাম');
  String get categoryRequired => _t('Category *', 'ক্যাটাগরি *');
  String get categoryRequiredValidation =>
      _t('Category is required', 'ক্যাটাগরি আবশ্যক');
  String get noBrand => _t('No brand', 'ব্র্যান্ড নেই');
  String get sellingPriceRequired =>
      _t('Selling price *', 'বিক্রয় মূল্য *');
  String get enterValidPrice =>
      _t('Enter a valid price', 'সঠিক মূল্য লিখুন');
  String get minimumStock => _t('Minimum stock', 'সর্বনিম্ন স্টক');
  String get wholeNumber => _t('Whole number', 'পূর্ণ সংখ্যা');
  String get oemNumber => _t('OEM number', 'OEM নম্বর');
  String get descriptionLabel => _t('Description', 'বিবরণ');
  String get activeLabel => _t('Active', 'সক্রিয়');

  // ── Product specs edit ────────────────────────────────────────────────
  String get specificationsSaved =>
      _t('Specifications saved', 'স্পেসিফিকেশন সংরক্ষিত');
  String get addSpecification =>
      _t('Add specification', 'স্পেসিফিকেশন যোগ করুন');
  String get saveSpecifications =>
      _t('Save specifications', 'স্পেসিফিকেশন সংরক্ষণ');
  String get labelLabel => _t('Label', 'লেবেল');
  String get valueLabel => _t('Value', 'মান');

  // ── Product compatibility edit ────────────────────────────────────────
  String get noVehiclesFound =>
      _t('No vehicles found.', 'কোনো গাড়ি পাওয়া যায়নি।');
  String get searchVehicleHint =>
      _t('Search make, model, year...', 'নির্মাতা, মডেল, বছর খুঁজুন...');
  String get noCompatibleVehiclesMessage => _t(
      'No compatible vehicles yet.\nTap "Add vehicle".',
      'এখনও কোনো সামঞ্জস্যপূর্ণ গাড়ি নেই।\n"গাড়ি যোগ করুন" চাপুন।');
  String get failedToLoadLabel =>
      _t('Failed to load.', 'লোড করা যায়নি।');

  // ── Shared widgets ────────────────────────────────────────────────────
  String get staffLabel => _t('Staff', 'স্টাফ');
  String get nothingToShow =>
      _t('Nothing to show.', 'দেখানোর মতো কিছু নেই।');
  String get justNow => _t('just now', 'এইমাত্র');
  String minutesAgo(int n) =>
      _t('${n}m ago', '$nমি আগে');
  String hoursAgo(int n) =>
      _t('${n}h ago', '$nঘ আগে');
  String daysAgo(int n) =>
      _t('${n}d ago', '$nদি আগে');
}

class _SDelegate extends LocalizationsDelegate<S> {
  const _SDelegate();

  @override
  bool isSupported(Locale locale) =>
      S.supportedLocales.any((l) => l.languageCode == locale.languageCode);

  @override
  Future<S> load(Locale locale) => SynchronousFuture(S(locale));

  @override
  bool shouldReload(_SDelegate old) => false;
}
