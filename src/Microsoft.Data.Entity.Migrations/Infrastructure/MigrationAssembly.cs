// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations.Utilities;

namespace Microsoft.Data.Entity.Migrations.Infrastructure
{
    public class MigrationAssembly
    {
        private readonly Assembly _assembly;
        private readonly string _namespace;

        private IReadOnlyList<IMigrationMetadata> _migrations;
        private IModel _model;

        public MigrationAssembly([NotNull] Assembly assembly, [NotNull] string @namespace)
        {
            Check.NotNull(assembly, "assembly");
            Check.NotEmpty(@namespace, "namespace");

            _assembly = assembly;
            _namespace = @namespace;
        }

        public virtual Assembly Assembly
        {
            get { return _assembly; }
        }

        public virtual string Namespace
        {
            get { return _namespace; }
        }

        public virtual IReadOnlyList<IMigrationMetadata> Migrations
        {
            get { return _migrations ?? (_migrations = LoadMigrations()); }
        }

        public virtual IModel Model
        {
            get { return _model ?? (_model = LoadModel()); }
        }

        protected virtual IReadOnlyList<IMigrationMetadata> LoadMigrations()
        {
            // TODO: Consider adding reference to MigrationIdentifier and validate MigrationId.

            return Assembly.GetAccessibleTypes()
                .Where(t => t.GetTypeInfo().IsSubclassOf(typeof(Migration))                           
                            && t.GetPublicConstructor() != null
                            && !t.GetTypeInfo().IsAbstract
                            && !t.GetTypeInfo().IsGenericType
                            && t.Namespace == Namespace)
                .Select(t => (IMigrationMetadata)Activator.CreateInstance(t))
                .OrderBy(m => m.Timestamp)
                .ToArray();
        }

        protected virtual IModel LoadModel()
        {
            var modelSnapshotType = Assembly.GetAccessibleTypes().SingleOrDefault(
                t => t.GetTypeInfo().IsSubclassOf(typeof(IModelSnapshot))
                     && t.GetPublicConstructor() != null
                     && !t.GetTypeInfo().IsAbstract
                     && !t.GetTypeInfo().IsGenericType
                     && t.Namespace == Namespace);

            return modelSnapshotType != null
                ? ((IModelSnapshot)Activator.CreateInstance(modelSnapshotType)).GetModel()
                : null;
        }
    }
}
