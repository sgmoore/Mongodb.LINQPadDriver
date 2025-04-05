#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LINQPad;
using MongoDB.Bson;

namespace MongoDB.LINQPadDriver.CustomMemberProviders
{
    /// <summary>
    /// CustomMemberProvider used by LinqPad when dumping Custom Entities
    /// </summary>
    /// <remarks>
    /// Rather than printing ObjectId, we just display the string representation.
    /// </remarks>
    public class EntityCustomMemberProvider : ICustomMemberProvider
    {
        private readonly object? _objectToWrite = null;

        private readonly PropertyInfo[] _propsToWrite;

        public EntityCustomMemberProvider(object objectToWrite)
        {
            _objectToWrite = objectToWrite;
            _propsToWrite = objectToWrite.GetType().GetProperties().ToArray();
        }

        public IEnumerable<string> GetNames()
        {
            return _propsToWrite.Select(p => p.Name);
        }

        public IEnumerable<Type> GetTypes()
        {
            return _propsToWrite.Select(p => p.PropertyType == typeof(ObjectId) ? typeof(string) : p.PropertyType);
        }

        public IEnumerable<object> GetValues()
        {
            return from p in _propsToWrite
                    let value = p.GetValue(_objectToWrite, null)
                    select p.PropertyType == typeof(ObjectId) ? ((ObjectId)value).ToString() : value;
        }
    }

}
