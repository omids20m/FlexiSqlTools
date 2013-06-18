using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlexiSqlTools.Core;

namespace FlexiSqlTools.Core
{
    public class DataBaseHelper
    {
        #region Private Methods

        private static void AppendUseDBExpression(string dbName, StoredProcedureConfig spConfig, StringBuilder stringBuilder)
        {
            if (spConfig.UseDataBase)
            {
                stringBuilder.AppendFormat("Use {0};\r\nGo\r\n", dbName);
            }
        }

        #endregion

        #region Public Static Methods

        public static string GetCommentForSp()
        {
            return string.Format("{1}" +
                                 "-- ============================================={1}" +
                                 "-- Author:          Omid Shariati{1}" +
                                 "-- Author email:    [OmidS20m] at [gmail] . [com]{1}" +
                                 "-- Create date:     {0}{1}" +
                                 "-- Description:     email me with \"FlexSqlTools\" subject{1}" +
                                 "-- ============================================={1}",
                                 DateTime.Now, Environment.NewLine);
        }

        public static string GenerateInsertSp(string dbName, IEnumerable<vw_SPGenenerator> allColumnsOfTable, StoredProcedureConfig spConfig)
        {
            bool hasIdentity = HasIdentity(allColumnsOfTable);
            var selectTableSchema = GetTableSchema(allColumnsOfTable, spConfig);

            StringBuilder sss = new StringBuilder(DataBaseHelper.GetCommentForSp());
            AppendUseDBExpression(dbName, spConfig, sss);

            sss.AppendFormat("Create Procedure {0} {1}" +
                             "	",
                             spConfig.Name, Environment.NewLine);

            AppendParameterDeclarationFor(allColumnsOfTable.Where(x => x.IsIdentity != 1), sss);

            if (spConfig.EnableEncryption)
                sss.AppendFormat(" {0}" +
                                 "With Encryption ", Environment.NewLine);
            if (hasIdentity == true)
            {
                #region if (hasIdentity == true)

                sss.AppendFormat(@",{0}" +
                                 "	@InsertedId int output{0}" +
                                 "", Environment.NewLine);

                #endregion
            }
            sss.AppendFormat("{0}" +
                             "As{0}" +
                             "Begin   {0}" +
                             "    Set NoCount On;{0}" +
                             "", Environment.NewLine);
            AppendNormalizeQueryForTextColumns(allColumnsOfTable, sss);

            sss.AppendFormat(
                "{3}" +
                "{3}" +
                "    Insert Into [{0}].[{1}].[{2}]{3}" +
                "		(",
                dbName, selectTableSchema, spConfig.Name.TableName, Environment.NewLine);
            int i = 0;
            foreach (var row in allColumnsOfTable.Where(x => x.COLUMN_NAME.ToLower() != "lastmodificationtime"))
            {
                if (row.IsIdentity == 1) continue;

                if (i > 0) sss.Append(", ");
                sss.Append(row.COLUMN_NAME);
                i++;
            }
            sss.AppendFormat("){0}" +
                             "	Values{0}" +
                             "		(",
                             Environment.NewLine);

            i = 0;
            foreach (var row in allColumnsOfTable.Where(x => x.COLUMN_NAME.ToLower() != "lastmodificationtime"))
            {
                if (row.IsIdentity == 1) continue;

                if (i > 0) sss.Append(@", ");
                sss.Append("@");
                sss.Append(row.COLUMN_NAME);
                i++;
            }

            if (hasIdentity == true)
            {
                sss.AppendFormat(@") {0}" +
                                 "    {0}" +
                                 "    Set @InsertedId = SCOPE_IDENTITY();{0}" +
                                 "End{0}" +
                                 "", Environment.NewLine);
            }
            else
            {
                sss.AppendFormat(@") {0}" +
                                 "    {0}" +
                                 "End{0}" +
                                 "", Environment.NewLine);
            }
            if (spConfig.UseGo)
            {
                sss.AppendLine("GO");
            }
            return sss.ToString();
        }
        
