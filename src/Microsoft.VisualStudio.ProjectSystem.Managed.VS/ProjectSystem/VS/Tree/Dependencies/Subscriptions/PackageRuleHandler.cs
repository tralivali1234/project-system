﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract,
            typeof(ICrossTargetRuleHandler<DependenciesRuleChangeContext>))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class PackageRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "NuGetDependency";

        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.NuGetGrey,
            expandedIcon: ManagedImageMonikers.NuGetGrey,
            unresolvedIcon: ManagedImageMonikers.NuGetGreyWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.NuGetGreyWarning);

        [ImportingConstructor]
        public PackageRuleHandler(ITargetFrameworkProvider targetFrameworkProvider)
        {
            TargetFrameworkProvider = targetFrameworkProvider;
        }

        private ITargetFrameworkProvider TargetFrameworkProvider { get; }

        protected override string UnresolvedRuleName { get; } = PackageReference.SchemaName;
        protected override string ResolvedRuleName { get; } = ResolvedPackageReference.SchemaName;
        public override string ProviderType { get; } = ProviderTypeString;

        public override ImageMoniker GetImplicitIcon()
        {
            return ManagedImageMonikers.NuGetGreyPrivate;
        }

        public override Task HandleAsync(
            IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectCatalogSnapshot, IProjectCapabilitiesSnapshot>> e,
            IImmutableDictionary<string, IProjectChangeDescription> projectChanges,
            ITargetedProjectContext context,
            bool isActiveContext,
            DependenciesRuleChangeContext ruleChangeContext)
        {
            if (projectChanges.TryGetValue(UnresolvedRuleName, out IProjectChangeDescription unresolvedChanges)
                && unresolvedChanges.Difference.AnyChanges)
            {
                HandleChangesForRule(
                    unresolvedChanges,
                    ruleChangeContext,
                    context.TargetFramework,
                    resolved: false);
            }

            var caseInsensitiveUnresolvedChanges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            caseInsensitiveUnresolvedChanges.AddRange(unresolvedChanges.After.Items.Keys);

            if (projectChanges.TryGetValue(ResolvedRuleName, out IProjectChangeDescription resolvedChanges)
                && resolvedChanges.Difference.AnyChanges)
            {
                HandleChangesForRule(
                    resolvedChanges,
                    ruleChangeContext,
                    context.TargetFramework,
                    resolved: true,
                    unresolvedChanges: caseInsensitiveUnresolvedChanges);
            }

            return Task.CompletedTask;
        }

        private void HandleChangesForRule(
            IProjectChangeDescription projectChange,
            DependenciesRuleChangeContext ruleChangeContext,
            ITargetFramework targetFramework,
            bool resolved,
            HashSet<string> unresolvedChanges = null)
        {
            Requires.NotNull(targetFramework, nameof(targetFramework));

            if (targetFramework == null)
            {
                return;
            }

            foreach (string removedItem in projectChange.Difference.RemovedItems)
            {
                IImmutableDictionary<string, string> properties = GetProjectItemProperties(projectChange.Before, removedItem);
                IDependencyModel model = GetDependencyModel(removedItem, resolved,
                                            properties, projectChange, unresolvedChanges, targetFramework);
                if (model == null)
                {
                    continue;
                }

                ruleChangeContext.IncludeRemovedChange(targetFramework, model);
            }

            foreach (string changedItem in projectChange.Difference.ChangedItems)
            {
                IImmutableDictionary<string, string> properties = GetProjectItemProperties(projectChange.After, changedItem);
                IDependencyModel model = GetDependencyModel(changedItem, resolved,
                                            properties, projectChange, unresolvedChanges, targetFramework);
                if (model == null)
                {
                    continue;
                }

                ruleChangeContext.IncludeRemovedChange(targetFramework, model);
                ruleChangeContext.IncludeAddedChange(targetFramework, model);
            }

            foreach (string addedItem in projectChange.Difference.AddedItems)
            {
                IImmutableDictionary<string, string> properties = GetProjectItemProperties(projectChange.After, addedItem);
                IDependencyModel model = GetDependencyModel(addedItem, resolved,
                                            properties, projectChange, unresolvedChanges, targetFramework);
                if (model == null)
                {
                    continue;
                }

                ruleChangeContext.IncludeAddedChange(targetFramework, model);
            }
        }

        private IDependencyModel GetDependencyModel(
            string itemSpec,
            bool resolved,
            IImmutableDictionary<string, string> properties,
            IProjectChangeDescription projectChange,
            HashSet<string> unresolvedChanges,
            ITargetFramework targetFramework)
        {
            PackageDependencyMetadata metadata;
            bool isTopLevel = true;
            bool isTarget = false;
            if (resolved)
            {
                metadata = new PackageDependencyMetadata(itemSpec, properties);
                isTopLevel = metadata.IsImplicitlyDefined
                             || (metadata.DependencyType == DependencyType.Package
                                 && unresolvedChanges != null
                                 && unresolvedChanges.Contains(metadata.Name));
                isTarget = metadata.IsTarget;
                ITargetFramework packageTargetFramework = TargetFrameworkProvider.GetTargetFramework(metadata.Target);
                if (!(packageTargetFramework?.Equals(targetFramework) == true))
                {
                    return null;
                }
            }
            else
            {
                metadata = CreateUnresolvedMetadata(itemSpec, properties);
            }

            if (isTarget)
            {
                return null;
            }

            string originalItemSpec = itemSpec;
            if (resolved && isTopLevel)
            {
                originalItemSpec = metadata.Name;
            }

            IDependencyModel dependencyModel = null;
            switch (metadata.DependencyType)
            {
                case DependencyType.Package:
                    dependencyModel = new PackageDependencyModel(
                        ProviderType,
                        itemSpec,
                        originalItemSpec,
                        metadata.Name,
                        DependencyTreeFlags.NuGetSubTreeNodeFlags,
                        metadata.Version,
                        resolved,
                        metadata.IsImplicitlyDefined,
                        isTopLevel,
                        !metadata.IsImplicitlyDefined /*visible*/,
                        properties,
                        metadata.DependenciesItemSpecs);
                    break;
                case DependencyType.Assembly:
                case DependencyType.FrameworkAssembly:
                    dependencyModel = new PackageAssemblyDependencyModel(
                        ProviderType,
                        itemSpec,
                        originalItemSpec,
                        metadata.Name,
                        DependencyTreeFlags.NuGetSubTreeNodeFlags,
                        resolved,
                        properties,
                        metadata.DependenciesItemSpecs);
                    break;
                case DependencyType.AnalyzerAssembly:
                    dependencyModel = new PackageAnalyzerAssemblyDependencyModel(
                        ProviderType,
                        itemSpec,
                        originalItemSpec,
                        metadata.Name,
                        DependencyTreeFlags.NuGetSubTreeNodeFlags,
                        resolved,
                        properties,
                        metadata.DependenciesItemSpecs);
                    break;
                case DependencyType.Diagnostic:
                    dependencyModel = new DiagnosticDependencyModel(
                        ProviderType,
                        itemSpec,
                        metadata.Severity,
                        metadata.DiagnosticCode,
                        metadata.Name,
                        DependencyTreeFlags.NuGetSubTreeNodeFlags,
                        isVisible: true,
                        properties: properties);
                    break;
                default:
                    dependencyModel = new PackageUnknownDependencyModel(
                        ProviderType,
                        itemSpec,
                        originalItemSpec,
                        metadata.Name,
                        DependencyTreeFlags.NuGetSubTreeNodeFlags,
                        resolved,
                        properties,
                        metadata.DependenciesItemSpecs);
                    break;
            }

            return dependencyModel;
        }

        public override IDependencyModel CreateRootDependencyNode()
        {
            return new SubTreeRootDependencyModel(
                ProviderType,
                VSResources.NuGetPackagesNodeName,
                s_iconSet,
                DependencyTreeFlags.NuGetSubTreeRootNodeFlags);
        }

        private static PackageDependencyMetadata CreateUnresolvedMetadata(string itemSpec,
                                                            IImmutableDictionary<string, string> properties)
        {
            // add this properties here since unresolved PackageReferences don't have it
            properties = properties.SetItem(ProjectItemMetadata.Resolved, "false");
            properties = properties.SetItem(ProjectItemMetadata.Type, DependencyType.Package.ToString());

            return new PackageDependencyMetadata(itemSpec, properties);
        }

        protected class PackageDependencyMetadata
        {
            public PackageDependencyMetadata(string itemSpec, IImmutableDictionary<string, string> properties)
            {
                Requires.NotNull(itemSpec, nameof(itemSpec));

                ItemSpec = itemSpec;
                Target = GetTargetFromDependencyId(ItemSpec);

                SetProperties(properties);
            }

            public string Name { get; private set; }
            public string Version { get; private set; }
            public DependencyType DependencyType { get; private set; }
            public string Path { get; private set; }
            public bool Resolved { get; private set; }
            public string ItemSpec { get; set; }
            public string Target { get; }
            public bool IsTarget
            {
                get
                {
                    return !ItemSpec.Contains("/");
                }
            }

            public bool IsImplicitlyDefined { get; private set; }

            public IImmutableDictionary<string, string> Properties { get; set; }

            public HashSet<string> DependenciesItemSpecs { get; private set; }

            public DiagnosticMessageSeverity Severity { get; private set; }
            public string DiagnosticCode { get; private set; }

            public void SetProperties(IImmutableDictionary<string, string> properties)
            {
                Requires.NotNull(properties, nameof(properties));
                Properties = properties;

                DependencyType = GetEnumMetadata<DependencyType>(ProjectItemMetadata.Type) ?? DependencyType.Unknown;
                Name = GetStringMetadata(ProjectItemMetadata.Name);
                if (string.IsNullOrEmpty(Name))
                {
                    Name = ItemSpec;
                }

                Version = GetStringMetadata(ProjectItemMetadata.Version);
                Path = GetStringMetadata(ProjectItemMetadata.Path);
                Resolved = GetBoolMetadata(ProjectItemMetadata.Resolved) ?? true;
                IsImplicitlyDefined = GetBoolMetadata(ProjectItemMetadata.IsImplicitlyDefined) ?? false;

                var dependenciesHashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (properties.ContainsKey(ProjectItemMetadata.Dependencies)
                    && properties[ProjectItemMetadata.Dependencies] != null)
                {
                    string[] dependencyIds = properties[ProjectItemMetadata.Dependencies]
                        .Split(Delimiter.Semicolon, StringSplitOptions.RemoveEmptyEntries);
                    // store only unique dependency IDs
                    foreach (string dependencyId in dependencyIds)
                    {
                        dependenciesHashSet.Add($"{Target}/{dependencyId}");
                    }
                }

                DependenciesItemSpecs = dependenciesHashSet;

                if (DependencyType == DependencyType.Diagnostic)
                {
                    Severity = GetEnumMetadata<DiagnosticMessageSeverity>(ProjectItemMetadata.Severity) ?? DiagnosticMessageSeverity.Info;
                    DiagnosticCode = GetStringMetadata(ProjectItemMetadata.DiagnosticCode);
                }
            }

            private string GetStringMetadata(string metadataName)
            {
                if (Properties.TryGetValue(metadataName, out string value))
                {
                    return value;
                }

                return string.Empty;
            }

            private T? GetEnumMetadata<T>(string metadataName) where T : struct
            {
                string enumString = GetStringMetadata(metadataName);
                return Enum.TryParse(enumString, ignoreCase: true, out T enumValue) ? enumValue : (T?)null;
            }

            private bool? GetBoolMetadata(string metadataName)
            {
                string boolString = GetStringMetadata(metadataName);
                return bool.TryParse(boolString, out bool boolValue) ? boolValue : (bool?)null;
            }

            public static string GetTargetFromDependencyId(string dependencyId)
            {
                string[] idParts = dependencyId.Split(Delimiter.ForwardSlash, StringSplitOptions.RemoveEmptyEntries);
                Requires.NotNull(idParts, nameof(idParts));
                if (idParts.Length == 0)
                {
                    // should never happen
                    throw new ArgumentException(nameof(idParts));
                }

                return idParts[0];
            }
        }

        protected enum DependencyType
        {
            Unknown,
            Target,
            Diagnostic,
            Package,
            Assembly,
            FrameworkAssembly,
            AnalyzerAssembly
        }
    }
}
