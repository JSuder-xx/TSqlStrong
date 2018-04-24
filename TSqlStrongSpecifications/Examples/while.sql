declare @myInitializedCounter int = 20;
-- OK: @myInitializedCounter is clearly not null
while (@myInitializedCounter > 0)
begin
    print @myInitializedCounter;

    set @myInitializedCounter = @myInitializedCounter - 1;
end;

declare @myUninitializedCounter int; -- Uh oh. We forgot to initialize

-- ERROR: Comparing possible null value
while (@myUninitializedCounter > 0)
begin
    print @myUninitializedCounter;

    -- ERROR: Binary operation with possibly null value
    set @myUninitializedCounter = @myUninitializedCounter - 1;
end;