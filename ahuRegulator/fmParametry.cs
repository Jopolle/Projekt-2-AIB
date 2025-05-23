using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ahuRegulator
{
    public partial class fmParametry : Form
    {

        public double kp;
        public double ki;
        public double t1;
        public double t2;
        public double kp2;
        public double ki2;
        public double naw_temp;

        public fmParametry()
        {
            InitializeComponent();
        }

        public void UstawKontrolki()
        {
            edKp.Text = kp.ToString();
            edKi.Text = ki.ToString();
            edT1.Text = t1.ToString();
            edT2.Text = t2.ToString();
            textBox1.Text = ki2.ToString();
            textBox2.Text = kp2.ToString();



        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                kp = Convert.ToDouble(edKp.Text);
                ki = Convert.ToDouble(edKi.Text);
                t1 = Convert.ToDouble(edT1.Text);
                t2 = Convert.ToDouble(edT2.Text);
                kp2 = Convert.ToDouble(textBox2.Text);
                ki2 = Convert.ToDouble(textBox1.Text);

                Close();
                DialogResult = DialogResult.OK;
            }
            catch
            {

            }

            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
            DialogResult = DialogResult.Cancel;
        }

        private void fmParametry_Load(object sender, EventArgs e)
        {
            UstawKontrolki();
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void edT2_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
