using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlexiSqlTools.Presentation.WindowsFormsApplication
{
    public partial class frmCompareData : Form
    {
        public frmCompareData()
        {
            InitializeComponent();
        }

        private void CompareData_Load(object sender, EventArgs e)
        {
            //comboBox1.Items.Add(
        }

        private void cbSourceDatabase_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (((ComboBox)sender).SelectedItem.ToString().ToLower() == "New Database".ToLower())
            {
                var editConnection = new EditConnection();
                var dialogResult = editConnection.ShowDialog();
                MessageBox.Show(dialogResult.ToString());
            }
        }
    }
}
