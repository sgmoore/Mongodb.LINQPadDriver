#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using LINQPad;
using MongoDB.Bson;

namespace MongoDB.LINQPadDriver.CustomMemberProviders
{
    /// <summary>
    /// CustomMemberProvider used by LinqPad when dumping BsonDocuments
    /// </summary>
    /// <remarks>
    /// Obviously it would be better to provide strongly typed classes, but if we
    /// don't know the type, this will dumping the results in a neater format.
    /// Rather than printing ObjectId, we just display the string representation.
    /// </remarks>
    public class BsonDocumentCustomMemberProvider : ICustomMemberProvider
    {      
        private readonly BsonDocument? _bsonDocument = null;
        public BsonDocumentCustomMemberProvider(BsonDocument bsonDocument)
        {
            _bsonDocument = bsonDocument;
        }

        IEnumerable<string> ICustomMemberProvider.GetNames()  => from elem in _bsonDocument select elem.Name;

        IEnumerable<Type> ICustomMemberProvider.GetTypes()    => from elem in _bsonDocument select BsonTypeMapper.MapToDotNetValue(elem.Value).GetType();

        IEnumerable<object> ICustomMemberProvider.GetValues() =>
            from elem in _bsonDocument
                // Leave BsonDocuments and array of BsonDocuments as they are so that the formatting
                // from this ICustomMemberProvider will apply recursively.
            let leave = elem.Value.IsBsonDocument ||
                (elem.Value.IsBsonArray && elem.Value.AsBsonArray.ToArray().All(a => a.IsBsonDocument))
            select elem.Value.IsObjectId ? "elem.Value.ToString()" :
                leave ? elem.Value : BsonTypeMapper.MapToDotNetValue(elem.Value);
    }

}
