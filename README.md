# DataLayer.Oracle

**DataLayer.Oracle** is a basic DataLayer implementation for calling PLSQL Stored Procedures. Built on .NET 5.0 framework and uses Oracle.ManagedDataAccess package for Oracle Provider.

  

# Example Usage
## Case  #1: Getting Cursors 
**provider.ExecuteReader** method fetchs SYS_REFCURSOR result into given T type which is a class represents the entity for the cursor.

 - If your PLSQL stored procedure does not have any Input parameters, you can pass null to the second parameter of ParameterCollection constructor.
 - If your PLSQL stored procedure has any Output parameters, you can pass List of Tuple<string, object> as third parameter to ParameterCollection constructor.

```
OracleProvider provider = new OracleProvider();
string connStr = "User Id=ORACLE_USR;Password=PWD;Data Source=DDB_NAME";
provider.SetConnectionString(connStr);
ParameterCollection param = new ParameterCollection("STORED_PROCEDURE_NAME", 
    new List<Tuple<string, object>>
{
    new Tuple<string, object>("INPUTPARAMETER1", "value1"),
    new Tuple<string, object>("INPUTPARAMETER2", "value2")
});
provider.ExecuteReader<T>(ref param);
```
## Case  #2: Executing PLSQL Stored Procedures for CRUD Operations 
**provider.ExecuteBasicStoredProcedure** method executes given PLSQL Stored Procedure and commits or rollbacks automatically. If you execute a Function, return parameter will be stored in *ResultMessage* property of  *ExecuteBasicStoredProcedure*'s response.
 - If your PLSQL stored procedure does not have any Input parameters, you can pass null to the second parameter of ParameterCollection constructor.
 - If your PLSQL stored procedure has any Output parameters, you can pass List of Tuple<string, object> as third parameter to ParameterCollection constructor.
```
OracleProvider provider = new OracleProvider();
string connStr = "User Id=ORACLE_USR;Password=PWD;Data Source=DDB_NAME";
provider.SetConnectionString(connStr);
ParameterCollection param = new ParameterCollection("STORED_PROCEDURE_NAME", 
    new List<Tuple<string, object>>
{
    new Tuple<string, object>("INPUTPARAMETER1", "value1"),
    new Tuple<string, object>("INPUTPARAMETER2", "value2")
});
provider.ExecuteBasicStoredProcedure(ref param);
```
Also, you can use **ExecuteStoredProcedure** method for your CRUD operations which also returns cursor. Cursor will be fetched into List of given T type.
```
OracleProvider provider = new OracleProvider();
string connStr = "User Id=ORACLE_USR;Password=PWD;Data Source=DDB_NAME";
provider.SetConnectionString(connStr);
ParameterCollection param = new ParameterCollection("STORED_PROCEDURE_NAME", 
    new List<Tuple<string, object>>
{
    new Tuple<string, object>("INPUTPARAMETER1", "value1"),
    new Tuple<string, object>("INPUTPARAMETER2", "value2")
});
var result = provider.ExecuteStoredProcedure<T>(ref param);
if (string.IsNullOrEmpty(result.ResultMessage))
{
    foreach (var item in result.List)
    {
        Console.WriteLine("...");
    }
}
else
{
    Console.WriteLine("Error occured when executing PLSQL SP: " + result.ResultMessage);
}
```
