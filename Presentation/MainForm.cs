using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Collections;
using FlexiSqlTools.Core;

namespace FlexiSqlTools.Presentation.WindowsFormsApplication
{
    public partial class MainForm : Form
    {

        private string insertTemplate;
        private string oldSearchTemplate;
        private string searchTemplate;
        private string updateTemplate;
        private string selectRowTemplate;
        private string selectAllTemplate;
        private string deleteRowTemplate;

        #region Private Fields

        private SqlConnection mainSqlConnection;

        #endregion

        #region Event Hanlers

        private void Form1_Load(object sender, EventArgs e)
        {
            EnableOrDisableControl();

            txtConnectionString.Text = string.Format(@"Data Source={0};Integrated Security = True;", txtServerName.Text);

            chkLstRowNumber.Enabled = chkRowNumber.Checked;

        }

        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {

        }

        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            EnableOrDisableControl();

            txtServerName_TextChanged(sender, e);

        }

        private void txtServerName_TextChanged(object sender, EventArgs e)
        {
            txtConnectionString.Text = "Data Source=" + txtServerName.Text + ";";

            if (rbtnWindows.Checked)
                txtConnectionString.Text += "Integrated Security = True;";
            else if (rbtnSql.Checked)
                txtConnectionString.Text += string.Format("User ID={0};password={1};", txtUsername.Text, txtPassword.Text);

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            //Microsoft.SqlServer.Management.Smo.

            string serverName = txtServerName.Text;

            mainSqlConnection = new SqlConnection(txtConnectionString.Text);

            GenerateTreeAndFillDBCombo();

        }

