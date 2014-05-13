// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Entity.Relational.Model;

namespace Microsoft.Data.Entity.SqlServer
{
    public class SqlServerTypeMapper : RelationalTypeMapper
    {
        // This dictionary is for invariant mappings from a sealed CLR type to a single
        // store type. If the CLR type is unsealed or if the mapping varies based on how the
        // type is used (e.g. in keys), then add custom mapping below.
        // TODO: Linear lookup is probably faster than dictionary
        private readonly IDictionary<Type, RelationalTypeMapping> _simpleMappings = new Dictionary<Type, RelationalTypeMapping>()
            {
                { typeof(int), new RelationalTypeMapping("int", DbType.Int32) },
                { typeof(DateTime), new RelationalTypeMapping("datetime2", DbType.DateTime2) },
                { typeof(Guid), new RelationalTypeMapping("uniqueidentifier", DbType.Guid) },
                { typeof(char), new RelationalTypeMapping("int", DbType.Int32) },
                { typeof(bool), new RelationalTypeMapping("bit", DbType.Boolean) },
                { typeof(byte), new RelationalTypeMapping("tinyint", DbType.Byte) },
                { typeof(double), new RelationalTypeMapping("float", DbType.Double) },
                { typeof(sbyte), new RelationalTypeMapping("smallint", DbType.SByte) },
                { typeof(ushort), new RelationalTypeMapping("int", DbType.UInt16) },
                { typeof(uint), new RelationalTypeMapping("bigint", DbType.UInt32) },
                { typeof(ulong), new RelationalTypeMapping("numeric(20, 0)", DbType.UInt64) },
                { typeof(DateTimeOffset), new RelationalTypeMapping("datetimeoffset", DbType.DateTimeOffset) },
            };

        private readonly RelationalTypeMapping _nonKeyStringMapping
            = new RelationalTypeMapping("nvarchar(max)", DbType.String);

        // TODO: It may be possible to increase 128 to 900, at least for SQL Server
        private readonly RelationalTypeMapping _keyStringMapping
            = new RelationalSizedTypeMapping("nvarchar(128)", DbType.String, 128);

        private readonly RelationalTypeMapping _nonKeyByteArrayMapping
            = new RelationalTypeMapping("varbinary(max)", DbType.Binary);

        // TODO: It may be possible to increase 128 to 900, at least for SQL Server
        private readonly RelationalTypeMapping _keyByteArrayMapping
            = new RelationalSizedTypeMapping("varbinary(128)", DbType.Binary, 128);

        public override RelationalTypeMapping GetTypeMapping(
            string specifiedType, string storageName, Type propertyType, bool isKey, bool isConcurrencyToken)
        {
            RelationalTypeMapping mapping;
            if (_simpleMappings.TryGetValue(propertyType, out mapping))
            {
                return mapping;
            }

            if (propertyType == typeof(string))
            {
                if (isKey)
                {
                    return _keyStringMapping;
                }
                return _nonKeyStringMapping;
            }

            if (propertyType == typeof(byte[]))
            {
                if (isKey)
                {
                    return _keyByteArrayMapping;
                }

                if (!isConcurrencyToken)
                {
                    return _nonKeyByteArrayMapping;
                }
            }

            return base.GetTypeMapping(specifiedType, storageName, propertyType, isKey, isConcurrencyToken);
        }
    }
}
