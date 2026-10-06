-- CANCELLED Bookings Report
 CREATE PROCEDURE [dbo].[usp_GetCancellationsReport]
    -- Date Range: Should be configurable for daily, monthly and 9-months in the reporting tool
     @StartDate datetime,
     @EndDate datetime
AS
BEGIN

WITH FilteredOrders AS (
    SELECT DISTINCT
        o._id AS orderId, 
        oi._id AS orderItemId,
        oi.itemCode,        
        o.externalSourceSystem,
        FORMAT(o.orderDate, 'MM/dd/yyyy') AS orderDate,
        o.customerExternalReferenceId,
        o.subtotal,
        o.externalReferenceId,
        o.memo,
        o.salesPersonFirstName,
        o.salesPersonLastName,
        o.customerMemberLevel,
        o.salesPersonEmail
    FROM exch_order.[order] o
    JOIN exch_order.orderItem oi ON o._id = oi.orderId
        LEFT JOIN exch_order.orderReturn r ON r.orderId = o._id
    WHERE o.orderStatus = 'CANCELLED'
    AND o.externalSourceSystem = 'CHO'
   AND (@StartDate IS NULL OR r.returnDate >= @StartDate)
AND (@EndDate IS NULL OR r.returnDate < @EndDate)
   
),

OrderAttributes AS (
    SELECT DISTINCT
        orderItemId,
        MAX(CASE WHEN attributeType = 'PROPERTY_NAME' THEN attributeValue END) AS property_name,
       MAX(CASE WHEN attributeType = 'CHECK_IN_DATE' 
         THEN TRY_CONVERT(datetime, attributeValue) END) AS check_in_date,
         MAX(CASE WHEN attributeType = 'CHECK_OUT_DATE' 
         THEN TRY_CONVERT(datetime, attributeValue) END) AS check_out_date,
        MAX(CASE WHEN attributeType = 'UNIT_TYPE' THEN attributeValue END) AS unit_type,
        MAX(CASE WHEN attributeType = 'NUM_ADULTS' THEN CAST(attributeValue AS INT) END) AS num_adults,
        MAX(CASE WHEN attributeType = 'NUM_CHILDREN' THEN CAST(attributeValue AS INT) END) AS num_children,
        MAX(CASE WHEN attributeType = 'PENALTY_DESCRIPTION' THEN attributeValue END) AS cancellation_policy
    FROM exch_order.orderItemAttribute
    WHERE orderItemId IN (SELECT orderItemId FROM FilteredOrders)
    GROUP BY orderItemId
),

TransactionDetails AS (
    SELECT 
        orderId,
        MAX(CASE WHEN transactionType = 'PURCHASE_BY_POINTS' THEN toTransactionAmount END) AS listing_affiliate_points,
        MAX(CASE WHEN transactionType = 'PURCHASE_BY_POINTS' THEN fromTransactionAmount END) AS points_used,
        MAX(CASE WHEN transactionType = 'ORDER_FEE' THEN fromTransactionAmount END) AS booking_fees,
        MAX(CASE WHEN transactionType = 'CANCELLATION_FEE' THEN fromTransactionAmount END) AS cancel_fees,
        MAX(CASE WHEN transactionType = 'POINTS_REFUND' THEN toTransactionAmount END) AS refund_points,
        MAX(CASE WHEN transactionType = 'PURCHASE_BY_POINTS' THEN toAccountId END) AS choice_privilege_number
    FROM exch_order.exchangeTransactionLog
    WHERE orderId IN (SELECT orderId FROM FilteredOrders)
    GROUP BY orderId
),

CancellationDetails AS (
    SELECT 
        r.orderId,
        r.externalReferenceId AS cancellation_number,
        FORMAT(r.returnDate, 'MM/dd/yyyy') AS cancellation_date,
        r.memo AS cancellation_comments
    FROM exch_order.orderReturn r
    WHERE r.orderId IN (SELECT orderId FROM FilteredOrders)
),

