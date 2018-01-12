﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Custom;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Reusable.Converters;
using Reusable.Data;
using Reusable.Exceptionize;

namespace Reusable
{
    public static class CsvConverter
    {
        /// <summary>
        /// Creates a data-table from rows and converts columns to target types.
        /// The first row must be a header.
        /// In order to use it with SqlBulkInsert columns must be ordered.
        /// </summary>
        /// <returns></returns>
        [NotNull, ContractAnnotation("rows: null => halt")]
        public static DataTable ToDataTable(
            [NotNull, ItemNotNull] this IEnumerable<IEnumerable<string>> rows,
            [NotNull] IEnumerable<(SoftString Name, Type Type)> columns,
            [NotNull] ITypeConverter converter)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            if (columns == null) throw new ArgumentNullException(nameof(columns));
            if (converter == null) throw new ArgumentNullException(nameof(converter));

            using (var enumerator = rows.GetEnumerator())
            {
                var dataTable = default(DataTable);
                while (enumerator.MoveNext())
                {
                    if (dataTable is default)
                    {
                        dataTable = CreateDataTable(enumerator.Current, columns);
                    }
                    else
                    {
                        var values = new object[dataTable.Columns.Count];
                        var columnTypes = dataTable.Columns.Cast<DataColumn>().Select(dc => dc.DataType);
                        foreach (var (value, type, ordinal) in enumerator.Current.Zip(columnTypes, (value, type) => (value, type)).Select((zip, ordinal) => (zip.value, zip.type, ordinal)))
                        {
                            values[ordinal] = converter.Convert(value, type);
                        }

                        dataTable.AddRow(values);
                    }
                }
                return dataTable ?? new DataTable();
            }
        }

        private static DataTable CreateDataTable(IEnumerable<string> header, IEnumerable<(SoftString Name, Type Type)> columns)
        {
            var dataTable = new DataTable();

            foreach (var name in header)
            {
                dataTable.AddColumn(name, c => c.DataType = columns.Single(column => column.Name.Equals(name)).Type);
            }

            return dataTable;
        }

        // There might be fewer columns in a csv then the target table has so we need to calculate their offsets.
        // <csv-column-ordinal, sql-column-ordinal>
        private static IDictionary<int, int> CreateColumnMap(IEnumerable<string> csvNames, IEnumerable<string> sqlNames)
        {
            var csvMap = csvNames.Select((name, ordinal) => (name, ordinal)).ToList();
            var sqlMap = sqlNames.Select((name, ordinal) => (name, ordinal)).ToList();
            return
                csvMap
                    .ToDictionary(
                        x => x.ordinal,
                        x => sqlMap.Single(y => y.name.Equals(x.name, StringComparison.OrdinalIgnoreCase)).ordinal);
        }
    }
}