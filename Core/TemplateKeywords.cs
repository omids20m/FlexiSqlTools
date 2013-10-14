using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlexiSqlTools.Core;
using System.Collections.Generic;

namespace FlexiSqlTools.Core
{
    public static class TemplateKeywords
    {
        public const string DatabaseName = "|dbName|";
        public const string SchemaName   = "|schemaName|";
        public const string TableName    = "|tableName|";

        public const string NormalizeNationalTexts = "|normalizeNationalTexts|";

        public const string InputParams      = "|inputParams|";
        public const string UpdateParams     = "|updateParams|";
        public const string UpdateColumnList = "|updateColumnList|";

        public const string InsertColumnList = "|insertColumnList|";
        public const string InsertValueList  = "|insertValueList|";
        public const string WhereStatement   = "|whereStatement|";
    }
}