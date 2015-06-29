using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DISPLAY
{
    public partial class NoteBox : Form
    {
        public NoteBox()
        {
            InitializeComponent();
        }

        static string Notes = null;

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OkayButton_Click(object sender, EventArgs e)
        {
            Notes = NoteBox1.Text;
            this.Close();
        }

        private void NoteBox_Load(object sender, EventArgs e)
        {
            NoteBox1.Text = Notes;
        }
    }
}
