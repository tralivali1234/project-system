﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio
{
    /// <summary>
    ///     Contains commonly used <see cref="IEqualityComparer{String}"/> instances.
    /// </summary>
    internal static class StringComparers
    {
        public static IEqualityComparer<string> WorkspaceProjectContextIds
        {
            get { return StringComparer.Ordinal; }
        }

        public static IEqualityComparer<string> Paths
        {
            get { return StringComparer.OrdinalIgnoreCase; }
        }

        public static IEqualityComparer<string> PropertyNames
        {
            get { return StringComparer.OrdinalIgnoreCase; }
        }

        public static IEqualityComparer<string> PropertyValues
        {
            get { return StringComparer.OrdinalIgnoreCase; }
        }

        public static IEqualityComparer<string> RuleNames
        {
            get { return StringComparer.OrdinalIgnoreCase; }
        }

        public static IEqualityComparer<string> ConfigurationDimensionNames
        {
            get { return StringComparer.Ordinal; }
        }

        public static IEqualityComparer<string> DependencyProviderTypes
        {
            get { return StringComparer.OrdinalIgnoreCase; }
        }

        public static IEqualityComparer<string> ItemTypes
        {
            get { return StringComparer.OrdinalIgnoreCase; }
        }
    }
}