PrimaryGuest AS (
    SELECT 
        og.orderId,
		guest_name = CASE
  WHEN NULLIF(TRIM(p.firstName), '') IS NOT NULL
    AND NULLIF(TRIM(p.lastName),  '') IS NOT NULL
    THEN (p.firstName) + ' ' + TRIM(p.lastName)
  WHEN NULLIF(TRIM(p.firstName), '') IS NOT NULL
    THEN TRIM(p.firstName)                 
  WHEN NULLIF(TRIM(p.lastName),  '') IS NOT NULL
    THEN TRIM(p.lastName)
  ELSE 'Member'
END
    FROM exch_order.orderGuest og
    JOIN exch_profile.profile p ON og.profileId = p._id
    WHERE og.isPrimary = 1
    AND og.orderId IN (SELECT orderId FROM FilteredOrders)
)

SELECT DISTINCT
    fo.externalSourceSystem AS [Affiliate Code],
    oa.property_name AS [Resort Name],
   
 
    FORMAT(CONVERT(DATETIME,oa.check_in_date) , 'MM/dd/yyyy') AS [Check In Date],
        FORMAT(CONVERT(DATETIME,oa.check_out_date) , 'MM/dd/yyyy') AS [Check Out Date],
    FORMAT(CONVERT(DATETIME,   cd.cancellation_date), 'MM/dd/yyyy') AS [Cancellation Date],
    fo.orderDate AS [Booking Date],
    fo.customerExternalReferenceId AS [Member Number],
    pg.guest_name AS [Guest Name],
    fo.subtotal AS [Bluegreen Points],
    td.listing_affiliate_points AS [Listing Affiliate Points],
    td.points_used AS [Points Used],
    COALESCE(td.booking_fees, 0) AS [Booking Fees],
    COALESCE(td.cancel_fees, 0) AS [Cancel Fees],
    COALESCE(td.refund_points, 0) AS [Refund Points],    
    cd.cancellation_number AS [Cancellation Confirmation Number], 
    oa.cancellation_policy AS [Cancellation Policy],
    cd.cancellation_comments AS [Cancellation Comments],
    fo.externalReferenceId AS [Booking Confirmation Number],
    DATEDIFF(day, oa.check_in_date, oa.check_out_date) AS [Length Of Stay],
    oa.unit_type AS [Unit Type],
    (ISNULL(oa.num_adults, 0) + ISNULL(oa.num_children, 0)) AS [Number Of Occupants],
    fo.memo AS [Booking Comments],
    CASE
  WHEN NULLIF(LTRIM(RTRIM(fo.salesPersonFirstName)), '') IS NULL
   AND NULLIF(LTRIM(RTRIM(fo.salesPersonLastName)),  '') IS NULL
    THEN 'Member'
  ELSE TRIM(
         CONCAT(
           COALESCE(NULLIF(TRIM(fo.salesPersonFirstName), ''), ''),
           CASE
             WHEN NULLIF(TRIM(fo.salesPersonFirstName), '') IS NOT NULL
              AND NULLIF(TRIM(fo.salesPersonLastName),  '') IS NOT NULL
             THEN ' ' ELSE '' END,
           COALESCE(NULLIF(TRIM(fo.salesPersonLastName),  ''), '')
         )
       )

    END AS [Booked By],
    pg.guest_name AS [Choice Member Name],
    td.choice_privilege_number AS [Choice Privilege Number],
    fo.customerMemberLevel AS [Membership Level],
    fo.salesPersonEmail AS [Booking Agent Email]
FROM FilteredOrders fo
LEFT JOIN OrderAttributes oa ON fo.orderItemId = oa.orderItemId
LEFT JOIN TransactionDetails td ON fo.orderId = td.orderId
LEFT JOIN CancellationDetails cd ON fo.orderId = cd.orderId
LEFT JOIN PrimaryGuest pg ON fo.orderId = pg.orderId
ORDER BY fo.orderDate;
END
