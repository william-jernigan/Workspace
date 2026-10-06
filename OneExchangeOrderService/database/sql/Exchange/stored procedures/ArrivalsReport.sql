
-- ARRIVALS Report
CREATE PROCEDURE [dbo].[usp_GetArrivalsReport]
    -- Date Range: Should be configurable for daily, monthly and 9-months in the reporting tool
    @StartDate datetime = NULL,
    @EndDate datetime = NULL,
    -- Fully qualified property code (e.g. 'CHO-ABC')
    @PropertyCode nvarchar(50) = NULL
    
AS
BEGIN

WITH FilteredOrders AS (
    SELECT 
        o._id AS orderId, 
        oi._id AS orderItemId,
        oi.itemCode, -- Added for property code filtering
        FORMAT(o.orderDate, 'MM/dd/yyyy') AS orderDate,
        o.customerExternalReferenceId,
        o.externalReferenceId,
        o.salesPersonFirstName,
        o.salesPersonLastName,
        o.customerPhoneNumber, -- Added for phone
        o.customerEmailAddress, -- Added for email
        o.customerExternalReferenceId AS memberCode -- Added for member code
    FROM exch_order.[order] o
    JOIN exch_order.orderItem oi ON o._id = oi.orderId
    WHERE o.orderStatus = 'CONFIRMED'
    AND (@PropertyCode IS NULL OR oi.itemCode LIKE '%' + @PropertyCode + '%')

),

OrderAttributes AS (
    SELECT 
        orderItemId,
        MAX(CASE WHEN attributeType = 'PROPERTY_NAME' THEN attributeValue END) AS property_name,
        MAX(CASE WHEN attributeType = 'CHECK_IN_DATE' THEN FORMAT(CAST(attributeValue AS DATETIME), 'MM/dd/yyyy') END) AS check_in_date,
        MAX(CASE WHEN attributeType = 'CHECK_OUT_DATE' THEN FORMAT(CAST(attributeValue AS DATETIME), 'MM/dd/yyyy') END) AS check_out_date,
        MAX(CASE WHEN attributeType = 'UNIT_TYPE' THEN attributeValue END) AS unit_type,
        MAX(CASE WHEN attributeType = 'NUM_ADULTS' THEN CAST(attributeValue AS INT) END) AS num_adults,
        MAX(CASE WHEN attributeType = 'NUM_CHILDREN' THEN CAST(attributeValue AS INT) END) AS num_children
    FROM exch_order.orderItemAttribute oia
    WHERE orderItemId IN (SELECT orderItemId FROM FilteredOrders)

    GROUP BY orderItemId
),

PrimaryGuest AS (
    SELECT 
        og.orderId,
        TRIM(COALESCE(p.firstName, '') + ' ' + COALESCE(p.lastName, '')) AS guest_name
    FROM exch_order.orderGuest og
    JOIN exch_profile.profile p ON og.profileId = p._id
    WHERE og.isPrimary = 1
    AND og.orderId IN (SELECT orderId FROM FilteredOrders)
)

SELECT 
    oa.property_name AS [Resort Name],
    oa.check_in_date AS [Check In Date],
    oa.check_out_date AS [Check Out Date],
    pg.guest_name AS [Guest Name],
    fo.externalReferenceId AS [Booking Confirmation Number],
    'Bluegreen Owner' AS [Booking Affiliate], -- Business expects this value
    fo.orderDate AS [Book Date],
    oa.unit_type AS [Unit Type],
    DATEDIFF(day, oa.check_in_date, oa.check_out_date) AS [Length Of Stay],
    (ISNULL(oa.num_adults, 0) + ISNULL(oa.num_children, 0)) AS [Number Of Occupants],
    CASE 
        WHEN fo.salesPersonFirstName IS NULL THEN 'Member'
        ELSE TRIM(COALESCE(fo.salesPersonFirstName, '') + ' ' + COALESCE(fo.salesPersonLastName, ''))
    END AS [Booked By],
    fo.customerPhoneNumber AS [Phone Number],
    fo.memberCode AS [Member Code],
    fo.customerEmailAddress AS [Email Address]
FROM FilteredOrders fo
LEFT JOIN OrderAttributes oa ON fo.orderItemId = oa.orderItemId
LEFT JOIN PrimaryGuest pg ON fo.orderId = pg.orderId
WHERE oa.check_in_date IS NOT NULL
AND oa.check_in_date >= @StartDate
AND oa.check_out_date <= @EndDate
ORDER BY oa.check_in_date;

END