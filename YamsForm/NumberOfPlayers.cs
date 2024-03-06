using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YamsForm
{
    public partial class NumberOfPlayers : Form
    {
        public NumberOfPlayers()
        {
            InitializeComponent();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button1.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int noOfPlayers = Int32.Parse(comboBox1.Text);
            Settings.noOfPlayers = noOfPlayers;

            Form playerNames = new PlayerName();
            this.Hide();
            playerNames.Show();
        }
    }
}
