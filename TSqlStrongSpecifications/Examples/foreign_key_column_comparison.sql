create table Parent (id int not null, constraint PK_Parent primary key clustered (id)); 
create table Child (id int not null, Parent_id int not null foreign key references Parent(id), constraint PK_Child primary key clustered (id));

-- OK 
select * from Parent inner join Child on Child.Parent_ID = Parent.ID
-- Type Error
select * from Parent inner join Child on Child.ID = Parent.ID
