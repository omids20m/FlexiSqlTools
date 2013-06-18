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
    public partial class EditConnection : Form
    {
        public EditConnection()
        {
            InitializeComponent();
        }

        private void rbtnUseWinAuth_CheckedChanged(object sender, EventArgs e)
        {
            lblUsername.Enabled = false;
            lblPassword.Enabled = false;
            txtUsername.Enabled = false;
            txtPassword.Enabled = false;
        }

        private void rbtnUseSqlServerAuth_CheckedChanged(object sender, EventArgs e)
        {
            lblUsername.Enabled = true;
            lblPassword.Enabled = true;
            txtUsername.Enabled = true;
            txtPassword.Enabled = true;
        }
    }
}
