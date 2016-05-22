begin try
begin tran

--INSERT INTO Person(email,isActive,passwordHash,salt,registartionDate)
 --VALUES ('testmail@screntaker.com',1,convert(VARBINARY(max), '893hf9h347hy8934798g'), 'efwei3', 22-05-2016 )
INSERT INTO Person(email,isActive,passwordHash,salt)
VALUES ('testmail@screntaker.ddd',1,convert(VARBINARY(max), LEFT(CAST(NEWID() AS VARCHAR(36)),15)),LEFT(CAST(NEWID() AS VARCHAR(36)),8))

INSERT INTO Folder(name,ownerId,isPublic,sharedCode)
VALUES ('General',IDENT_CURRENT('Person'),0,convert(VARBINARY(max), LEFT(CAST(NEWID() AS VARCHAR(36)),15)))

commit tran
end try
begin catch
rollback tran
end catch;