        public static string GenerateUpdateSp(string dbName, IEnumerable<vw_SPGenenerator> allColumnsOfTable, StoredProcedureConfig spConfig)
        {

            var primaryKeys = allColumnsOfTable.Where(p => p.Constraint_Type == "PK").ToList();
            var selectTableSchema = GetTableSchema(allColumnsOfTable, spConfig);
            if (!primaryKeys.Any()) 
                return "";

            StringBuilder sss = new StringBuilder(GetCommentForSp());
            AppendUseDBExpression(dbName, spConfig, sss);

            sss.AppendFormat(@"Create Procedure {0}", spConfig.Name.ToString());
            sss.AppendFormat(" {0}" +
                             "	", Environment.NewLine);

            AppendParameterDeclarationFor(allColumnsOfTable, sss);

            sss.Append(" ");

            if (spConfig.EnableEncryption)
                sss.AppendFormat("{0}" +
                                 "With Encryption ", Environment.NewLine);
            sss.AppendFormat("{0}" +
                             "AS{0}" +
                             "BEGIN{0}" +
                             "    Set NoCount On;{0}" +
                             "", Environment.NewLine);

            AppendNormalizeQueryForTextColumns(allColumnsOfTable, sss);
            sss.AppendFormat("{3}" +
                             "                {3}" +
                             "	Update [{0}].[{1}].[{2}]{3}" +
                             "	Set{3}" +
                             "		", 
                             dbName, selectTableSchema, spConfig.Name.TableName, Environment.NewLine);

            int i = 0;
            foreach (var row in allColumnsOfTable)
            {
                if (row.IsIdentity == 1) continue;

                if (i > 0)
                    sss.AppendFormat(", {0}" +
                                     "		", Environment.NewLine);
                sss.Append(row.COLUMN_NAME);
                sss.Append(" = ");
                sss.AppendFormat("{0}", (row.COLUMN_NAME.ToLower() == "lastmodificationtime") ? "GetDate()" : "@" + row.COLUMN_NAME);
                i++;
            }
            sss.AppendFormat(" {0}" +
                             "	Where{0}" +
                             "		",
                             Environment.NewLine);

            i = 0;
            foreach (var PKRow in primaryKeys)
            {
                if (i > 0) 
                    sss.AppendFormat(" and {0}", Environment.NewLine);
                sss.AppendFormat(@"{0} = @{0}", PKRow.COLUMN_NAME);
                i++;
            }

            sss.AppendFormat("{0}" +
                             "END{0}",
                             Environment.NewLine);
            if (spConfig.UseGo)
            {
                sss.AppendLine("GO");
            }
            return sss.ToString();
        }

        public static string GenerateSelectRowSp(string dbName, IEnumerable<vw_SPGenenerator> allColumnsOfTable, StoredProcedureConfig spConfig)
        {
            var primaryKeys = allColumnsOfTable.Where(p => p.Constraint_Type == "PK");
            var SelectTABLE_SCHEMA = GetTableSchema(allColumnsOfTable, spConfig);
            if (primaryKeys.Count() == 0)
                return "";

            StringBuilder sss = new StringBuilder(GetCommentForSp());
            AppendUseDBExpression(dbName, spConfig, sss);

            sss.Append("Create Procedure ");
            sss.Append(spConfig.Name.ToString());
            sss.AppendFormat(" {0}" +
                             "	", Environment.NewLine);

            int i = 0;
            foreach (var PKRow in primaryKeys)
            {
                if (i > 0) sss.Append(", ");
                sss.AppendFormat("@{0} {1}", PKRow.COLUMN_NAME, PKRow.DATA_TYPE);

                //stringBuilder.Append((PKRow.CHARACTER_MAXIMUM_LENGTH != null) ? "(" + PKRow.CHARACTER_MAXIMUM_LENGTH + ")" : "");
                sss.Append((PKRow.CHARACTER_MAXIMUM_LENGTH != null && (PKRow.DATA_TYPE != "ntext") &&
                            (PKRow.DATA_TYPE != "image"))
                               ? "(" + ((PKRow.CHARACTER_MAXIMUM_LENGTH == -1) ? "MAX" : PKRow.CHARACTER_MAXIMUM_LENGTH.ToString()) + ")"
                               : "");
                i++;
            }
            sss.Append(" ");

            if (spConfig.EnableEncryption)
            {
                sss.AppendFormat("{0}" +
                                 "With Encryption {0}", Environment.NewLine);
            }
            sss.AppendFormat(@"{0}" +
                             "AS{0}" +
                             "BEGIN{0}" +
                             "    Set NoCount On;{0}" +
                             "	SELECT ",
                             Environment.NewLine);

            int iii = 0;
            foreach (var row in allColumnsOfTable)
            {
                if (iii > 0)
                {
                    sss.Append(", ");
                }
                sss.Append(row.COLUMN_NAME);
                iii++;
            }
            sss.AppendFormat(@"{3}" +
                             "	FROM [{0}].[{1}].[{2}]{3}" +
                             "	WHERE{3}" +
                             "		",
                             dbName, SelectTABLE_SCHEMA, spConfig.Name.TableName, Environment.NewLine);

            i = 0;
            foreach (var PKRow in primaryKeys)
            {
                if (i > 0)
                {
                    sss.Append(" AND ");
                }
                sss.AppendFormat("{0} = @{1}", PKRow.COLUMN_NAME, PKRow.COLUMN_NAME);
                i++;
            }
            sss.Append(" ");

            sss.AppendFormat(@"{0}" +
                             "END{0}", Environment.NewLine);
            if (spConfig.UseGo)
            {
                sss.AppendLine("GO");
            }
            return sss.ToString();
        }
        
