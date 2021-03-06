﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class SubTreeRootDependencyModel : DependencyModel
    {
        public SubTreeRootDependencyModel(
            string providerType,
            string name,
            DependencyIconSet iconSet,
            ProjectTreeFlags flags)
            : base(providerType, name, name, flags, true, false, null)
        {
            IconSet = iconSet;
            Flags = flags.Union(DependencyTreeFlags.DependencyFlags)
                         .Union(DependencyTreeFlags.SubTreeRootNodeFlags)
                         .Except(DependencyTreeFlags.SupportsRuleProperties)
                         .Except(DependencyTreeFlags.SupportsRemove);
        }
    }
}
