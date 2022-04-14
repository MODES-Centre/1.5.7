using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ApiExternalTestApp
{
    public partial class ConnectionForm_ : Form
    {
        public ConnectionForm_()
        {
            InitializeComponent();
        }

        public string serverName;
        public bool isLocal = false;

        public string User = Environment.UserDomainName + "\\" + Environment.UserName;
        public string Pass = "";

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            serverName = textBox1.Text;
            User = textBox2.Text;
            Pass = textBox3.Text;
            Close();
        }

        private void ConnectionForm__Load(object sender, EventArgs e)
        {
            textBox1.Text = "modes-centre";
            comboBox1.SelectedIndex = 1;
            textBox2.Text = Environment.UserDomainName + "\\" + Environment.UserName;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 1)
            {
                isLocal = false;
                groupBox1.Enabled = false;
            }
            else
            {
                isLocal = true;
                groupBox1.Enabled = true;
            }
        }
    }
}
