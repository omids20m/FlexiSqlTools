using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlexiSqlTools.Core;
using System.Collections.Generic;

namespace FlexiSqlTools.Core
{
    public static class TemplateExtensions
    {
        public static IEnumerable<vw_SPGenenerator> GetInputParams(this IEnumerable<vw_SPGenenerator> allColumnsOfTable)
        {
            return allColumnsOfTable.Where(x => x.COLUMN_NAME.ToLower() != "lastmodificationtime");
        }
        
        public static IEnumerable<vw_SPGenenerator> GetTextColumns(this IEnumerable<vw_SPGenenerator> allColumnsOfTable)
        {
            return allColumnsOfTable.Where(
                p => (
                         (p.DATA_TYPE.ToLower() == "varchar") ||
                         (p.DATA_TYPE.ToLower() == "nvarchar") ||
                         (p.DATA_TYPE.ToLower() == "text") ||
                         (p.DATA_TYPE.ToLower() == "ntext")
                     )
                );
        }
        
        public static IEnumerable<vw_SPGenenerator> GetNonTextualColumns(this IEnumerable<vw_SPGenenerator> allColumnsOfTable)
        {
            return allColumnsOfTable.Where(
                p => (
                         (p.DATA_TYPE.ToLower() != "varchar") &&
                         (p.DATA_TYPE.ToLower() != "nvarchar") &&
                         (p.DATA_TYPE.ToLower() != "text") &&
                         (p.DATA_TYPE.ToLower() != "ntext")
                     )
                );
        }

        public static IEnumerable<vw_SPGenenerator> GetNationalTextColumns(this IEnumerable<vw_SPGenenerator> allColumnsOfTable)
        {
            return allColumnsOfTable.Where(
                p => (
                         (p.DATA_TYPE.ToLower() == "nvarchar") ||
                         (p.DATA_TYPE.ToLower() == "ntext")
                     )
                );
        }

        public static IEnumerable<vw_SPGenenerator> GetNoIdentityColumns(this IEnumerable<vw_SPGenenerator> allColumnsOfTable)
        {
            return allColumnsOfTable.Where(x => x.IsIdentity != 1);
        }

        public static StringBuilder GetUpdateColumnListString(this IEnumerable<vw_SPGenenerator> allColumnsOfTable)
        {
            StringBuilder builder = new StringBuilder();
            int i = 0;
            foreach (var row in allColumnsOfTable)
            {
                if (row.IsIdentity == 1) continue;

                if (i > 0)
                    builder.AppendFormat(", {0}" +
                                     "		", Environment.NewLine);
                builder.Append(row.COLUMN_NAME);
                builder.Append(" = ");
                builder.AppendFormat("{0}", (row.COLUMN_NAME.ToLower() == "lastmodificationtime") ? "GetDate()" : "@" + row.COLUMN_NAME);
                i++;
            }
            return builder;
        }

        public static IEnumerable<vw_SPGenenerator> GetPrimaryKeys(this IEnumerable<vw_SPGenenerator> allColumnsOfTable)
        {
            return allColumnsOfTable.Where(p => p.Constraint_Type == "PK").ToList();
        }

        public static string GetTableSchema(this IEnumerable<vw_SPGenenerator> allColumnsOfTable, string tableName)
        {
            return allColumnsOfTable.First(p => p.TABLE_NAME == tableName).TABLE_SCHEMA;
        }

        private static string GetTypeWithLength(vw_SPGenenerator row)
        {
            if (row.CHARACTER_MAXIMUM_LENGTH != null && (row.DATA_TYPE != "text") && (row.DATA_TYPE != "ntext") && (row.DATA_TYPE != "image"))
            {
                return string.Format("({0})", (row.CHARACTER_MAXIMUM_LENGTH == -1) ? "MAX" : row.CHARACTER_MAXIMUM_LENGTH.ToString());
            }
            return "";
        }
        public static StringBuilder GetUpdateParams(this IEnumerable<vw_SPGenenerator> allColumnsOfTable)
        {
            StringBuilder sss = new StringBuilder();
            int i = 0;
            foreach (var row in allColumnsOfTable.GetInputParams())
            {
                if (i > 0)
                    sss.AppendFormat(", {0}" +
                                     "	", Environment.NewLine);
                sss.AppendFormat("@{0} {1}{2}", row.COLUMN_NAME, row.DATA_TYPE, GetTypeWithLength(row));
                i++;
            }
            return sss;
        }

        public static StringBuilder NormalizeTextColumnsQuery(this IEnumerable<vw_SPGenenerator> allColumnsOfTable, string indent = "    ")
        {
            StringBuilder sss = new StringBuilder();
            var textColumns = allColumnsOfTable.GetNationalTextColumns();
            int k = 0;
            foreach (var textCol in textColumns)
            {
                if (k > 0)
                    sss.AppendFormat("{0}", Environment.NewLine);
                sss.AppendFormat(@"{0}Set @{1} = dbo.GetStandardString(@{1});", indent, textCol.COLUMN_NAME);
                k++;
            }
            return sss;
        }


        public static bool HasIdentity(this IEnumerable<vw_SPGenenerator> allColumnsOfTable)
        {
            bool hasIdentity = false;
            if (allColumnsOfTable.Where(x => (x.IsIdentity.HasValue && x.IsIdentity == 1)).Count() > 0)
            {
                hasIdentity = true;
            }
            return hasIdentity;
        }

        public static StringBuilder GetInsertColumnList(this IEnumerable<vw_SPGenenerator> allColumnsOfTable)
        {
            var sss = new StringBuilder();
            int i = 0;
            foreach (var row in allColumnsOfTable.Where(x => x.COLUMN_NAME.ToLower() != "lastmodificationtime"))
            {
                if (row.IsIdentity == 1) continue;

                if (i > 0) sss.Append(", ");
                sss.Append(row.COLUMN_NAME);
                i++;
            }
            return sss;
        }
        
        public static StringBuilder GetInsertValueList(this IEnumerable<vw_SPGenenerator> allColumnsOfTable)
        {
            var sss = new StringBuilder();
            var i = 0;
            foreach (var row in allColumnsOfTable.Where(x => x.COLUMN_NAME.ToLower() != "lastmodificationtime"))
            {
                if (row.IsIdentity == 1) continue;

                if (i > 0) sss.Append(@", ");
                sss.Append("@");
                sss.Append(row.COLUMN_NAME);
                i++;
            }
            return sss;
        }

        public static StringBuilder GetWhereStatement(this IEnumerable<vw_SPGenenerator> columns)
        {
            var sss = new StringBuilder();
            var i = 0;
            foreach (var PKRow in columns)
            {
                if (i > 0)
                    sss.AppendFormat(" and {0}", Environment.NewLine);
                sss.AppendFormat(@"{0} = @{0}", PKRow.COLUMN_NAME);
                i++;
            }
            return sss;
        }
    }
}