/**
 * Source-agnostic data the label dialog needs to render a barcode/label.
 *
 * The same dialog is opened from several places — the catalog (a Part), a Goods
 * Receipt line, and a Stock Lot — each with its own shape. They are normalised
 * to {@link LabelData} via the mappers below so the dialog never depends on a
 * specific response type.
 */

import { PartResponse } from '../../services/part.service';
import { StockLotResponse } from '../../services/stock-lot.service';
import { GoodsReceiptLineResponse } from '../../../procurement/services/goods-receipt.service';

export interface LabelData {
    /** Product display name (variant-aware where available). */
    name: string;
    /** Part id — lets the dialog fetch brand/category/MRP/compatibility on demand. */
    partId?: string;
    brand?: string;
    category?: string;
    /** Local-language name (e.g. Bengali) shown alongside the catalog name. */
    localName?: string | null;
    /** Compact vehicle-compatibility summary, e.g. "Honda Civic 2020, Toyota Corolla +3". */
    compatibility?: string | null;
    /** Identifier printed/encoded — variant SKU when the row is a variant. */
    sku: string;
    partNumber?: string;
    oemNumber?: string;
    /** Stored manufacturer barcode (UPC/EAN), variant-resolved. Empty when none. */
    barcode?: string | null;
    unit?: string;
    /** Selling price / MRP for the price footer. */
    price?: number | null;
    // ── Lot / batch context (only present from GRN / Stock Lot) ──
    batchNumber?: string | null;
    /** ISO date string. */
    mfgDate?: string | null;
    /** ISO date string. */
    expiryDate?: string | null;
    // ── Variant awareness ──
    isVariant?: boolean;
    variantName?: string | null;
    /** Pre-fills the print quantity (auto-qty); defaults to 1 when omitted. */
    defaultQuantity?: number;
}

/** Catalog row → label. Resolves variant SKU/barcode when the row is a variant. */
export function labelFromPart(part: PartResponse): LabelData {
    const isVariant = !!part.isVariant;
    return {
        name: part.displayName || part.name,
        partId: part.id,
        brand: part.brandName ?? undefined,
        category: part.categoryName,
        sku: (isVariant ? part.variantSKU : part.sku) || part.sku,
        partNumber: part.partNumber,
        oemNumber: part.oemNumber ?? undefined,
        localName: part.localName ?? null,
        compatibility: part.vehicleFit ?? null,
        barcode: (isVariant ? part.variantBarcode : part.barcode) ?? part.barcode ?? null,
        unit: part.unitCode || part.unitName || undefined,
        price: part.sellingPrice ?? null,
        isVariant,
        variantName: part.variantName ?? null,
        defaultQuantity: 1,
    };
}

/**
 * Goods Receipt line → label. Quantity defaults to the accepted ("good")
 * quantity so receiving staff print one label per unit put into stock.
 * `mfgDate` falls back to the receipt date when no explicit date exists.
 */
export function labelFromGrnLine(line: GoodsReceiptLineResponse, receivedDate?: string): LabelData {
    const isVariant = !!line.variantId;
    const accepted = line.acceptedQuantity ?? line.receivedQuantity ?? 1;
    return {
        name: line.displayName || line.partName,
        partId: line.partId,
        sku: line.variantSKU || line.partSKU,
        barcode: line.variantBarcode ?? line.barcode ?? null,
        // Price is the catalog MRP, not a per-lot value; receiving labels omit it.
        price: null,
        batchNumber: line.batchNumber ?? null,
        mfgDate: receivedDate ?? null,
        expiryDate: line.expiryDate ?? null,
        isVariant,
        variantName: line.variantName ?? null,
        defaultQuantity: accepted > 0 ? accepted : 1,
    };
}

/**
 * Stock Lot → label (reprint path). Carries the lot's batch + dates so a
 * reprinted label matches what was first printed at receipt. Quantity is left
 * at 1 — reprints are on-demand, not one-per-unit.
 */
export function labelFromStockLot(lot: StockLotResponse): LabelData {
    return {
        name: lot.displayName || lot.partName,
        partId: lot.partId,
        sku: lot.variantSku || lot.partSKU,
        // Price is the catalog MRP, not a per-lot value; reprint labels omit it.
        price: null,
        batchNumber: lot.manufacturerLotNumber || lot.lotNumber || null,
        mfgDate: lot.receivingDate ?? null,
        expiryDate: lot.expiryDate ?? null,
        isVariant: !!lot.variantId,
        variantName: lot.variantName ?? null,
        defaultQuantity: 1,
    };
}
