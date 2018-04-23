declare @one int = 2;
declare @two int = 1;
declare @three int;

select @one + @two; -- OK
select @one + @three; -- Null Warning 
select @one + 'apples'; -- ERROR
select @one + null; -- ERROR
