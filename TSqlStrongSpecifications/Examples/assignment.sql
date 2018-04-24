declare @myInt int = 1; -- OK
declare @uninitialized int;
declare @sometext varchar(20) = 'Hi'; -- OK

set @uninitialized = @myInt; -- OK
select @uninitialized = 1; -- OK
set @uninitialized = (select 1); -- OK

select @someText = 'Other'; -- OK

select @someText = @myInt; -- ERROR
set @someText = @myInt; -- ERROR

select @uninitialized = @someText; -- ERROR
set @uninitialized = @someText; -- ERROR
set @uninitialized = (select 'BOB'); -- ERROR





