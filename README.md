# This is a LINQPad driver for MongoDB

Based on https://github.com/mkjeff/Mongodb.LINQPadDriver

Requirement
-------------
* LINQPad 6.x, 
* .net core 3.x

Installation
-------------
1. Download LPX6 from github
2. Click `Add connection`
3. Click `View more drivers...`
4. Click on 'Install driver from .LPX6 file' and pick the download file.

 
Setup connection
-------------
1. Add connection, choose `Build data context automatically` and select MongoDB Driver click `Next`.
2. Configure some connection information.
> Because MongoDB document is type-less if you want to use the strong-typed document you need to tell the driver where are the type definitions(`Path to typed documents assembly or source file (optional):`) and which namespace's types will be used.

**Note**
> Entities are defined as ```IQueryable<T>```. If you need access to ```IMongoCollection<T>``` there is a method called ```EntityName_Collection()``` for each table.

> The collection type will be exposed as ```IQueryable<BsonDocument>``` or ```IMongoCollection<BsonDocument>``` if no type named as collection name had been found in the assembly or cs file.


To make dumping BsonDocuments as neat as possible the driver implements some CustomMemberProviders and by default ObjectID's are displayed as simple strings. You can display that feature at runtime by setting 
```
MongoDB.LINQPadDriver.MongoDriver.DisableCustomMemberProviders = true;
```
