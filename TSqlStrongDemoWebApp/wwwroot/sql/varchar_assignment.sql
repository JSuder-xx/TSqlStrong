/** ----------------------------------------------------------------------------
--------------------------------------------------------------------------------
When assigning a varchar(X) to a varchar(Y) it is possible to get a run-time
error if X > Y. However, explicit casting renders this a completely safe 
operation. T-SQL Strong will ensure that varchar values can be assigned without
possibility of generating a run-time error.

COMING SOON: All validations will be configurable as Error, Warning, Ignore so
if this validation is too strict for your tastes you can simply disable it. 
--------------------------------------------------------------------------------
-------------------------------------------------------------------------------- */

declare @narrowText varchar(10);

-- OK: Ten characters fit
select @narrowText = '1234567890';
-- ERROR: Eleven characters do not fit.
select @narrowText = '12345678901';
-- OK: With a cast you are safe.
select @narrowText = cast('12345678901' as varchar(10));

declare @wideText varchar(100);
-- OK
select @wideText = @narrowText;
-- ERROR
select @narrowText = @wideText;
-- OK
select @narrowText = cast(@wideText as varchar(10));

if (@wideText = '1234')
begin
	-- OK: T-SQL Strong knows that wide text is actually quite small now.
	select @narrowText = @wideText;
end;

if (@wideText = '1234567890123')
begin
	-- ERROR: Wide text has 13 characters which will not fit into @narrowText
	select @narrowText = @wideText;
end;
