﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class ProjectDependencyModel : DependencyModel
    {
        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: KnownMonikers.Application,
            expandedIcon: KnownMonikers.Application,
            unresolvedIcon: ManagedImageMonikers.ApplicationWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.ApplicationWarning);

        private static readonly DependencyIconSet s_implicitIconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.ApplicationPrivate,
            expandedIcon: ManagedImageMonikers.ApplicationPrivate,
            unresolvedIcon: ManagedImageMonikers.ApplicationWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.ApplicationWarning);

        public ProjectDependencyModel(
            string providerType,
            string path,
            string originalItemSpec,
            ProjectTreeFlags flags,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
            : base(providerType, path, originalItemSpec, flags, resolved, isImplicit, properties)
        {
            Flags = Flags.Union(DependencyTreeFlags.SupportsHierarchy);

            if (Resolved)
            {
                SchemaName = ResolvedProjectReference.SchemaName;
            }
            else
            {
                SchemaName = ProjectReference.SchemaName;
            }

            Caption = System.IO.Path.GetFileNameWithoutExtension(Name);
            SchemaItemType = ProjectReference.PrimaryDataSourceItemType;
            Priority = Dependency.ProjectNodePriority;
            IconSet = isImplicit ? s_implicitIconSet : s_iconSet;
        }
    }
}
