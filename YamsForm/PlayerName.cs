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
    public partial class PlayerName : Form
    {
        public PlayerName()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void PlayerName_Load(object sender, EventArgs e)
        {
            Settings.InitializePlayerNames();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            Settings.PlayerNames[Settings.currentPlayer] = textBox1.Text;
            Settings.currentPlayer++;


            if (Settings.currentPlayer == Settings.noOfPlayers)
            {
                this.Hide();
                Form Yams = new Yams();
                Yams.Show();

            }

            textBox1.Text = null;
        }
    }
}
