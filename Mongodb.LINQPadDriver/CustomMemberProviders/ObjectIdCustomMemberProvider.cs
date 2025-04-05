#nullable enable
using System;
using System.Collections.Generic;
using LINQPad;
using MongoDB.Bson;

namespace MongoDB.LINQPadDriver.CustomMemberProviders
{

    public class ObjectIdCustomMemberProvider : ICustomMemberProvider
    {
        private readonly ObjectId? _objectId;
        public ObjectIdCustomMemberProvider(ObjectId? oid)
        {
            _objectId = oid;
        }

        // Empty string tells LINQPad to “collapse” the containing object.)
        IEnumerable<string> ICustomMemberProvider.GetNames()  => new string[] { "" };

        IEnumerable<Type> ICustomMemberProvider.GetTypes()    => new Type[]   { typeof(string) };

        IEnumerable<object?> ICustomMemberProvider.GetValues() => new object?[] { _objectId?.ToString() };
    }
}
