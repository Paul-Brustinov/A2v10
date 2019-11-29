﻿/* DEMO */
------------------------------------------------
set noexec off;
go
------------------------------------------------
if DB_NAME() = N'master'
begin
	declare @err nvarchar(255);
	set @err = N'Error! Can not use the master database!';
	print @err;
	raiserror (@err, 16, -1) with nowait;
	set noexec on;
end
go

------------------------------------------------
if not exists(select * from INFORMATION_SCHEMA.SCHEMATA where SCHEMA_NAME=N'a2v10demo')
begin
	exec sp_executesql N'create schema a2v10demo';
end
go
------------------------------------------------
begin
	set nocount on;
	grant execute on schema ::a2v10demo to public;
end
go
------------------------------------------------
if not exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA=N'a2v10demo' and TABLE_NAME=N'Attachments')
begin
	create table a2v10demo.Attachments
	(
		Id bigint identity(100, 1) not null constraint PK_Attachments primary key,
		Name nvarchar(255) null,
		Mime nvarchar(255) null,
		Data varbinary(max) null,
		DateCreated datetime not null constraint DF_Attachments_DateCreated default(getdate()),
		UserId bigint not null
	);
end

------------------------------------------------
if not exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA=N'a2v10demo' and TABLE_NAME=N'Catalog.Customers')
begin
	create table a2v10demo.[Catalog.Customers] (
		Id bigint identity(100, 1) not null constraint [PK_Catalog.Customers] primary key,
		Name nvarchar(255) null,
		Amount money null,
		Memo nvarchar(255) null,
		[Date] datetime null,
		Photo bigint null
	) 
end
go


--alter table a2v10demo.[Catalog.Customers] add Photo bigint null;
--alter table a2v10demo.[Catalog.Customers] add [Date] datetime null;
------------------------------------------------
create or alter procedure a2v10demo.[Catalog.Customer.Index]
@UserId bigint,
@Id bigint = null, -- если вызывается как Browse
@Order nvarchar(255) = null,
@Dir nvarchar(255) = null,
@Filter nvarchar(255) = null,
@Offset int = 0,
@PageSize int = 4,
@InStockOnly bit = 0,
@DocumentId bigint = null
as
begin
	set nocount on;

	--throw 60000,  @DocumentId, 0;
	-- TODO get page size form meta

	declare @Asc nvarchar(10), @Desc nvarchar(10);
	set @Asc = N'asc'; set @Desc = N'desc';
	set @Dir = lower(@Dir);
	set @Offset = isnull(@Offset, 0);

	--raiserror(@Dir , 16, -1) with nowait;

	with T as (
		select Id, Name, Amount, Memo, Photo, [Date],
			_RowNumber = row_number() over (
			order by
			case when @Order=N'Id' and @Dir = @Asc then c.Id end asc,
			case when @Order=N'Id' and @Dir = @Desc  then c.Id end desc,
			case when @Order=N'Name' and @Dir = @Asc then c.[Name] end asc,
			case when @Order=N'Name' and @Dir = @Desc  then c.[Name] end desc,
			case when @Order=N'Amount' and @Dir = @Asc then c.[Amount] end asc,
			case when @Order=N'Amount' and @Dir = @Desc  then c.[Amount] end desc,
			case when @Order=N'Memo' and @Dir = @Asc then c.[Memo] end asc,
			case when @Order=N'Memo' and @Dir = @Desc  then c.[Memo] end desc
			)
		from a2v10demo.[Catalog.Customers] c
		where @Filter is null or upper(c.Name) like N'%' + upper(@Filter) + N'%'
	)
	select top(@PageSize) [Customers!TCustomer!Array]=null, [Id!!Id] = Id, Name, Amount, Memo, Photo, [Date],
		[!!RowCount] = (select count(1) from T)
	from T 
		where [_RowNumber] > @Offset and [_RowNumber] <= @Offset + @PageSize
	order by [_RowNumber];

	-- служебный набор данных добавляется в Root
	select [!$System!] = null, [!!PageSize] = @PageSize;
