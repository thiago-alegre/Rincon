BEGIN;

CREATE TEMP TABLE affected_cash_register_sessions AS
SELECT DISTINCT sr."CashRegisterSessionId" AS "Id"
FROM "SaleReturns" sr
INNER JOIN "Sales" s ON s."Id" = sr."SaleId"
WHERE sr."CashRegisterSessionId" IS DISTINCT FROM s."CashRegisterSessionId"
  AND sr."CashRegisterSessionId" IS NOT NULL
  AND s."CashRegisterSessionId" IS NOT NULL
UNION
SELECT DISTINCT s."CashRegisterSessionId" AS "Id"
FROM "SaleReturns" sr
INNER JOIN "Sales" s ON s."Id" = sr."SaleId"
WHERE sr."CashRegisterSessionId" IS DISTINCT FROM s."CashRegisterSessionId"
  AND s."CashRegisterSessionId" IS NOT NULL;

UPDATE "SaleReturns" sr
SET "CashRegisterSessionId" = s."CashRegisterSessionId"
FROM "Sales" s
WHERE s."Id" = sr."SaleId"
  AND s."CashRegisterSessionId" IS NOT NULL
  AND sr."CashRegisterSessionId" IS DISTINCT FROM s."CashRegisterSessionId";

WITH expected AS (
    SELECT
        c."Id",
        c."OpeningAmount"
            + COALESCE(SUM(
                CASE
                    WHEN s."Id" IS NULL OR s."IsVoided" THEN 0
                    WHEN s."CashAmount" > 0 OR s."TransferAmount" > 0 THEN s."CashAmount"
                    WHEN s."PaymentMethod" = 1 THEN s."Total"
                    ELSE 0
                END
            ), 0)
            + COALESCE((
                SELECT SUM(p."Amount")
                FROM "PersonalAccountPayments" p
                WHERE p."CashRegisterSessionId" = c."Id"
                  AND p."PaymentMethod" = 1
            ), 0)
            - COALESCE((
                SELECT SUM(
                    CASE
                        WHEN linked_sale."Id" IS NULL THEN
                            CASE WHEN sr."PaymentMethod" = 1 THEN sr."Total" ELSE 0 END
                        WHEN linked_sale."IsVoided" THEN 0
                        WHEN linked_sale."CashAmount" > 0 OR linked_sale."TransferAmount" > 0 THEN linked_sale."CashAmount"
                        WHEN linked_sale."PaymentMethod" = 1 THEN linked_sale."Total"
                        ELSE 0
                    END
                )
                FROM "SaleReturns" sr
                LEFT JOIN "Sales" linked_sale ON linked_sale."Id" = sr."SaleId"
                WHERE sr."CashRegisterSessionId" = c."Id"
            ), 0) AS "ExpectedCash"
    FROM "CashRegisterSessions" c
    LEFT JOIN "Sales" s ON s."CashRegisterSessionId" = c."Id"
    WHERE c."Id" IN (SELECT "Id" FROM affected_cash_register_sessions WHERE "Id" IS NOT NULL)
      AND c."ClosedAt" IS NOT NULL
      AND c."CountedCashAmount" IS NOT NULL
    GROUP BY c."Id", c."OpeningAmount"
)
UPDATE "CashRegisterSessions" c
SET "ExpectedCashAmount" = e."ExpectedCash",
    "Difference" = c."CountedCashAmount" - e."ExpectedCash"
FROM expected e
WHERE e."Id" = c."Id";

SELECT
    c."Id" AS "Caja",
    c."OpeningAmount" AS "Monto inicial",
    c."ExpectedCashAmount" AS "Efectivo esperado",
    c."CountedCashAmount" AS "Efectivo contado",
    c."Difference" AS "Diferencia"
FROM "CashRegisterSessions" c
WHERE c."Id" IN (SELECT "Id" FROM affected_cash_register_sessions WHERE "Id" IS NOT NULL)
ORDER BY c."Id";

COMMIT;
