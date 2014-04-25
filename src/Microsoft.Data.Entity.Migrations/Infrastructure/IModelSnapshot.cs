// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.Data.Entity.Metadata;

namespace Microsoft.Data.Entity.Migrations.Infrastructure
{
    public interface IModelSnapshot
    {
        IModel GetModel();
    }
}