        public static string GenerateSelectAllSp(string dbName, IEnumerable<vw_SPGenenerator> allColumnsOfTable, StoredProcedureConfig spConfig)
        {
            var SelectTABLE_SCHEMA = GetTableSchema(allColumnsOfTable, spConfig);

            StringBuilder stringBuilder = new StringBuilder(GetCommentForSp());
            AppendUseDBExpression(dbName, spConfig, stringBuilder);

            stringBuilder.AppendFormat("Create Procedure {0}{1}" +
                                       "	",
                                       spConfig.Name, Environment.NewLine);

            if (spConfig.EnableEncryption)
            {
                stringBuilder.AppendFormat(@"{0}" +
                                           "With Encryption ", Environment.NewLine);
            }
            stringBuilder.AppendFormat(@"{0}" +
                                       "AS{0}" +
                                       "BEGIN{0}" +
                                       "    Set NoCount On;{0}" +
                                       "	SELECT ", Environment.NewLine);

            int i = 0;
            foreach (var row in allColumnsOfTable)
            {
                if (i > 0) stringBuilder.Append(", ");
                stringBuilder.Append(row.COLUMN_NAME);
                i++;
            }
            stringBuilder.AppendFormat(@"{3}" +
                                       "	FROM [{0}].[{1}].[{2}]",
                                       dbName, SelectTABLE_SCHEMA, spConfig.Name.TableName, Environment.NewLine);
            //            stringBuilder.Append(@"
            //    WHERE ");

            //            i = 0;
            //            foreach (var PKRow in primaryKeys)
            //            {
            //                if (i > 0) stringBuilder.Append(" AND ");
            //                stringBuilder.Append(PKRow.COLUMN_NAME);
            //                stringBuilder.Append(" = ");
            //                stringBuilder.Append("@");
            //                stringBuilder.Append(PKRow.COLUMN_NAME);
            //                i++;
            //            }
            //            stringBuilder.Append(" ");

            stringBuilder.AppendFormat(@"{0}" +
                                       "END{0}", Environment.NewLine);

            if (spConfig.UseGo)
            {
                stringBuilder.AppendLine("GO");
            }
            return stringBuilder.ToString();
        }
        
