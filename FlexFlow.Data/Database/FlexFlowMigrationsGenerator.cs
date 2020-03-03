using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System;
using System.Collections.Generic;

namespace FlexFlow.Data.Database
{
    /// <summary>
    /// Adds a pragma that disables warning CS1591 ("Missing XML comment for publicly visible type or member") to EF Migrations as they are created.
    /// </summary>
    public class FlexFlowMigrationsGenerator : CSharpMigrationsGenerator
    {
        private const string _pragmaWarningDisable = @"#pragma warning disable 1591";
        private const string _pragmaWarningRestore = @"#pragma warning restore 1591";

        /// <inheritdoc />
        public FlexFlowMigrationsGenerator(
            MigrationsCodeGeneratorDependencies dependencies, CSharpMigrationsGeneratorDependencies csharpDependencies)
            : base(dependencies, csharpDependencies)
        { }

        /// <inheritdoc />
        public override string GenerateMigration(string migrationNamespace, string migrationName,
                                                 IReadOnlyList<MigrationOperation> upOperations,
                                                 IReadOnlyList<MigrationOperation> downOperations)
        {
            return _pragmaWarningDisable
                   + Environment.NewLine
                   + base.GenerateMigration(migrationNamespace, migrationName, upOperations, downOperations)
                   + Environment.NewLine
                   + _pragmaWarningRestore
                   + Environment.NewLine;
        }

        /// <inheritdoc />
        public override string GenerateMetadata(string migrationNamespace, Type contextType, string migrationName,
                                                string migrationId, IModel targetModel)
            => _pragmaWarningDisable
               + Environment.NewLine
               + base.GenerateMetadata(migrationNamespace, contextType, migrationName, migrationId, targetModel)
               + Environment.NewLine
               + _pragmaWarningRestore
               + Environment.NewLine;
    }
}
