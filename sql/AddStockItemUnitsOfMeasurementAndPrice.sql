-- ============================================================
-- Migration: Add StockItemUnitsOfMeasurementAndPrice table
-- Run this against the target database BEFORE deploying the
-- updated application code.
-- ============================================================

BEGIN TRANSACTION;

-- 1. Create the new pricing-tier table
CREATE TABLE StockItemUnitsOfMeasurementAndPrice (
    id                    INT              IDENTITY(1,1) NOT NULL,
    supplierGUID          UNIQUEIDENTIFIER NOT NULL,
    stockCode             NVARCHAR(250)    NOT NULL,
    supplierCost          MONEY            NOT NULL DEFAULT 0,
    stockUnit             FLOAT            NOT NULL DEFAULT 0,
    unitOfMeasurementName NVARCHAR(50)     NOT NULL DEFAULT '',
    sortOrder             INT              NOT NULL DEFAULT 0,

    CONSTRAINT PK_StockItemUnitsOfMeasurementAndPrice
        PRIMARY KEY (id),

    CONSTRAINT FK_StockItemUnitsOfMeasurementAndPrice_StockItem
        FOREIGN KEY (supplierGUID, stockCode)
        REFERENCES StockItem (supplierGUID, stockCode)
        ON DELETE CASCADE
);

-- 2. Migrate existing data — one tier per stock item (sortOrder = 0)
INSERT INTO StockItemUnitsOfMeasurementAndPrice
    (supplierGUID, stockCode, supplierCost, stockUnit, unitOfMeasurementName, sortOrder)
SELECT
    supplierGUID,
    stockCode,
    supplierCost,
    stockUnit,
    unitOfMeasurementName,
    0
FROM StockItem;

-- 3. Remove the three columns now held in StockItemUnitsOfMeasurementAndPrice
ALTER TABLE StockItem DROP COLUMN supplierCost;
ALTER TABLE StockItem DROP COLUMN stockUnit;
ALTER TABLE StockItem DROP COLUMN unitOfMeasurementName;

COMMIT TRANSACTION;