        public static string GenerateDeleteRowSp(string dbName, IEnumerable<vw_SPGenenerator> allColumnsOfTable, StoredProcedureConfig spConfig)
        {
            var conditionFields = allColumnsOfTable.Where(p => p.Constraint_Type == "PK");
            if (conditionFields.Count() == 0) { conditionFields = allColumnsOfTable.ToList(); }
            var SelectTABLE_SCHEMA = GetTableSchema(allColumnsOfTable, spConfig);

            StringBuilder sss = new StringBuilder(GetCommentForSp());
            AppendUseDBExpression(dbName, spConfig, sss);

            sss.AppendFormat("Create Procedure {0} {1}" +
                             "	", 
                             spConfig.Name.ToString(), Environment.NewLine);

            int i = 0;
            foreach (var PKRow in conditionFields)
            {
                if (i > 0)
                {
                    sss.Append(", ");
                }
                sss.AppendFormat("@{0} {1}", PKRow.COLUMN_NAME, PKRow.DATA_TYPE);

                //stringBuilder.Append((PKRow.CHARACTER_MAXIMUM_LENGTH != null) ? "(" + PKRow.CHARACTER_MAXIMUM_LENGTH + ")" : "");
                sss.Append((PKRow.CHARACTER_MAXIMUM_LENGTH != null && (PKRow.DATA_TYPE != "ntext") && (PKRow.DATA_TYPE != "image"))
                               ? "(" + ((PKRow.CHARACTER_MAXIMUM_LENGTH == -1) ? "MAX" : PKRow.CHARACTER_MAXIMUM_LENGTH.ToString()) + ")"
                               : "");
                i++;
            }
            sss.Append(" ");

            if (spConfig.EnableEncryption)
            {
                sss.AppendFormat(@"{0}" +
                                 "With Encryption ", Environment.NewLine);
            }
            sss.AppendFormat(@"{3}" +
                             "AS{3}" +
                             "BEGIN{3}" +
                             "    Set NoCount On;{3}" +
                             "    Begin Try{3}" +
                             "	    DELETE FROM [{0}].[{1}].[{2}]{3}" +
                             "	    WHERE ",
                             dbName, SelectTABLE_SCHEMA, spConfig.Name.TableName, Environment.NewLine);

            i = 0;
            foreach (var PKRow in conditionFields)
            {
                if (i > 0)
                {
                    sss.Append(" AND ");
                }
                sss.AppendFormat("{0} = @{1}", PKRow.COLUMN_NAME, PKRow.COLUMN_NAME);
                i++;
            }

            sss.AppendFormat(@" {0}" +
                             "	End Try{0}" +
                             "	Begin Catch{0}" +
                             "		Return ERROR_NUMBER(){0}" +
                             "	End Catch{0}" +
                             "END{0}" +
                             "", 
                             Environment.NewLine);
            if (spConfig.UseGo)
            {
                sss.AppendLine("GO");
            }
            return sss.ToString();
        }
        