end
go
------------------------------------------------
create or alter procedure a2v10demo.[Catalog.Customer.Load]
@UserId bigint,
@Id bigint = null /*for create new */
as
begin
	set nocount on;
	select [Customer!TCustomer!Object]=null, [Id!!Id] = Id, Name, Amount, Memo, [Date],
		Photo,
		[Images!TImage!Array] = null
	from a2v10demo.[Catalog.Customers] where Id=@Id;

	select [!TImage!Array] = null, [Id!!Id] = 51, [Photo] = 119, [!TCustomer.Images!ParentId] = @Id
	union all
	select [!TImage!Array] = null, [Id!!Id] = 52, [Photo] = 120, [!TCustomer.Images!ParentId] = @Id;
end
go
------------------------------------------------
create or alter procedure a2v10demo.[Catalog.Customer.Photo.Load]
@UserId bigint,
@Id bigint = null,
@Key nvarchar(255)
as
begin
	set nocount on;
	select Mime, Stream = Data, Name from a2v10demo.Attachments where Id=@Id;
end
go
------------------------------------------------
create or alter procedure a2v10demo.[Catalog.Customer.Photo.Update]
@UserId bigint,
@Id bigint,
@Key nvarchar(255),
@Mime nvarchar(255),
@Name nvarchar(255),
@Stream varbinary(max),
@RetId bigint output
as
begin
	set nocount on;
	declare @rtable table (Id bigint);
	
	insert into a2v10demo.Attachments(UserId, Name, Mime, Data)
		output inserted.Id into @rtable
	values(@UserId, @Name, @Mime, @Stream);

	select @RetId = Id from @rtable;
end
go

--select * from a2frontis_wt.a2data.Attachments where Id=108


------------------------------------------------
if exists (select * from INFORMATION_SCHEMA.ROUTINES where ROUTINE_SCHEMA=N'a2v10demo' and ROUTINE_NAME=N'Catalog.Customer.Metadata')
	drop procedure a2v10demo.[Catalog.Customer.Metadata]
go
------------------------------------------------
if exists (select * from INFORMATION_SCHEMA.ROUTINES where ROUTINE_SCHEMA=N'a2v10demo' and ROUTINE_NAME=N'Catalog.Customer.Update')
	drop procedure a2v10demo.[Catalog.Customer.Update]
go
------------------------------------------------
if exists (select * from INFORMATION_SCHEMA.DOMAINS where DATA_TYPE = N'table type'and DOMAIN_SCHEMA=N'a2v10demo' and DOMAIN_NAME=N'Catalog.Customer.TableType')
	drop type a2v10demo.[Catalog.Customer.TableType]
go
------------------------------------------------
create type [a2v10demo].[Catalog.Customer.TableType] as
table (
	[Id] bigint null,
	[Name] nvarchar(255),
	[Amount] money,
	[Memo] nvarchar(255),
	[Photo] bigint,
	[Date] datetime
)
go
------------------------------------------------
create procedure a2v10demo.[Catalog.Customer.Metadata]
as
begin
	set nocount on;
	declare @Customer a2v10demo.[Catalog.Customer.TableType];
	/*!!! Всегда нужен путь в модели !!!*/
	select [Customer!Customer!Metadata]=null, * from @Customer;
end
go
------------------------------------------------
create procedure a2v10demo.[Catalog.Customer.Update]
@UserId bigint,
@Customer a2v10demo.[Catalog.Customer.TableType] readonly
as
begin
	set nocount on;
	/*
	declare @msg nvarchar(max);
	select @msg = (select * from @Customer for xml auto);
	throw 60000,  @msg, 0;
	*/
	declare @output table(op sysname, id bigint);
	declare @RetId bigint;

	merge a2v10demo.[Catalog.Customers] as target
	using @Customer as source
	on (target.Id = source.Id)
	when matched then
		update set 
			target.[Name] = source.[Name],
			target.[Amount] = source.Amount,
			target.[Memo] = source.[Memo],
			target.[Photo] = source.[Photo],
			target.[Date] = source.[Date]
	when not matched by target then
		insert (Name, Amount, Memo, Photo, [Date])
		values (Name, Amount, Memo, Photo, [Date])
	output 
		$action op, inserted.Id id
	into @output(op, id);
	select top(1) @RetId = id from @output;

	exec [a2v10demo].[Catalog.Customer.Load] @UserId, @RetId;