        private void GenerateTreeAndFillDBCombo()
        {
            string serverName = txtServerName.Text;
            mainSqlConnection = new SqlConnection(txtConnectionString.Text);

            SqlDataAdapter da = new SqlDataAdapter("SELECT database_id AS ID, NULL AS ParentID, NAME AS Text FROM sys.databases WHERE state != 6 ORDER BY [Name] ", mainSqlConnection);

            DataSet ds = new DataSet();

            da.Fill(ds);

            //foreach (DataRow dr in ds.Tables[0].Rows)
            //{
            //    comboDB.Items.Add(dr["text"].ToString());
            //}

            comboDB.DisplayMember = "text";
            comboDB.ValueMember = "text";
            comboDB.DataSource = ds.Tables[0];

            treeView1.Nodes.Clear();

            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                //childrenQuery += string.Format("SELECT name FROM [{0}].[sys].[Tables] ORDER BY name ", dr["text"]);

                TreeNode tn = new TreeNode(dr["text"].ToString(), GetTablesAndViewsAsTreeNode(dr["text"].ToString()));
                treeView1.Nodes.Add(tn);
            }



        }

        private void btnBuild_Click(object sender, EventArgs e)
        {
            //Dictionary<string, TreeNodeCollection> dic = new Dictionary<string, TreeNodeCollection>();
            //string result = "";
            StringBuilder result = new StringBuilder();

            foreach (TreeNode node in treeView1.Nodes)
            {
                //result += GenerateQuery(node);
                string generatedQuery = GenerateAndExecuteQuery(node);
                if (!string.IsNullOrEmpty(generatedQuery))
                {
                    result.AppendLine(generatedQuery);
                }
                //result.AppendLine("GO");
            }

            if (chkcopyToClipboard.Checked) Clipboard.SetText(result.ToString());

            txtGeneratedQuery.Text = result.ToString();
            txtGeneratedQuery.SelectAll();
            tabControl1.SelectedIndex = 2;

        }

        #endregion

        #region Designer

        private void EnableOrDisableControl()
        {
            if (rbtnWindows.Checked)
            {
                txtUsername.Enabled = false;
                txtPassword.Enabled = false;
            }
            else if (rbtnSql.Checked)
            {

                txtUsername.Enabled = true;
                txtPassword.Enabled = true;
            }
        }

        private TreeNode[] GetTablesAndViewsAsTreeNode(string dbName)
        {
            Collection<TreeNode> result = new Collection<TreeNode>();
            DataSet dsSchemas = GetDatabseSchemas(dbName);

            if (dsSchemas != null && dsSchemas.Tables.Count > 0)
            {
                foreach (DataRow dr in dsSchemas.Tables[0].Rows)
                {
                    var schemaName = dr["Schema_Name"].ToString();
                    TreeNode shemaNodes = new TreeNode(schemaName);
                    DataSet dsTables = GetDatabseTables(dbName, schemaName);
                    DataSet dsViews = GetDatabaseViews(dbName, schemaName);
                    if (dsTables != null && dsTables.Tables.Count > 0)
                    {
                        foreach (DataRow tableRow in dsTables.Tables[0].Rows)
                        {
                            TreeNode tableNode = new TreeNode(string.Format("{0}.{1}", tableRow["Schema_Name"], tableRow["Table_Name"]));
                            shemaNodes.Nodes.Add(tableNode);
                        }
                    }
                    if (dsViews != null && dsViews.Tables.Count > 0)
                    {
                        foreach (DataRow viewRow in dsViews.Tables[0].Rows)
                        {
                            //TreeNode treeNode = new TreeNode(dr["name"].ToString());
                            TreeNode viewNode = new TreeNode(string.Format("{0}.{1}", viewRow["Schema_Name"], viewRow["View_Name"]));
                            shemaNodes.Nodes.Add(viewNode);
                        }
                    }


                    result.Add(shemaNodes);
                }
            }
            return result.ToArray();
        }

        #endregion

        #region Helpers

        private void FillDatabaseCombobox(ComboBox databaseCombobox, string connectionString)
        {
            SqlConnection con = new SqlConnection(connectionString);

            SqlDataAdapter da = new SqlDataAdapter("SELECT database_id AS ID, NULL AS ParentID, NAME AS Text FROM sys.databases WHERE state != 6 ORDER BY [Name] ", con);

            DataSet ds = new DataSet();

            try
            {
                da.Fill(ds);
                databaseCombobox.DisplayMember = "text";
                databaseCombobox.ValueMember = "text";
                databaseCombobox.DataSource = ds.Tables[0];
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error: Cant execute query.{0}{1}", Environment.NewLine, ex.Message));
            }
        }

        #endregion

        public MainForm()
        {
            deleteRowTemplate = DataBaseHelper.GetCommentForSp() +
                                "if OBJECT_ID ('|tableName|_Create') is not null\r\n" +
                                "Begin\n" +
                                "    Drop Proc [|dbName|].[|schemaName|].[|tableName|_Create]\n" +
                                "End\n" +
                                "GO\n" +
                                "Create Procedure [|dbName|].[|schemaName|].[|tableName|_Create]\n" +
                                "	--@Id int, @Id int, @Id int, @Id int \n" +
                                "AS\n" +
                                "BEGIN\n" +
                                "    Set NoCount On;\n" +
                                "    Begin Try\n" +
                                "	    DELETE FROM [|dbName|].[|schemaName|].[|tableName|]\n" +
                                "	    WHERE |whereStatement|\n" +
                                "	End Try\n" +
                                "	Begin Catch\n" +
                                "		Return ERROR_NUMBER()\n" +
                                "	End Catch\n" +
                                "END\n" +
                                "GO\n";
            insertTemplate = DataBaseHelper.GetCommentForSp() +
                             "if OBJECT_ID ('|tableName|_Create') is not null\r\n" +
                             "Begin\n" +
                             "    Drop Proc [|dbName|].[|schemaName|].[|tableName|_Create]\n" +
                             "End\n" +
                             "GO\n" +
                             "Create Procedure [|dbName|].[|schemaName|].[|tableName|_Create]\n" +
                             "	|inputParams|,\n" +
                             "	@InsertedId int output\n" +
                             "With Encryption\n" +
                             "As\n" +
                             "Begin\n" +
                             "    Set NoCount On;\n" +
                             "    |normalizeNationalTexts|\n" +
                             "\n" +
                             "    Insert Into [|dbName|].[|schemaName|].[|tableName|]\n" +
                             "        (|insertColumnList|)\n" +
                             "    Values\n" +
                             "        (|insertValueList|)\n" +
                             "    \n" +
                             "    Set @InsertedId = SCOPE_IDENTITY();\n" +
                             "End\n" +
                             "GO\n";
            updateTemplate = DataBaseHelper.GetCommentForSp() +
                             "if OBJECT_ID ('|tableName|_Update') is not null\n" +
                             "Begin\n" +
                             "Drop Proc |tableName|_Update\n" +
                             "End\n" +
                             "GO\n" +
                             "Create Procedure [|dbName|].[|schemaName|].|tableName|_Update\n" +
                             "|updateParams|\n" +
                             "/*|identityParams|,\n" +
                             "--|inputParams|*/\n" +
                             "AS\n" +
                             "BEGIN\n" +
                             "Set NoCount On;\n" +
                             "|normalizeNationalTexts|\n" +
                             "\n" +
                             "Update [|dbName|].[|schemaName|].[|tableName|]\n" +
                             "Set\n" +
                             "|updateColumnList|\n" +
                             "Where\n" +
                             "|whereStatement|\n" +
                             "END\n" +
                             "GO\n";
            InitializeComponent();
        }

        private DataSet GetDatabseSchemas(string dbName)
        {
            string selectCommandText = string.Format(
                "Select [{0}].[sys].[schemas].[name] as schema_name, " +
                "       [{0}].[sys].[schemas].[schema_id], " +
                "       [{0}].[sys].[schemas].[principal_id] " +
                "From   [{0}].[sys].[schemas]", dbName);
            SqlDataAdapter da = new SqlDataAdapter(selectCommandText, mainSqlConnection);
            DataSet ds = new DataSet();
            try
            {
                da.Fill(ds);
                return ds;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        private DataSet GetDatabseTables(string dbName, string shemaName)
        {
            //SqlDataAdapter da = new SqlDataAdapter(string.Format("SELECT name FROM [{0}].[sys].[Tables] ORDER BY name ", dbName), mainSqlConnection);
            string selectCommandText = string.Format(
                "SELECT  [{0}].[sys].[schemas].[name] as Schema_Name, {2}" +
                "        [{0}].[sys].[Tables].[name] as Table_Name {2}" +
                "FROM    [{0}].[sys].[Tables] {2}" +
                "inner join [{0}].[sys].[schemas] on [{0}].[sys].[Tables].[schema_id] = [{0}].[sys].[schemas].[schema_id] {2}" +
                "WHERE [{0}].[sys].[schemas].[name] like '{1}' {2}" +
                "ORDER BY Schema_Name, Table_Name",
                dbName, shemaName, Environment.NewLine);
            SqlDataAdapter da = new SqlDataAdapter(selectCommandText, mainSqlConnection);
            DataSet ds = new DataSet();
            try
            {
                da.Fill(ds);
                return ds;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        private DataSet GetDatabaseViews(string dbName, string shemaName)
        {
            //SqlDataAdapter da = new SqlDataAdapter(string.Format("SELECT name FROM [{0}].[sys].[Views] ORDER BY name ", dbName), mainSqlConnection);
            string selectCommandText = string.Format(
                "SELECT  [{0}].[sys].[schemas].[name] as Schema_Name, {2}" +
                "        [{0}].[sys].[Views].[name] as View_Name {2}" +
                "FROM    [{0}].[sys].[Views] {2}" +
                "inner join [{0}].[sys].[schemas] on [{0}].[sys].[Views].[schema_id] = [{0}].[sys].[schemas].[schema_id]{2}" +
                "WHERE [{0}].[sys].[schemas].[name] like '{1}' {2}" +
                "ORDER BY Schema_Name, View_Name",
                dbName, shemaName, Environment.NewLine);
            SqlDataAdapter da = new SqlDataAdapter(selectCommandText, mainSqlConnection);
            DataSet ds = new DataSet();
            try
            {
                da.Fill(ds);
                return ds;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        private string GenerateAndExecuteQuery(TreeNode databaseNode)
        {
            StringBuilder result = new StringBuilder(String.Empty);
            //var nodesCsv = TreeNodeCollectionToCsv(GetCheckedTablesAsTreeNodes(databaseNode.Nodes), "'");
            var nodesCsv = TreeNodeCollectionToCsv(GetCheckedTablesAsTreeNodes(databaseNode.Nodes, false), "");
            if (nodesCsv == "" || nodesCsv == "''") return "";
            Collection<vw_SPGenenerator> dicTables_dicColumns = GetTabels_Rows(databaseNode.Text, nodesCsv);

            if (dicTables_dicColumns.Count > 0)
            {
                foreach (TreeNode schemaNode in databaseNode.Nodes)
                {
                    foreach (TreeNode tblNode in schemaNode.Nodes)
                    {

                        StoredProcedureConfig storedProcedureConfig = new StoredProcedureConfig();
                        storedProcedureConfig.UseGo = chkUseGo.Checked;
                        storedProcedureConfig.UseDataBase = chkAddUseDB.Checked;
                        string schemaName = tblNode.Text.Split('.')[0];
                        string tableName = tblNode.Text.Split('.')[1];
                        storedProcedureConfig.Name = new StoredProcedureName()
                                                         {
                                                             Prefix = txtPrefix.Text.Trim(),
                                                             Postfix = txtPostfix.Text.Trim(),
                                                             SchemaName = schemaName,
                                                             TableName = tableName /*,
                                                             Name = ""*/
                                                         };

                        if (tblNode.Checked)
                        {
                            var allColumnsForTable = GetColumnsCollectionForTable(dicTables_dicColumns, schemaName, tableName);
                            if (chkDeleteRow.Checked)
                            {
                                #region if (chkDeleteRow.Checked)

                                storedProcedureConfig.Name.Name = txtDeleteRow.Text.Trim();
                                string deleteRowSpQuery = DataBaseHelper.GenerateDeleteRowSp(databaseNode.Text, allColumnsForTable, storedProcedureConfig);
                                result.AppendLine(deleteRowSpQuery);
                                //if(chkUseGo.Checked) result.AppendLine("GO");
                                ExecuteQueryIfNeeded(deleteRowSpQuery);

                                #endregion

                                #region From Template

                                storedProcedureConfig.Name.Name = txtDeleteRow.Text.Trim();
                                string generatedFromTemplate = DataBaseHelper.GenerateQueryFromTemplate(databaseNode.Text, allColumnsForTable, storedProcedureConfig, this.deleteRowTemplate);
                                result.AppendLine(generatedFromTemplate);
                                //if(chkUseGo.Checked) result.AppendLine("GO");
                                ExecuteQueryIfNeeded(generatedFromTemplate);

                                #endregion From Template
                            }
                            if (chkInsert.Checked)
                            {
                                #region if (chkInsert.Checked)

                                storedProcedureConfig.Name.Name = txtInsert.Text.Trim();
                                string insertSpQuery = DataBaseHelper.GenerateInsertSp(databaseNode.Text, allColumnsForTable, storedProcedureConfig);
                                result.AppendLine(insertSpQuery);
                                //if (chkUseGo.Checked) result.AppendLine("GO");
                                ExecuteQueryIfNeeded(insertSpQuery);

                                #endregion

                                #region From Template

                                storedProcedureConfig.Name.Name = txtInsert.Text.Trim();
                                string generatedFromTemplate = DataBaseHelper.GenerateQueryFromTemplate(databaseNode.Text, allColumnsForTable, storedProcedureConfig, this.insertTemplate);
                                result.AppendLine(generatedFromTemplate);
                                //if(chkUseGo.Checked) result.AppendLine("GO");
                                ExecuteQueryIfNeeded(generatedFromTemplate);

                                #endregion From Template
                            }
                            if (chkSelectAll.Checked)
                            {
                                #region if (chkSelectAll.Checked)

                                storedProcedureConfig.Name.Name = txtSelectAll.Text.Trim();
                                string selectAllSpQuery = DataBaseHelper.GenerateSelectAllSp(databaseNode.Text, allColumnsForTable, storedProcedureConfig);
                                result.AppendLine(selectAllSpQuery);
                                //if (chkUseGo.Checked) result.AppendLine("GO");
                                ExecuteQueryIfNeeded(selectAllSpQuery);

                                #endregion
                            }
                            if (chkSelectRow.Checked)
                            {
                                #region if (chkSelectRow.Checked)

                                storedProcedureConfig.Name.Name = txtSelectRow.Text.Trim();
                                string selectRowSpQuery = DataBaseHelper.GenerateSelectRowSp(databaseNode.Text, allColumnsForTable, storedProcedureConfig);
                                result.AppendLine(selectRowSpQuery);
                                //if (chkUseGo.Checked) result.AppendLine("GO");
                                ExecuteQueryIfNeeded(selectRowSpQuery);

                                #endregion
                            }
                            if (chkUpdate.Checked)
                            {
                                #region if (chkUpdate.Checked)

                                storedProcedureConfig.Name.Name = txtUpdate.Text.Trim();
                                string updateSpQuery = DataBaseHelper.GenerateUpdateSp(databaseNode.Text, allColumnsForTable, storedProcedureConfig);
                                result.AppendLine(updateSpQuery);
                                //if (chkUseGo.Checked) result.AppendLine("GO");
                                ExecuteQueryIfNeeded(updateSpQuery);

                                #endregion

                                #region From Template

                                storedProcedureConfig.Name.Name = txtUpdate.Text.Trim();
                                string generatedFromTemplate = DataBaseHelper.GenerateQueryFromTemplate(databaseNode.Text, allColumnsForTable, storedProcedureConfig, this.updateTemplate);
                                result.AppendLine(generatedFromTemplate);
                                //if(chkUseGo.Checked) result.AppendLine("GO");
                                ExecuteQueryIfNeeded(generatedFromTemplate);

                                #endregion From Template
                            }
                            if (chkOldSearch_PageSort.Checked)
                            {
                                #region if (chkOldSearch_PageSort.Checked)

                                storedProcedureConfig.Name.Name = txtOldSearch_PageSort.Text.Trim();
                                string oldSearchWithPageSortSpQuery = DataBaseHelper.GenerateOldSearchWithPageSortTotalCountSp(databaseNode.Text, allColumnsForTable, storedProcedureConfig);
                                result.AppendLine(oldSearchWithPageSortSpQuery);
                                //if (chkUseGo.Checked) result.AppendLine("GO");
                                ExecuteQueryIfNeeded(oldSearchWithPageSortSpQuery);

                                #endregion
                            }
                            if (chkSearch_PageSort.Checked)
                            {
                                #region if (chkSearch_PageSort.Checked)

                                storedProcedureConfig.Name.Name = txtSearch_PageSort.Text.Trim();
                                string searchWithPageSortSpQuery = DataBaseHelper.GenerateSearchWithPagingAndSortingTotalCountSp(databaseNode.Text, allColumnsForTable, storedProcedureConfig);
                                result.AppendLine(searchWithPageSortSpQuery);
                                //if (chkUseGo.Checked) result.AppendLine("GO");
                                ExecuteQueryIfNeeded(searchWithPageSortSpQuery);

                                #endregion
                            }
                        }
                    }
                }
            }
            return result.ToString();
        }

        private void ExecuteQueryIfNeeded(string SelectRowSPQuery)
        {
            if (chkExecuteQueryInDB.Checked)
                throw new NotImplementedException();
        }

        private static IEnumerable<vw_SPGenenerator> GetColumnsCollectionForTable(Collection<vw_SPGenenerator> dicTables_dicColumns, string schemaName, string tableName)
        {
            return dicTables_dicColumns.Where(p => (p.TABLE_SCHEMA == schemaName && p.TABLE_NAME == tableName));
        }

        private static string GetCheckedTablesInCSV(TreeNode databaseNode)
        {
            string result = "";
            for (int i = 0, checkedCount = 0; i < databaseNode.Nodes.Count; i++)
            {
                if (databaseNode.Nodes[i].Checked)
                {
                    if (checkedCount == 0) result += ",";
                    checkedCount++;
                    result += databaseNode.Nodes[i].Text;
                }
            }
            return result;
        }

        private static string TreeNodeCollectionToCsv(Collection<TreeNode> nodes, string quoteChar)
        {
            string csvTableNames = "";
            // be ezaye har table
            for (int i = 0; i < nodes.Count; i++)
            {
                if (i != 0) csvTableNames += ",";
                csvTableNames += quoteChar + nodes[i].Text + quoteChar;
            }

            if (csvTableNames == "") csvTableNames = quoteChar + quoteChar;
            return csvTableNames;
        }

        private static Collection<TreeNode> GetCheckedTablesAsTreeNodes(TreeNodeCollection nodes, bool recursiveCall)
        {
            Collection<TreeNode> col = new Collection<TreeNode>();

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Checked && recursiveCall)
                    col.Add(nodes[i]);
                if (nodes[i].Nodes.Count > 0)
                {
                    var subNodes = GetCheckedTablesAsTreeNodes(nodes[i].Nodes, true);
                    for (int j = 0; j < subNodes.Count; j++)
                    {
                        if (subNodes[j].Checked)
                            col.Add(subNodes[j]);
                    }
                }
            }
            return col;
        }

        #region with Hashtable

        private Collection<vw_SPGenenerator> GetTabels_Rows(string databaseName, string csvTableNames)
        {
            mainSqlConnection = new SqlConnection(txtConnectionString.Text);

            #region  selectCommand

            string whereClause = "";
            string[] schemaDotTableNameArray = csvTableNames.Split(',');
            for (int i = 0; i < schemaDotTableNameArray.Length; i++)
            {
                string[] temp = schemaDotTableNameArray[i].Split('.');
                string schemaName = temp[0];
                string tableName = temp[1];
                if (i > 0) whereClause += " Or ";
                whereClause += string.Format(@"
	            ([{0}].INFORMATION_SCHEMA.COLUMNS.TABLE_SCHEMA = '{1}'
	            And [{0}].INFORMATION_SCHEMA.COLUMNS.TABLE_NAME = '{2}'
	            )", databaseName, schemaName, tableName);
            }
            string selectCommand = string.Format(
                "{2}" +
                "SELECT     TOP (100) PERCENT {2}" +
                "	[{0}].INFORMATION_SCHEMA.COLUMNS.TABLE_SCHEMA,{2}" +
                "	[{0}].INFORMATION_SCHEMA.COLUMNS.TABLE_NAME,{2}" +
                "	[{0}].INFORMATION_SCHEMA.COLUMNS.COLUMN_NAME,{2}" +
                "	[{0}].INFORMATION_SCHEMA.COLUMNS.ORDINAL_POSITION,{2}" +
                "	[{0}].INFORMATION_SCHEMA.COLUMNS.DATA_TYPE,{2}" +
                "	[{0}].INFORMATION_SCHEMA.COLUMNS.CHARACTER_MAXIMUM_LENGTH,{2}" +
                "	({2}" +
                "		SELECT COUNT(*) AS Expr1{2}" +
                "		FROM [{0}].sys.all_columns {2}" +
                "		INNER JOIN [{0}].sys.tables ON [{0}].sys.all_columns.object_id = [{0}].sys.tables.object_id{2}" +
                "		INNER JOIN [{0}].sys.types ON [{0}].sys.all_columns.system_type_id = [{0}].sys.types.system_type_id{2}" +
                "		INNER JOIN {2}" +
                "			[{0}].sys.identity_columns ON [{0}].sys.tables.object_id = [{0}].sys.identity_columns.object_id{2}" +
                "			AND{2}" +
                "			[{0}].sys.all_columns.column_id = [{0}].sys.identity_columns.column_id{2}" +
                "		 WHERE{2}" +
                "			([{0}].sys.tables.name = [{0}].INFORMATION_SCHEMA.COLUMNS.TABLE_NAME){2}" +
                "			AND{2}" +
                "			([{0}].sys.all_columns.name = [{0}].INFORMATION_SCHEMA.COLUMNS.COLUMN_NAME){2}" +
                "	) AS IsIdentity,{2}" +
                "	vw_PK_Table_Column.name AS Constraint_Name,{2}" +
                "	vw_PK_Table_Column.type AS Constraint_Type,{2}" +
                "	vw_PK_Table_Column.unique_index_id,{2}" +
                "	IndexTypeTable.IndexType{2}" +
                "FROM{2}" +
                "	[{0}].INFORMATION_SCHEMA.COLUMNS {2}" +
                "	LEFT OUTER JOIN{2}" +
                "	({2}" +
                "		SELECT{2}" +
                "			tables_1.name AS tblName,{2}" +
                "			all_columns_1.name AS ColName,{2}" +
                "			[{0}].sys.indexes.type AS IndexType{2}" +
                "		FROM{2}" +
                "			[{0}].sys.all_columns AS all_columns_1{2}" +
                "		INNER JOIN [{0}].sys.tables AS tables_1 ON all_columns_1.object_id = tables_1.object_id{2}" +
                "		INNER JOIN{2}" +
                "			[{0}].sys.index_columns AS index_columns_1 ON tables_1.object_id = index_columns_1.object_id {2}" +
                "			AND {2}" +
                "			all_columns_1.column_id = index_columns_1.column_id{2}" +
                "		INNER JOIN{2}" +
                "			[{0}].sys.indexes ON index_columns_1.object_id = [{0}].sys.indexes.object_id{2}" +
                "			AND{2}" +
                "			index_columns_1.index_id = [{0}].sys.indexes.index_id) AS IndexTypeTable ON [{0}].INFORMATION_SCHEMA.COLUMNS.TABLE_NAME = IndexTypeTable.tblName {2}" +
                "			AND {2}" +
                "			[{0}].INFORMATION_SCHEMA.COLUMNS.COLUMN_NAME = IndexTypeTable.ColName{2}" +
                "		LEFT OUTER JOIN{2}" +
                "			({2}" +
                "				SELECT{2}" +
                "					[{0}].sys.key_constraints.name,{2}" +
                "					[{0}].INFORMATION_SCHEMA.KEY_COLUMN_USAGE.TABLE_NAME,{2}" +
                "					[{0}].INFORMATION_SCHEMA.KEY_COLUMN_USAGE.COLUMN_NAME,{2}" +
                "					[{0}].sys.key_constraints.object_id,{2}" +
                "					[{0}].sys.key_constraints.principal_id,{2}" +
                "					[{0}].sys.key_constraints.parent_object_id,{2}" +
                "					[{0}].sys.key_constraints.type,{2}" +
                "					[{0}].sys.key_constraints.unique_index_id{2}" +
                "				FROM{2}" +
                "					[{0}].INFORMATION_SCHEMA.KEY_COLUMN_USAGE{2}" +
                "					INNER JOIN [{0}].sys.key_constraints ON [{0}].INFORMATION_SCHEMA.KEY_COLUMN_USAGE.CONSTRAINT_NAME = [{0}].sys.key_constraints.name{2}" +
                "			) {2}" +
                "			AS vw_PK_Table_Column ON [{0}].INFORMATION_SCHEMA.COLUMNS.TABLE_NAME = vw_PK_Table_Column.TABLE_NAME {2}" +
                "			AND {2}" +
                "			[{0}].INFORMATION_SCHEMA.COLUMNS.COLUMN_NAME = vw_PK_Table_Column.COLUMN_NAME{2}" +
                "        WHERE{2}" +
                "	        ({1}){2}" +
                "ORDER BY {2}" +
                "	[{0}].INFORMATION_SCHEMA.COLUMNS.TABLE_NAME,{2}" +
                "	[{0}].INFORMATION_SCHEMA.COLUMNS.ORDINAL_POSITION{2}",
                databaseName, whereClause, Environment.NewLine);

            #endregion

            SqlDataAdapter da = new SqlDataAdapter(selectCommand, mainSqlConnection);

            DataSet dsColumnsOfTable = new DataSet();
            da.Fill(dsColumnsOfTable);

            Collection<vw_SPGenenerator> records = new Collection<vw_SPGenenerator>();

            //vw_SPGenenerator record;
            foreach (DataRow row in dsColumnsOfTable.Tables[0].Rows)
            {
                vw_SPGenenerator record = new vw_SPGenenerator();

                record.CHARACTER_MAXIMUM_LENGTH = (row["CHARACTER_MAXIMUM_LENGTH"] == DBNull.Value) ? null : (int?) row["CHARACTER_MAXIMUM_LENGTH"];
                record.COLUMN_NAME = (string) row["COLUMN_NAME"];

                if (row["Constraint_Name"] != System.DBNull.Value)
                    record.Constraint_Name = (string) row["Constraint_Name"];
                if (row["Constraint_Type"] != System.DBNull.Value)
                    record.Constraint_Type = (string) row["Constraint_Type"];

                record.DATA_TYPE = (string) row["DATA_TYPE"];
                record.IsIdentity = (int?) row["IsIdentity"];
                //record.IsIndex = (int?)row["IsIndex"];
                if (row["IndexType"] != DBNull.Value) record.IndexType = (byte?) row["IndexType"];

                record.ORDINAL_POSITION = (int?) row["ORDINAL_POSITION"];
                record.TABLE_NAME = (string) row["TABLE_NAME"];
                record.TABLE_SCHEMA = (string) row["TABLE_SCHEMA"];

                if (row["unique_index_id"] != System.DBNull.Value)
                    record.unique_index_id = (int?) row["unique_index_id"];

                //(string)dr["COLUMN_NAME"], 

                records.Add(record);
            }
            return records;
        }

        #endregion

        private void comboDB_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillComboTables(txtConnectionString.Text, comboDB, comboTables);
        }

        private void FillComboTables(string connectionString, ComboBox databaseComboBox, ComboBox tableCombobox)
        {
            SqlConnection con = new SqlConnection(connectionString);
            SqlDataAdapter da = new SqlDataAdapter(string.Format(
                "Use [{0}];{1}" +
                "{1}" +
                "SELECT  [sys].[schemas].[name] as Schema_Name, [sys].[Tables].[name] as Table_Name, [sys].[schemas].[name] + '.' + [sys].[Tables].[name] as Schema_NameDotTable_Name{1}" +
                "FROM [sys].[Tables] {1}" +
                "inner join [sys].[schemas] on [sys].[Tables].[schema_id] = [sys].[schemas].[schema_id]{1}" +
                "ORDER BY Schema_Name, Table_Name ", databaseComboBox.SelectedValue, Environment.NewLine), con);
            DataSet ds = new DataSet();
            try
            {
                da.Fill(ds);

                tableCombobox.DisplayMember = "Schema_NameDotTable_Name";
                tableCombobox.ValueMember = "Schema_NameDotTable_Name";
                tableCombobox.DataSource = ds.Tables[0];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void chkRowNumber_CheckedChanged(object sender, EventArgs e)
        {
            chkLstRowNumber.Enabled = chkRowNumber.Checked;
        }

        private void chkLstRowNumber_SelectedIndexChanged(object sender, EventArgs e)
        {
            //for (int i = 0; i < chkLstRowNumber.CheckedItems.Count; i++)
            //{
            //    ((CheckBox)chkLstRowNumber.CheckedItems[i]).Checked = false;
            //}
        }

        private void btnBuildCustom_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not Implemented Yet", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void comboTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            Collection<vw_SPGenenerator> rows = GetTabels_Rows(comboDB.SelectedValue.ToString(), comboTables.SelectedValue.ToString());

            chkLstMain.Items.Clear();
            chkLstWhere.Items.Clear();
            chkLstRowNumber.Items.Clear();

            foreach (vw_SPGenenerator dr in rows)
            {
                string itemText = dr.COLUMN_NAME;
                if (dr.Constraint_Type != null) itemText += "(" + dr.Constraint_Type + ")";
                //if (dr.IsIndex == 1) itemText += "(IX_C)";
                //if (dr.IsIndex == 2) itemText += "(IX)";
                if (dr.IndexType == 1) itemText += "(IX_C)";
                if (dr.IndexType == 2) itemText += "(IX)";
                if (dr.IsIdentity == 1) itemText += "(Identity)";

                chkLstMain.Items.Add(itemText);
                chkLstWhere.Items.Add(itemText);
                chkLstOrder.Items.Add(itemText);
                chkLstRowNumber.Items.Add(itemText);
            }
        }

        private void rbtn_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtnDelete.Checked)
            {
                chkLstMain.Enabled = false;
                chkLstWhere.Enabled = true;
                chkLstOrder.Enabled = false;
                chkLstRowNumber.Enabled = false;
            }
            else if (rbtnInsert.Checked)
            {
                chkLstMain.Enabled = true;
                chkLstWhere.Enabled = false;
                chkLstOrder.Enabled = false;
                chkLstRowNumber.Enabled = false;
            }
            else if (rbtnSelect.Checked)
            {
                chkLstMain.Enabled = true;
                chkLstWhere.Enabled = true;
                chkLstOrder.Enabled = true;
                chkLstRowNumber.Enabled = true;
            }
            else if (rbtnUpdate.Checked)
            {
                chkLstMain.Enabled = true;
                chkLstWhere.Enabled = true;
                chkLstOrder.Enabled = false;
                chkLstRowNumber.Enabled = false;
            }
            else if (rbtnSearch.Checked)
            {
                chkLstMain.Enabled = true;
                chkLstWhere.Enabled = true;
                chkLstOrder.Enabled = true;
                chkLstRowNumber.Enabled = true;
            }
            else
            {
                chkLstMain.Enabled = true;
                chkLstWhere.Enabled = true;
                chkLstOrder.Enabled = true;
                chkLstRowNumber.Enabled = true;
            }
        }

        private void btnConnectSourceDB_Click(object sender, EventArgs e)
        {
            FillDatabaseCombobox(cbSourceDatabase, txtSourceConnectionString.Text);
        }

        private void btnConnectDestinationDB_Click(object sender, EventArgs e)
        {
            FillDatabaseCombobox(cbDestinationDatabase, txtDestinationConnectionString.Text);
        }

        private void txtSourceConnectionStrng_TextChanged(object sender, EventArgs e)
        {
            txtSourceConnectionString.Text = string.Format("Data Source={0};", txtSourceServerName.Text);
            if (rbSourceWinAuth.Checked)
                txtSourceConnectionString.Text += "Integrated Security = True;";
            else if (rbSourceSqlAuth.Checked)
                txtSourceConnectionString.Text += string.Format("User ID={0};password={1};", txtSourceUsername.Text, txtSourcePassword.Text);
        }

        private void txtDestinationServerName_TextChanged(object sender, EventArgs e)
        {
            txtDestinationConnectionString.Text = string.Format("Data Source={0};", txtDestinationServerName.Text);
            if (rbDestinationWinAuth.Checked)
                txtDestinationConnectionString.Text += "Integrated Security = True;";
            else if (rbDestinationSqlAuth.Checked)
                txtDestinationConnectionString.Text += string.Format("User ID={0};password={1};", txtDestinationUsername.Text, txtDestinationPassword.Text);
        }

        private void cbSourceDatabase_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillComboTables(txtSourceConnectionString.Text, cbSourceDatabase, cbSourceTable);
        }

        private void cbDestinationDatabase_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillComboTables(txtDestinationConnectionString.Text, cbDestinationDatabase, cbDestinationTable);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var result = new StringBuilder();

            foreach (TreeNode node in treeView1.Nodes)
            {
                //result += GenerateQuery(node);
                string generatedQuery = GenerateAndExecuteQuery(node);
                if (!string.IsNullOrEmpty(generatedQuery))
                {
                    result.AppendLine(generatedQuery);
                }
                //result.AppendLine("GO");
            }

            if (chkcopyToClipboard.Checked) Clipboard.SetText(result.ToString());

            txtGeneratedQuery.Text = result.ToString();
            txtGeneratedQuery.SelectAll();
            tabControl1.SelectedIndex = 2;
        }

        private void btnDeleteRowTemplate_Click(object sender, EventArgs e)
        {
            var templateDialog = new TemplateDialog();
            var dres = templateDialog.ShowDialog();
            this.deleteRowTemplate = templateDialog.textBox1.Text;
        }

        private void btnInsertTemplate_Click(object sender, EventArgs e)
        {
            var templateDialog = new TemplateDialog();
            templateDialog.textBox1.Text = insertTemplate;
            var dialogResult = templateDialog.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                this.insertTemplate = templateDialog.textBox1.Text;
            }
        }

        private void btnUpdateTemplate_Click(object sender, EventArgs e)
        {
            var templateDialog = new TemplateDialog();
            templateDialog.textBox1.Text = updateTemplate;
            var dialogResult = templateDialog.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                this.updateTemplate = templateDialog.textBox1.Text;
            }
        }
    }
}