        public static string GenerateSearchWithPagingAndSortingTotalCountSp(string dbName, IEnumerable<vw_SPGenenerator> allColumnsOfTable, StoredProcedureConfig spConfig)
        {
            var SelectTABLE_SCHEMA = GetTableSchema(allColumnsOfTable, spConfig);

            StringBuilder sss = new StringBuilder(GetCommentForSp());
            AppendUseDBExpression(dbName, spConfig, sss);

            var nonPrimaryKeys = allColumnsOfTable.Where(p => p.Constraint_Type != "PK");
            var textColumns = GetTextColumns(allColumnsOfTable);
            var nonTextColumns = GetNonTextualColumns(allColumnsOfTable);

            sss.Append("Create Procedure ");
            sss.Append(spConfig.Name.ToString());
            sss.AppendFormat(@"{0}" +
                             "({0}" +
                             "	", Environment.NewLine);
            AppendParameterDeclarationFor(nonPrimaryKeys.Where(x => x.COLUMN_NAME.ToLower() != "lastmodificationtime" && x.COLUMN_NAME.ToLower() != "lastmodifiedby"), sss);

            sss.AppendFormat(@", {0}" +
                             "    {0}" +
                             "	@PageIndex int = 0,{0}" +
                             "	@PageSize int = 20,{0}" +
                             "	@SortField varchar(100),{0}" +
                             "	@SortDirection varchar(5){0}" +
                             "){0}" +
                             "", Environment.NewLine);

            if (spConfig.EnableEncryption)
                sss.AppendFormat(@"{0}" +
                                 "With Encryption ", Environment.NewLine);
            sss.AppendFormat(@"{0}" +
                             "AS{0}" +
                             "BEGIN{0}" +
                             "    Set NoCount On;{0}" +
                             "        {0}" +
                             "    Declare @UpPageNumber int;{0}" +
                             "    Declare @DownPageNumber int;{0}" +
                             "    Set @UpPageNumber = @PageIndex * @PageSize ;{0}" +
                             "    Set @DownPageNumber = @UpPageNumber + @PageSize;{0}" +
                             "    Set @UpPageNumber = @UpPageNumber + 1;{0}" +
                             "        {0}" +
                             "", Environment.NewLine);

            AppendNormalizeQueryForTextColumnsForSearch(allColumnsOfTable, sss);

            sss.AppendFormat(@"{0}" +
                             "                {0}" +
                             "    Select{0}" +
                             "        AllResults.RowNumber,{0}" +
                             "        AllResults.DataCount", Environment.NewLine);
            foreach (var col in allColumnsOfTable)
            {
                sss.AppendFormat(",{0}" +
                                 "        ", Environment.NewLine);
                sss.AppendFormat(@"AllResults.{0}", col.COLUMN_NAME);
            }
            sss.AppendFormat("{0}" +
                             "    From{0}" +
                             "    (", Environment.NewLine);

            sss.AppendFormat("{0}" +
                             "        Select{0}" +
                             "            DataCount = Count(*) over(),{0}" +
                             "            ROW_NUMBER() OVER (ORDER BY ", Environment.NewLine);

            var ll = 0;
            foreach (var allCol in allColumnsOfTable)
            {
                if (ll > 0) sss.Append(@",");
                sss.AppendFormat("{4}" +
                                 "                Case When @SortField = '{3}' AND @SortDirection = 'ASC' Then [{0}].[{1}].[{2}].[{3}] End,{4}" +
                                 "                Case When @SortField = '{3}' AND @SortDirection = 'DESC' Then [{0}].[{1}].[{2}].[{3}] End DESC", 
                                 dbName, SelectTABLE_SCHEMA, spConfig.Name.TableName, allCol.COLUMN_NAME, Environment.NewLine);
                ll++;
            }
            sss.AppendFormat("{0}" +
                             "            ) as RowNumber", Environment.NewLine);

            foreach (var col in allColumnsOfTable)
            {
                sss.AppendFormat(",{4}" +
                                 "            [{0}].[{1}].[{2}].[{3}]",
                                 dbName, SelectTABLE_SCHEMA, spConfig.Name.TableName, col.COLUMN_NAME, Environment.NewLine);
            }

            sss.AppendFormat(@"{3}" +
                             "        From{3}" +
                             "            [{0}].[{1}].[{2}]{3}" +
                             "        Where{3}" +
                             "            ", 
                             dbName, SelectTABLE_SCHEMA, spConfig.Name.TableName, Environment.NewLine);

            int j = 0;
            foreach (var col in textColumns.Where(x => x.Constraint_Type != "PK"))
            {
                if (j > 0)
                    sss.AppendFormat("And {0}" +
                                     "            ", Environment.NewLine);
                sss.AppendFormat("(@{3} is null    Or    [{0}].[{1}].[{2}].[{3}] like @{3}) ", dbName, SelectTABLE_SCHEMA, spConfig.Name.TableName, col.COLUMN_NAME);
                j++;
            }

            foreach (var col in nonTextColumns.Where(x => x.Constraint_Type != "PK").Where(x => x.COLUMN_NAME.ToLower() != "lastmodificationtime" && x.COLUMN_NAME.ToLower() != "lastmodifiedby"))
            {
                if (j > 0)
                    sss.AppendFormat("And {0}" +
                                     "            ", Environment.NewLine);
                sss.AppendFormat("(@{3} is null    Or    [{0}].[{1}].[{2}].[{3}] = @{3}) ", dbName, SelectTABLE_SCHEMA, spConfig.Name.TableName, col.COLUMN_NAME);
                j++;
            }
            sss.AppendFormat("{0}" +
                             "    )AllResults{0}" +
                             "	WHERE RowNumber >= @UpPageNumber AND RowNumber <= @DownPageNumber{0}" +
                             "	ORDER BY RowNumber{0}" +
                             "End{0}" +
                             "", 
                             Environment.NewLine);
            if (spConfig.UseGo)
            {
                sss.AppendLine("GO");
            }
            return sss.ToString();
        }
        [Obsolete]
        public static string GenerateOldSearchWithPageSortTotalCountSp(string dbName, IEnumerable<vw_SPGenenerator> allColumnsOfTable, StoredProcedureConfig spConfig)
        {
            var primaryKeys = allColumnsOfTable.Where(p => p.Constraint_Type == "PK");
            var selectTableSchema = GetTableSchema(allColumnsOfTable, spConfig);

            StringBuilder sss = new StringBuilder(GetCommentForSp());
            AppendUseDBExpression(dbName, spConfig, sss);

            sss.AppendFormat("Create Procedure {0}" +
                             "( {1}" +
                             "	",
                             spConfig.Name.ToString(), Environment.NewLine);

            //int i = 0;
            foreach (var PKRow in primaryKeys)
            {
                //if (i > 0) stringBuilder.Append(", ");
                sss.AppendFormat("@{0} {1}", PKRow.COLUMN_NAME, PKRow.DATA_TYPE);

                //stringBuilder.Append((PKRow.CHARACTER_MAXIMUM_LENGTH != null) ? "(" + PKRow.CHARACTER_MAXIMUM_LENGTH + ")" : "");
                sss.Append((PKRow.CHARACTER_MAXIMUM_LENGTH != null && (PKRow.DATA_TYPE != "ntext") && (PKRow.DATA_TYPE != "image"))
                    ? "(" + ((PKRow.CHARACTER_MAXIMUM_LENGTH == -1) ? "MAX" : PKRow.CHARACTER_MAXIMUM_LENGTH.ToString()) + ")"
                    : "");
                sss.Append(", ");
                //i++;
            }
            sss.Append(" ");

            sss.AppendFormat("{0}" +
                             "	@startRowIndex		int,{0}" +
                             "	@maximumRows		int,{0}" +
                             "	@sortBy			nvarchar(30),{0}" +
                             "	@TotalCount		int OUTPUT{0}" +
                             "	){0}", 
                             Environment.NewLine);

            if (spConfig.EnableEncryption)
            {
                sss.AppendFormat("{0}" +
                                 "With Encryption ", Environment.NewLine);
            }
            sss.AppendFormat(
                @"{3}" +
                "AS{3}" +
                "BEGIN{3}" +
                "    Set NoCount On;{3}" +
                "    {3}" +
                "    DECLARE{3}" +
                "        @sqlStatement nvarchar(max),    -- SQL statement to execute{3}" +
                "        @upperBound int{3}" +
                "        {3}" +
                "    IF @startRowIndex  < 1 SET @startRowIndex = 1{3}" +
                "    IF @maximumRows < 1 SET @maximumRows = 1{3}" +
                "    {3}" +
                "    SET @upperBound = @startRowIndex + @maximumRows{3}" +
                "    {3}" +
                "    Select @TotalCount = Count(*) From [{0}].[{1}].[{2}]{3}" +
                "    {3}" +
                "    SET @sqlStatement = ' {3}" +
                "    SELECT tbl.*{3}" +
                "            FROM ({3}" +
                "                  SELECT  ROW_NUMBER() OVER(ORDER BY ' + @SortBy + ') AS rowNumber, *{3}" +
                "                  FROM    [{0}].[{1}].[{2}]{3}" +
                "                 ) AS tbl{3}" +
                "            WHERE  rowNumber >= ' + CONVERT(varchar(9), @startRowIndex) + ' AND{3}" +
                "                   rowNumber <  ' + CONVERT(varchar(9), @upperBound){3}" +
                "  exec (@sqlStatement){3}" +
                "      {3}" +
                "END{3}", 
                dbName, selectTableSchema, spConfig.Name.TableName, Environment.NewLine);

            if (spConfig.UseGo)
            {
                sss.AppendLine("GO");
            }
            return sss.ToString();
        }
        