end
go

------------------------------------------------
create or alter procedure a2v10demo.[Catalog.Invoke]
@UserId bigint,
@Id bigint = null,
@Name nvarchar(255) = null,
@Memo nvarchar(255) = null,
@Amount money = null,
@Photo bigint = null
as
begin
	set nocount on;
	--throw 60000, @Name, 0;
	exec a2v10demo.[Catalog.Customer.Load] @UserId, @Id;
end
go


------------------------------------------------
create or alter procedure [a2demo].[Agent.Copy]
	@TenantId int = null,
	@UserId bigint,
	@Id bigint = null,
	@Name nvarchar(255) = null
as
begin
	set nocount on;
	set transaction isolation level read uncommitted;
	select [Agent!TAgent!Object] = null, [Id!!Id] = cast(0 as bigint), [Name!!Name] = [Name], [Type], 
		Code, Tag, Memo, Folder, ParentFolder=Parent, Phone,
		[Address!TAddress!Object] = null,
		DateCreated, DateModified
	from a2demo.Agents where Id=@Id and Void=0;

	select [!TAddress!Object] = null, [Id!!Id] = Id, [!TAgent.Address!ParentId] = cast(0 as bigint),
		Country, City, Street, Build, Appt 
	from a2demo.Addresses where Agent = @Id;

	select [Countries!TCountry!Array] = null, [Code!!Id] = Code, [Name!!Name] = [Name], [Cities!TCity!LazyArray] = null
	from a2demo.Countries;

	-- we need type declaration for City
	select [!TCity!Array] = null, [Id!!Id] = Id, [Name!!Name] = [Name], [Streets!TStreet!LazyArray] = null
	from a2demo.Cities where 0 <> 0; 

	-- we need type declaration for Street
	select [!TStreet!Array] = null, [Id!!Id] = Id, [Name!!Name] = [Name]
	from a2demo.Streets where 0 <> 0; 

	select [Params!TParam!Object] = null, [Name] = @Name;
	select [!$System!] = null, [!!Copy] = 1;
end
go

------------------------------------------------
if not exists(select * from INFORMATION_SCHEMA.SCHEMATA where SCHEMA_NAME=N'a2user_state')
begin
	exec sp_executesql N'create schema a2user_state';
	grant execute on schema ::a2user_state to public;
end
go

------------------------------------------------
if not exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA=N'a2user_state' and TABLE_NAME=N'UserInfo')
begin
	create table a2user_state.UserInfo
	(
		UserId bigint not null,
		[Key] nvarchar(32) not null,
		[StringVal] nvarchar(255) null,
		[DateFrom] datetime null,
		[DateTo] datetime null,
		[BoolVal] bit null,
		[IntVal] bigint null,
		[FloatVal] float null,
		constraint PK_UserInfo primary key(UserId, [Key])
	);
end
go
------------------------------------------------
create or alter procedure a2user_state.[SetGlobalPeriod]
@TenantId int = 0,
@UserId bigint,
@From datetime,
@To datetime
as
begin
	set nocount on;
	set transaction isolation level read committed;
	update a2user_state.UserInfo set DateFrom=@From, DateTo = @To
	where UserId = @UserId and [Key] = N'Period';
	if @@rowcount = 0
		insert into a2user_state.UserInfo(UserId, [Key], [DateFrom], [DateTo])
			values (@UserId, N'Period', @From, @To);
end
go

------------------------------------------------
create or alter procedure a2user_state.GetUserGlobalPeriod(@UserId bigint, @From datetime = null output, @To datetime = null output)
as
	set nocount on;
	set transaction isolation level read uncommitted;
	if @From is null and @To is null
	begin
		select @From = [DateFrom], @To = DateTo 
		from a2user_state.UserInfo where UserId = @UserId;
		if @From is null
			set @From = a2sys.fn_trimtime(getdate());
		if @To is null
			set @To = @From;
	end
go
------------------------------------------------
set noexec off;
go

--delete from a2v10demo.[Catalog.Customers] where Id>=105

