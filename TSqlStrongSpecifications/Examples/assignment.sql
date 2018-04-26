declare @myInt int = 1; -- OK
declare @uninitializedInt int;
declare @sometext varchar(20) = 'Hi'; -- OK

set @uninitializedInt = @myInt; -- OK
select @uninitializedInt = 1; -- OK
set @uninitializedInt = (select 1); -- OK

select @someText = 'Other'; -- OK

select @someText = @myInt; -- ERROR
set @someText = @myInt; -- ERROR
set @someText = cast(@myInt as varchar(100)); -- OK

select @uninitializedInt = @someText; -- ERROR
select @uninitializedInt = cast(@someText as int); -- OK
set @uninitializedInt = @someText; -- ERROR
set @uninitializedInt = (select 'BOB'); -- ERROR





