﻿using Dapper;
using Postulate.Lite.Core;
using Postulate.Lite.Core.Extensions;
using Postulate.Lite.Core.Metadata;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Postulate.Lite.SqlServer
{
	public partial class SqlServerProvider<TKey> : CommandProvider<TKey>
	{
		public override string CommentPrefix => "--";

		public override string CreateTableCommand(Type modelType)
		{
			var columns = MappedColumns(modelType);
			var pkColumns = GetPrimaryKeyColumns(modelType, columns, out bool identityIsPrimaryKey);
			var identityName = modelType.GetIdentityName();

			string constraintName = TableName(modelType).Replace(".", "_");

			List<string> members = new List<string>();
			members.AddRange(columns.Select(pi => SqlColumnSyntax(pi, (identityName.Equals(pi.Name)))));
			members.Add(PrimaryKeySyntax(constraintName, pkColumns));
			if (!identityIsPrimaryKey) members.Add(UniqueIdSyntax(constraintName, modelType.GetIdentityProperty()));

			return
				$"CREATE TABLE {ApplyDelimiter(TableName(modelType))} (" +
					string.Join(",\r\n\t", members) +
				")";
		}

		public override string AddForeignKeyCommand(PropertyInfo propertyInfo)
		{
			throw new NotImplementedException();
		}

		public override string AddColumnCommand(PropertyInfo propertyInfo)
		{
			throw new NotImplementedException();
		}

		public override string AlterColumnCommand(PropertyInfo propertyInfo)
		{
			throw new NotImplementedException();
		}

		public override string DropColumnCommand(ColumnInfo columnInfo)
		{
			throw new NotImplementedException();
		}

		public override string DropForeignKeyCommand(ForeignKeyInfo foreignKeyInfo)
		{
			return $"ALTER TABLE [{foreignKeyInfo.Child.Schema}].[{foreignKeyInfo.Child.TableName}] DROP CONSTRAINT [{foreignKeyInfo.ConstraintName}]";
		}

		public override string DropTableCommand(TableInfo tableInfo)
		{
			throw new NotImplementedException();
		}

		public override string DropPrimaryKeyCommand(Type modelType)
		{
			throw new NotImplementedException();
		}

		public override string AddPrimaryKeyCommand(Type modelType)
		{
			throw new NotImplementedException();
		}

		public override string AddForeignKeyCommand(ForeignKeyInfo foreignkeyInfo)
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<ForeignKeyInfo> GetDependentForeignKeys(IDbConnection connection, TableInfo tableInfo)
		{
			return connection.Query<ForeignKeyData>(
				@"SELECT
                    [fk].[name] AS [ConstraintName],
                    SCHEMA_NAME([parent].[schema_id]) AS [ReferencedSchema],
                    [parent].[name] AS [ReferencedTable],
                    [refdcol].[name] AS [ReferencedColumn],
                    SCHEMA_NAME([child].[schema_id]) AS [ReferencingSchema],
                    [child].[name] AS [ReferencingTable],
                    [rfincol].[name] AS [ReferencingColumn],
					CONVERT(bit, CASE [fk].[delete_referential_action] WHEN 1 THEN 1 ELSE 0 END) AS [CascadeDelete]
                FROM
                    [sys].[foreign_keys] [fk] INNER JOIN [sys].[tables] [child] ON [fk].[parent_object_id]=[child].[object_id]
                    INNER JOIN [sys].[tables] [parent] ON [fk].[referenced_object_id]=[parent].[object_id]
                    INNER JOIN [sys].[foreign_key_columns] [fkcol] ON
                        [fk].[parent_object_id]=[fkcol].[parent_object_id] AND
                        [fk].[object_id]=[fkcol].[constraint_object_id]
                    INNER JOIN [sys].[columns] [refdcol] ON
                        [fkcol].[referenced_column_id]=[refdcol].[column_id] AND
                        [fkcol].[referenced_object_id]=[refdcol].[object_id]
                    INNER JOIN [sys].[columns] [rfincol] ON
                        [fkcol].[parent_column_id]=[rfincol].[column_id] AND
                        [fkcol].[parent_object_id]=[rfincol].[object_id]
				WHERE
                    [fk].[referenced_object_id]=OBJECT_ID(@schema+'.' + @table)", new { schema = tableInfo.Schema, table = tableInfo.Name })
			.Select(fk => new ForeignKeyInfo()
			{
				ConstraintName = fk.ConstraintName,
				Child = new ColumnInfo() { Schema = fk.ReferencingSchema, TableName = fk.ReferencingTable, ColumnName = fk.ReferencingColumn },
				Parent = new ColumnInfo() { Schema = fk.ReferencedSchema, TableName = fk.ReferencedTable, ColumnName = fk.ReferencedColumn },
				CascadeDelete = fk.CascadeDelete
			});
		}
	}
}