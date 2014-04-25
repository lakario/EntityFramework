// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations.Model;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Migrations.Infrastructure
{
    public interface IMigrationMetadata
    {
        string Name { get; }
        string Timestamp { get; }
        IModel SourceModel { get; }
        IModel TargetModel { get; }
        IReadOnlyList<MigrationOperation> UpgradeOperations { get; }
        IReadOnlyList<MigrationOperation> DowngradeOperations { get; }
    }
}
