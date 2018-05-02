/** ----------------------------------------------------------------------------
--------------------------------------------------------------------------------
This example does not illustrate any exotic new features but it does show
that T-SQL Strong can validate cursors. 

Try making changes to the code below that will cause type errors. 

Examples include:
  1. Changing a name of a cursor, column, or table to be wrong.
  2. Look for TRY IT comments below for other suggestions.
--------------------------------------------------------------------------------
-------------------------------------------------------------------------------- */

CREATE TABLE PURCHASING.VENDOR (
    VendorID int not null,
    Name nvarchar(100),
    PreferredVendorStatus int
);
GO

CREATE TABLE PURCHASING.ProductVendor (
    ProductID int not null,
    VendorID int not null
);
GO

CREATE TABLE Production.Product (
    ProductID int not null,
    Name nvarchar(100)
);
GO

DECLARE @vendor_id int, @vendor_name nvarchar(50),  
    @message nvarchar(80), @product nvarchar(50);  

-- Vendor Products Report 

DECLARE vendor_cursor CURSOR FOR   
SELECT VendorID, Name  
FROM Purchasing.Vendor  
WHERE PreferredVendorStatus = 1  
ORDER BY VendorID;  

OPEN vendor_cursor  

FETCH NEXT FROM vendor_cursor   
-- TRY IT: The vendor_cursor has two columns. Try adding a third variable to the INTO variable list and 
-- see what T-SQL Strong has to say about it.
INTO @vendor_id, @vendor_name

WHILE @@FETCH_STATUS = 0  
BEGIN  
    PRINT @vendor_name;
   
    -- Declare an inner cursor based     
    -- on vendor_id from the outer cursor.  

    DECLARE product_cursor CURSOR FOR   
    SELECT v.Name  
    FROM Purchasing.ProductVendor pv, Production.Product v  
    WHERE pv.ProductID = v.ProductID AND  
    pv.VendorID = @vendor_id  -- Variable value from the outer cursor  

    OPEN product_cursor  
    FETCH NEXT FROM product_cursor INTO @product  

    IF @@FETCH_STATUS <> 0   
        PRINT '         <<None>>'       

    WHILE @@FETCH_STATUS = 0
    BEGIN      
        SELECT @message = N'         ' + Coalesce(@product, N'');
        PRINT @message  
        FETCH NEXT FROM product_cursor INTO @product  
    END  

    CLOSE product_cursor  
    DEALLOCATE product_cursor  
        -- Get the next vendor.  
    FETCH NEXT FROM vendor_cursor   
    INTO @vendor_id, @vendor_name  
END   
CLOSE vendor_cursor;  
DEALLOCATE vendor_cursor; 