        #endregion
        
        #region Private Methods

        private static IEnumerable<vw_SPGenenerator> GetNonTextualColumns(IEnumerable<vw_SPGenenerator> allColumnsOfTable)
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
        
        private static IEnumerable<vw_SPGenenerator> GetTextColumns(IEnumerable<vw_SPGenenerator> allColumnsOfTable)
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
        
        private static IEnumerable<vw_SPGenenerator> GetNationalTextColumns(IEnumerable<vw_SPGenenerator> allColumnsOfTable)
        {
            return allColumnsOfTable.Where(
                p => (
                         (p.DATA_TYPE.ToLower() == "nvarchar") ||
                         (p.DATA_TYPE.ToLower() == "ntext")
                     )
                );
        }
        
        private static string GetTableSchema(IEnumerable<vw_SPGenenerator> allColumnsOfTable, StoredProcedureConfig spConfig)
        {
            return allColumnsOfTable.First(p => p.TABLE_NAME == spConfig.Name.TableName).TABLE_SCHEMA;
        }
        
        private static void AppendNormalizeQueryForTextColumns(IEnumerable<vw_SPGenenerator> allColumnsOfTable, StringBuilder sss)
        {
            var textColumns = GetNationalTextColumns(allColumnsOfTable);
            int k = 0;
            foreach (var textCol in textColumns)
            {
                if (k > 0)
                    sss.AppendFormat("{0}", Environment.NewLine);
                sss.AppendFormat(@"    Set @{0} = dbo.GetStandardString(@{0});", textCol.COLUMN_NAME);
                k++;
            }
        }
        
