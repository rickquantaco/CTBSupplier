-- ============================================================
-- Migration: Add notes column to StockItemUnitsOfMeasurementAndPrice
-- Run this against the CTBSupplier database BEFORE deploying
-- the updated application code.
-- ============================================================

ALTER TABLE StockItemUnitsOfMeasurementAndPrice
    ADD notes NVARCHAR(100) NULL;
