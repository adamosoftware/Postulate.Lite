﻿using System;

namespace Postulate.Core.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class IdentityAttribute : Attribute
	{
		public IdentityAttribute(string columnName)
		{
			ColumnName = columnName;
		}

		public string ColumnName { get; }
	}
}