        private static void AppendParameterDeclarationFor(IEnumerable<vw_SPGenenerator> allColumnsOfTable, StringBuilder sss)
        {
            int i = 0;
            foreach (var row in allColumnsOfTable.Where(x => x.COLUMN_NAME.ToLower() != "lastmodificationtime"))
            {
                if (i > 0)
                    sss.AppendFormat(", {0}" +
                                     "	", Environment.NewLine);
                sss.AppendFormat("@{0} {1}{2}", row.COLUMN_NAME, row.DATA_TYPE, GetTypeWithLength(row));
                i++;
            }
        }

        private static string GetTypeWithLength(vw_SPGenenerator row)
        {
            if (row.CHARACTER_MAXIMUM_LENGTH != null && (row.DATA_TYPE != "text") && (row.DATA_TYPE != "ntext") && (row.DATA_TYPE != "image"))
            {
                return string.Format("({0})", (row.CHARACTER_MAXIMUM_LENGTH == -1) ? "MAX" : row.CHARACTER_MAXIMUM_LENGTH.ToString());
            }
            return "";
        }

        private static void AppendNormalizeQueryForTextColumnsForSearch(IEnumerable<vw_SPGenenerator> allColumnsOfTable, StringBuilder sss)
        {
            var textColumns = GetTextColumns(allColumnsOfTable);
            int k = 0;
            foreach (var textCol in textColumns)
            {
                if (k > 0)
                    sss.AppendLine("");
                if(textCol.DATA_TYPE.ToLower() == "nvarchar" || textCol.DATA_TYPE.ToLower() == "ntext")
                {
                    sss.AppendFormat(@"    Set @{0} = '%' + dbo.GetStandardString(@{0}) + '%';", textCol.COLUMN_NAME);
                }
                else
                {
                    sss.AppendFormat(@"    Set @{0} = '%' + @{0} + '%';", textCol.COLUMN_NAME);
                }
                k++;
            }
        }

        private static bool HasIdentity(IEnumerable<vw_SPGenenerator> allColumnsOfTable)
        {
            bool hasIdentity = false;
            if (allColumnsOfTable.Where(x => (x.IsIdentity.HasValue && x.IsIdentity == 1)).Count() > 0)
            {
                hasIdentity = true;
            }
            return hasIdentity;
        }
        
        #endregion Private Methods
    }
}