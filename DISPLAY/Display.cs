/*
 DISPLAY REWRITTEN IN C# BY: ZACHARY SAMUELS 9/01/11
 VERSION 1.11
 
 ORIGINAL WRITTEN IN QBASIC BY: LEO DENLEA 02/17/00
 
 DESCRIPTION: THIS PROGRAM READS DATA FROM A CNC FORMAT FILE (.NC)
			  AND DISPLAYS THE DESIGN WITH MULTIPLE OPTIONS.
 
 NOTE: TRACING AND LINE SELECTION CURRENTLY ONLY WORK IF THE ORIGIN
       IS IN THE LOWER LEFT HAND CORNER
*/

#region Directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.IO;
using System.Media;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
#endregion

namespace DISPLAY
{
    public partial class Display : Form
    {
        #region Fields
        /*****************************************
        * VARIABLE USAGE */
        [DllImport("user32.dll")]
        private static extern int FindWindow(string lpszClassName, string lpszWindowName);
        [DllImport("user32.dll")]
        private static extern int ShowWindow(int hWnd, int nCmdShow);
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 1;
        public Bitmap mainCanvas;

        private StreamReader NCFIN = null;
        private int SCREENWIDTH = (int)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
        private int SCREENHEIGHT = (int)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
        private int Position = 0, min_i = 0;
        private Label ProgramDetails, GCODE, drawingBarLabel, Inch_Calculator, ArcLabel1, ArcLabel2;
        private bool Trace = false, Locked = false, ContainsXorY = false, G0Show = true, ArcDirection = false, ShowFBB = true, ZoomFormOpen = false, ContainsFBB = false;
        private Bitmap TempBitMap = null;
        private double X = 0, Y = 0, XMAX = 0, XMIN = 0, YMAX = 0, YMIN = 0, LINE_INCH, ARC_INCH, SCALE;
        private string GNUMBER = null, FILENAME = null, Notes = null, Burn_Inches = null, SizeDesign = null;
        private List<NCParse> NCList = new List<NCParse>();
        private Dictionary<string, double> PointInches = new Dictionary<string, double>();
        private TemporaryFile CanvasCopy, G0CanvasCopy, ArcDirectionCanvasCopy;
        private Label[] InchColor;
        private static int SecondToLast;
        private static int Last;
        /****************************************/
        #endregion

        #region Display Form
        public Display()
        {
            InitializeComponent();
        }

        private void full_maximize(object sender, EventArgs e)
        {
            // First, Hide the taskbar

            int hWnd = FindWindow("Shell_TrayWnd", "");
            // ShowWindow(hWnd, SW_HIDE);

            // Then, format and size the window. 
            // Important: Borderstyle -must- be first, 
            // if placed after the sizing functions, 
            // it'll strangely firm up the taskbar distance.

            FormBorderStyle = FormBorderStyle.Sizable; // Was none.
            this.Location = new Point(30, 30);
            this.WindowState = FormWindowState.Normal;

            //        The following is optional, but worth knowing.

                      this.Size = new Size(Screen.PrimaryScreen.Bounds.Width-80, Screen.PrimaryScreen.Bounds.Height-80);
            //        this.TopMost = true;
        }

        #region Form Load
        //Form Load Event
        private void Display_Load(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();  //If command line arguments exist, they are stored here
            if (args.Length <= 1) //If there are no command line arguments, open a file dialog for the user
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    if (!@openFileDialog1.FileName.Contains(".NC") && !@openFileDialog1.FileName.Contains(".nc"))
                    {
                        MessageBox.Show("NOT A .NC FILE" + args);
                        Application.Exit();
                        return;
                    }
                    try
                    {
                        FileStream NcReader = new FileStream(openFileDialog1.FileName, FileMode.Open);
                        NCFIN = new StreamReader(NcReader);
                    }
                    catch (Exception ex)
                    {
                        this.SendToBack();
                        MessageBox.Show(ex.ToString() + " occured.");
                        Application.Exit();
                    }
                }
                else
                {
                    Application.Exit();
                    return;
                }
            }
            else if (args.Length > 1) //If command line arguments are found
            {
                if (args[1] == "/h" || args[1] == "/H")
                {
                    MessageBox.Show("Help:\n\nPossible Arguments:\n  Argument 1: Thisfile.exe\n  Argument 2: NCFILE\n  Argument 3: /G0|/X\n" +
                        "  Argument 4: /PointCheck|/X\n  Argument 5: /RadiusCheck|/X\n  Argument 6: /OriginCheck|/X\n  Argument 7: /NoFBB\n  " +
                        "Example: >Display.exe Test /G0 /X /RadiusCheck /OriginCheck /NoFBB");
                    Application.Exit();
                }

                if (File.Exists(args[1] + ".NC")) //If an NC file exists
                {
                    args[1] += ".NC";
                }
                // The following is not right but I'm not fixing it now. 
                else if (!args[1].Contains(".NC") && !args[1].Contains(".nc"))  //If an NC file does not exist
                {
                    MessageBox.Show("NOT A .NC FILE or file not found. " + String.Join(", ", args));
                    Application.Exit();
                    return;
                }
                else
                {
                    MessageBox.Show("FILE DOES NOT EXIST. " + String.Join(", ", args));
                    Application.Exit();
                    return;
                }
                try
                {
                    FileStream NcReader = new FileStream(args[1], FileMode.Open);
                    NCFIN = new StreamReader(NcReader);
                }
                catch (Exception ex)
                {
                    this.SendToBack();
                    MessageBox.Show(ex.ToString() + " occured.");
                    Application.Exit();
                }
            }

            //If the user enters /NoFBB as the 7th argument, fast burn bridges are not shown
            if (args.Length > 6)
            {
                if (args[6] == "/NoFBB")
                    ShowFBB = false;
            }

            try
            {
                Read_And_Parse();
            }
            catch (Exception ex)
            {
                this.SendToBack();
                MessageBox.Show("Exception " + ex.ToString() + " occurred. \n\nThis error is generally caused by a bad NC command and sometimes a blank line in the NC Code.  Please take a look at the NC Code.");
                Application.Exit();
            }

            //Title the Display window as (Display - [NCFile])
            if (args.Length > 1)
                this.Text = "File: " + args[1].Substring(0, args[1].Length - 3);
            else
                this.Text = "File: " + Path.GetFileNameWithoutExtension(@openFileDialog1.FileName);

            FILENAME = this.Text;

            try
            {
                Scale_Window();
            }
            catch (Exception ex)
            {
                this.SendToBack();
                MessageBox.Show("Exception " + ex.ToString() + " occurred. \n\nThis error is generally caused by a bad NC command and sometimes a blank line in the NC Code.  Please take a look at the NC Code.");
                Application.Exit();
            }

            Graphics g = Graphics.FromImage(mainCanvas);
            g.Clear(Color.Black);

            //Sets the position of the last BURN and the position of the second to last BURN (Used for coloring the last and second to last line)
            try
            {
                for (int i = NCList.Count - 1; i >= 0; i--)
                {
                    if ((NCList[i].ContainsX || NCList[i].ContainsY) && (NCList[i].G != "0" && NCList[i].G != "00"))
                    {
                        Last = NCList[i].LineNumber;
                        for (int j = i - 1; j > 0; j--)
                        {
                            if (NCList[j].ContainsX || NCList[j].ContainsY && (NCList[j].G != "0" && NCList[j].G != "00"))
                            {
                                SecondToLast = NCList[j].LineNumber;
                                break;
                            }
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                this.SendToBack();
                MessageBox.Show("Exception " + ex.ToString() + " occurred. \n\nThis error is generally caused by a bad NC command and sometimes a blank line in the NC Code.  Please take a look at the NC Code.");
                Application.Exit();
            }

            //Sets the X and Y back to the origin and draws the design
            X = Math.Abs(XMIN);
            Y = Math.Abs(YMAX);
            G0Show = false;
            // MessageBox.Show("Count of NCList = " + NCList.Count);
            try
            {
                for (int j = 0; j < NCList.Count; j++)
                    draw_design(j);
            }
            catch (Exception ex)
            {
                this.SendToBack();
                MessageBox.Show("Exception " + ex.ToString() + " occurred. \n\nThis error is generally caused by a bad NC command and sometimes a blank line in the NC Code.  Please take a look at the NC Code.");
                Application.Exit();
            }

            // Draw_Meta();

            G0CanvasCopy = new TemporaryFile();
            //Saves the NO G0 canvas to a temporary file
            mainCanvas.Save(@G0CanvasCopy.FilePath);
            G0Show = true;

            ArcDirection = true;
            for (int j = 0; j < NCList.Count; j++)
                draw_design(j);
            //Saves the Arc Direction canvas to a temorary file
            ArcDirectionCanvasCopy = new TemporaryFile();
            mainCanvas.Save(@ArcDirectionCanvasCopy.FilePath);
            ArcDirection = false;

            for (int j = 0; j < NCList.Count; j++)
                draw_design(j);

            #region Dynamic Labels
            /*******************************************
            * CREATE DYNAMIC LABELS
            */
            ProgramDetails = new Label();
            ProgramDetails.Text = "CNC (.NC) DISPLAY BY ZACH SAMUELS\n" + String.Format("{0, 30}", "VERSION ") + Constants.Version;
            ProgramDetails.Location = new Point((int)(SCREENWIDTH * .75), 10);
            ProgramDetails.AutoSize = true;
            ProgramDetails.Font = new Font("Times New Roman", 9.0F);
            this.Controls.Add(ProgramDetails);
            RainbowTimer.Start();

            Label Controls = new Label();
            Controls.Text = "Controls:\nD - Redraw the object\nT - Trace the design\nG - Make G0 Moves Transparent\nA - Show Arc Directions\n" +
                "Z - Open zoom window\nL - Select a line from line number\nN - Add Notes\nP - Print\nV - Print Preview\nO - Open new file (If not opened from command)\nX - Stop all commands\nESC - Quit";
            Controls.Location = new Point((int)(SCREENWIDTH * .75), 60);
            Controls.AutoSize = true;
            Controls.Font = new Font("Times New Roman", 9.0F);
            Controls.ForeColor = Color.White;
            this.Controls.Add(Controls);

            GCODE = new Label();
            GCODE.Text = "GCODE:\nSpeed:\nPoint:\nLine Number:";
            GCODE.Location = new Point((int)(SCREENWIDTH * .75), 270);
            GCODE.AutoSize = true;
            GCODE.ForeColor = Color.OrangeRed;
            GCODE.Font = new Font("Times New Roman", 9.0F);
            this.Controls.Add(GCODE);

            /// NOTE: DUE TO THE DESIGN BEING DRAW TO 3 FILES, POINTINCHES MUST BE DIVIDED BY 3 ///
            Inch_Calculator = new Label();
            Inch_Calculator.Font = new Font("Times New Roman", 9.0F);
            Inch_Calculator.Location = new Point((int)(SCREENWIDTH * .75), 440);
            Inch_Calculator.Text = "Inch Calculator\n\nG0 Inches: " + (PointInches["G0"]/3).ToString("0.0");
            double TotalInches = 0;
            foreach (string Key in PointInches.Keys)
            {
                if (Key != "G0" && PointInches[Key] != 0)
                {
                    Burn_Inches += Key + " inches: " + (PointInches[Key]/3).ToString("0.0") + "\n";
                    Inch_Calculator.Text += "\n" + Key + ": " + (PointInches[Key]/3).ToString("0.0");
                    TotalInches += PointInches[Key]/3;
                }
            }
            TotalInches += PointInches["G0"]/3;
            Inch_Calculator.Text += "\nTotal Inches Moved: " + TotalInches.ToString("0.0") + "\nEfficiency Ratio Burn/Travel: " +
                (((TotalInches - PointInches["G0"]/3) / TotalInches) * 100).ToString("0.0") +
                "%\n\nSize of Design: " + (XMAX - XMIN).ToString("0.0") + " X " + (YMAX - YMIN).ToString("0.0");
            SizeDesign = (XMAX - XMIN).ToString("0.0") + " x " + (YMAX - YMIN).ToString("0.0");
            
            Inch_Calculator.AutoSize = true;
            Inch_Calculator.ForeColor = Color.White;
            Inch_Calculator.Font = new Font("Times New Roman", 9.0F);
            this.Controls.Add(Inch_Calculator);

            int InchColorY = 620;
            InchColor = new Label[11];
            for (int i = 0; i < 11; i++)
                InchColor[i] = new Label();

            InchColor[0].Text = "G0 Move";
            InchColor[0].ForeColor = Color.BlueViolet;
            InchColor[0].Location = new Point((int)(SCREENWIDTH * .75), InchColorY);

            InchColor[1].Text = "Etching";
            InchColor[1].ForeColor = Color.Blue;
            InchColor[1].Location = new Point((InchColor[0].Location.X + 60), InchColorY);

            InchColor[2].Text = "2 Point";
            InchColor[2].ForeColor = Color.Green;
            InchColor[2].Location = new Point((InchColor[1].Location.X + 50), InchColorY);

            InchColor[3].Text = "3 Point";
            InchColor[3].ForeColor = Color.Cyan;
            InchColor[3].Location = new Point((InchColor[0].Location.X), InchColorY + 20);

            InchColor[4].Text = "1 Pass";
            InchColor[4].ForeColor = Color.WhiteSmoke;
            InchColor[4].Location = new Point((InchColor[3].Location.X + 50), InchColorY + 20);

            InchColor[5].Text = "1.5 Point";
            InchColor[5].ForeColor = Color.LightCyan;
            InchColor[5].Location = new Point((InchColor[4].Location.X + 50), InchColorY + 20);

            InchColor[6].Text = "Buffer";
            InchColor[6].ForeColor = Color.LightBlue;
            InchColor[6].Location = new Point((InchColor[0].Location.X), InchColorY + 40);

            InchColor[7].Text = "Snug";
            InchColor[7].ForeColor = Color.Gray;
            InchColor[7].Location = new Point((InchColor[6].Location.X + 50), InchColorY + 40);

            InchColor[8].Text = "Wide";
            InchColor[8].ForeColor = Color.LightGreen;
            InchColor[8].Location = new Point((InchColor[7].Location.X + 50), InchColorY + 40);

            InchColor[9].Text = "Pulsing";
            InchColor[9].ForeColor = Color.Salmon;
            InchColor[9].Location = new Point((InchColor[0].Location.X), InchColorY + 60);

            if (ShowFBB == true)
            {
                InchColor[10].Text = "FBB";
                InchColor[10].ForeColor = Color.Red;
                InchColor[10].Location = new Point((InchColor[9].Location.X + 50), InchColorY + 60);
            }

            foreach (Label l in InchColor)
            {
                l.AutoSize = true;
                l.Font = new Font("Times New Roman", 9.0F);
                this.Controls.Add(l);
            }

            drawingBarLabel = new Label();
            drawingBarLabel.Text = "Slow Down/Speed Up Drawing";
            drawingBarLabel.Location = new Point((int)(SCREENWIDTH * .75), 360);
            drawingBarLabel.AutoSize = true;
            drawingBarLabel.ForeColor = Color.White;
            drawingBarLabel.Font = new Font("Times New Roman", 9.0F);
            drawingBarLabel.Hide();
            this.Controls.Add(drawingBarLabel);

            drawingBar.Location = new Point((int)(SCREENWIDTH * .75), 375);
            drawingBar.Width = 300;
            drawingBar.TickStyle = TickStyle.None;
            drawingBar.Hide();
            /*
             * ******************************************/
            #endregion

            this.BackColor = Color.Black;
            this.ForeColor = Color.Black;
            this.Location = new Point(0, 0);

            //Saves the canvas to a temporary file
            CanvasCopy = new TemporaryFile();
            mainCanvas.Save(@CanvasCopy.FilePath);

            ArgsCommands(args);
            full_maximize(sender, e);

            this.Refresh(); //Refreshes the form
            g.Dispose(); //Releases the resources used by Graphics g
            this.TopMost = true;
            this.Activate();
        }
        #endregion

        private void Display_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawImage(mainCanvas, 10, 10, mainCanvas.Width*.8f, mainCanvas.Height*.8f); //Places the canvas at 10,10
            this.DoubleBuffered = true; //Double buffers for smoother graphics
        }
        #region DrawMeta
        /*
        private void Draw_Meta()
        {
            using (Graphics og = CreateGraphics())
            {
                IntPtr hdc = og.GetHdc();
                try
                {
                    var myImage = new Metafile(hdc, EmfType.EmfPlusOnly);
                    using (Graphics g = Graphics.FromImage(myImage))
                    {
                        g.DrawString("Hello", new Font(FontFamily.GenericSerif, 18), Brushes.Blue, 0, 0);
                    }
                }
                finally
                {
                    og.ReleaseHdc(hdc);
                }

            }

        }
        // var G0MetaTempFile = new TemporaryFile();
        //var G0MetaFile = new Metafile(G0MetaTempFile.FilePath);
        //Graphics gm = Graphics;

        */
        #endregion
        #region Scale Window
        /*
        * Scales the design to the size of the window.
         * Should scale it to printer resolution not screen resolution.
        */
        private void Scale_Window()
        {
            double New_XMax = Math.Abs(XMIN - XMAX);
            double New_YMax = Math.Abs(YMIN - YMAX);

            SCALE = Math.Floor((SCREENWIDTH * .85) / New_XMax);
            //If the design is scaled in the X direction but goes out of bounds in the Y direction, it is scaled to YMAX
            if (New_YMax * SCALE > SCREENHEIGHT * .90)
            {
                SCALE = Math.Floor((SCREENHEIGHT * .90) / New_YMax);
            }
            New_YMax *= SCALE + 2; //Add 2 for extra room
            New_XMax *= SCALE + 2;

            mainCanvas = new Bitmap((int)New_XMax, (int)New_YMax, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        }
        #endregion

        #region Form Key Down Event
        /*
         * Form Keydown Event
         */
        public void Display_KeyDown(object sender, KeyEventArgs e)
        {
            Keys keydown = e.KeyCode; //Sets the KeyCode pressed to keydown

            if (keydown == Keys.Escape) //If the user presses escape, the application exists
            {
                if (!ZoomFormOpen)
                    Application.Exit();
            }

            Graphics g = Graphics.FromImage(mainCanvas);

            //If the canvas is not locked and the user does not press X, Z or N
            if (Locked == false && keydown != Keys.X && keydown != Keys.Z && keydown != Keys.N && keydown != Keys.P && keydown != Keys.V)
            {
                //Reset X and Y to the origin
                X = Math.Abs(XMIN);
                Y = Math.Abs(YMAX);

                Locked = true; //Locks the form
                if (keydown == Keys.D) //Draws the design to a timer that can be sped up or slown down
                {
                    drawingBar.Show(); //Shows the drawing trackbar
                    drawingBarLabel.Show(); //Shows the drawingBar Label
                    Position = 0; //Sets the position of lines to 0
                    g.Clear(Color.Black); //Clears the canvas
                    RedrawTimer.Start(); //Starts the RedrawTimer
                }
                else if (keydown == Keys.G) //If the user presses G this makes the G0 moves transparent
                {
                    Bitmap Canvas;

                    //Draws the No G0 canvas
                    using (FileStream stream = new FileStream(@G0CanvasCopy.FilePath, FileMode.Open, FileAccess.Read))
                    {
                        Canvas = (Bitmap)Bitmap.FromStream(stream);
                    }
                    g.DrawImage(Canvas, new Point(0, 0));

                }
                else if (keydown == Keys.A)
                {
                    ArcLabel1 = new Label();
                    ArcLabel1.Text = "G3 Counter-Clockwise";
                    ArcLabel1.Location = new Point((int)(SCREENWIDTH * .75), 370);
                    ArcLabel1.AutoSize = true;
                    ArcLabel1.ForeColor = Color.Blue;
                    ArcLabel1.Font = new Font("Times New Roman", 11.0F);
                    this.Controls.Add(ArcLabel1);

                    ArcLabel2 = new Label();
                    ArcLabel2.Text = "G2 Clockwise";
                    ArcLabel2.Location = new Point((int)(SCREENWIDTH * .75), 350);
                    ArcLabel2.AutoSize = true;
                    ArcLabel2.ForeColor = Color.Red;
                    ArcLabel2.Font = new Font("Times New Roman", 11.0F);
                    this.Controls.Add(ArcLabel2);

                    Bitmap Canvas;
                    using (FileStream stream = new FileStream(@ArcDirectionCanvasCopy.FilePath, FileMode.Open, FileAccess.Read))
                    {
                        Canvas = (Bitmap)Bitmap.FromStream(stream);
                    }
                    g.DrawImage(Canvas, new Point(0, 0));
                }
                else if (keydown == Keys.T) //If the user presses T, he/she can trace through the lines
                {
                    ///WARNING THIS ONLY WORKS IF THE ORIGIN IS IN THE LOWER LEFT HAND CORNER///
                    MessageBox.Show("WARNING: TRACING ONLY WORKS IF THE ORIGIN IS IN THE LOWER LEFT HAND CORNER.  PRESS OKAY TO CONTINUE WITH TRACING.");
            
                    Position = 0; //Resets the position to -1
                    GCODE.Text = "GCODE:\nSpeed:\nPoint:\nLine Number:"; //Resets the GCode Label Text
                    Trace = true;
                }
                else if (keydown == Keys.O) //If the user presses O he/she can open another file if he/she opened the program NOT from the command line
                {
                    Application.Restart();
                }
                else if (keydown == Keys.L) //If the user presses L, he/she can select a line number and then trace through the design
                {
                    ///WARNING THIS ONLY WORKS IF THE ORIGIN IS IN THE LOWER LEFT HAND CORNER///
                    MessageBox.Show("WARNING: TRACING ONLY WORKS IF THE ORIGIN IS IN THE LOWER LEFT HAND CORNER.  PRESS OKAY TO CONTINUE WITH TRACING.");
            
                    Trace = true;
                    
                    //Since C# does not have an input box function, a VisualBasic function is used instead
                    //This function pops up an input box and gets the line number
                    this.SendToBack();
                    string LineNumberString = Microsoft.VisualBasic.Interaction.InputBox("Line Number?", "Goto Line");

                    int LineNumber;
                    int.TryParse(LineNumberString, out LineNumber); //Trys to parse the user input box
                    LineNumber -= 1; //Subtracts 1 because NCList starts at 0 (This assumes that lines start at 1)
                    Position = LineNumber;
                    if (LineNumber < 0 || LineNumber >= NCList.Count) //If the user enters and invalid line number
                    {
                        MessageBox.Show("Invalid Line Number");
                        return;
                    }

                    //Redraws the main canvas
                    Bitmap Canvas;
                    using (FileStream stream = new FileStream(@CanvasCopy.FilePath, FileMode.Open, FileAccess.Read)) //Redraws the canvas
                    {
                        Canvas = (Bitmap)Bitmap.FromStream(stream);
                    }
                    g.DrawImage(Canvas, new Point(0, 0));

                    //Selects the line or arc entered
                    if (NCList[Position].ContainsX || NCList[Position].ContainsY)
                    {
                        for (int i = Position - 1; i >= 0; i--) //Searches for the previous line that contains X or Y
                        {
                            if (NCList[i].ContainsX || NCList[i].ContainsY) //If a line contains X or contains Y
                            {
                                if (NCList[Position].ContainsI || NCList[Position].ContainsJ) //If a line contains I or J
                                {
                                    DrawArc(NCList[i].Xabs, NCList[i].YabsInvert, Position, g, new Pen(Color.White)); //Draws the Arc
                                    break;
                                }
                                else
                                {
                                    //Draws the Line
                                    g.DrawLine(new Pen(Color.White), new PointF((float)(NCList[Position].Xabs * SCALE),
                                        (float)(NCList[Position].YabsInvert * SCALE)), new PointF((float)(NCList[i].Xabs * SCALE),
                                            (float)(NCList[i].YabsInvert * SCALE)));
                                    break;
                                }
                            }
                        }
                    }
                    GCODE.Text = "GCODE: " + NCList[LineNumber].GCODE + "\nSpeed: " + NCList[LineNumber].Speed + "\nPoint: " +
                        NCList[LineNumber].POINT + "\nLine Number: " + NCList[LineNumber].LineNumber;
                }
            }
            else if (keydown == Keys.V) //If the user presses V, he/she can preview the design + inch calculator + notes
            {
                mainCanvas.MakeTransparent(Color.Black); //Sets the black background to transparent
                for (int x = 0; x < mainCanvas.Width; x++)
                {
                    for (int y = 0; y < mainCanvas.Height; y++)
                    {
                        if (mainCanvas.GetPixel(x, y).Name != "0") //If a pixel is transparent make it black
                            mainCanvas.SetPixel(x, y, Color.Black);
                    }
                }

                //Creates a temporary bitmap that will be printed
                TempBitMap = new Bitmap(mainCanvas, new Size((int)(mainCanvas.Width),
                    (int)(mainCanvas.Height)));

                // Prints the document
                PrintDocument doc = new PrintDocument();
                doc.PrintPage += this.Doc_PrintPage;

                //Shows the Print Settings and Print Preview Dialogs
                PrintPreviewDialog dlgPreviewSettings = new PrintPreviewDialog();
                PrintDialog dlgPrintSettings = new PrintDialog();
                dlgPreviewSettings.Document = doc;
                dlgPreviewSettings.ClientSize = new System.Drawing.Size(600, 800);
                dlgPrintSettings.Document = doc;
                if (dlgPrintSettings.ShowDialog() == DialogResult.OK)
                {
                    // dlgPreviewSettings.Focus();
                    DialogResult dlgResult;
                    dlgResult = dlgPreviewSettings.ShowDialog(this);
                    if (dlgResult == DialogResult.Cancel)
                    {
                        // MessageBox.Show("Print Preview Showdialog returned Cancel.");
                    }
                    else
                    {
                        MessageBox.Show("Print Preview Showdialog returned Other than Cancel. " + dlgResult);
                    }
                }
                // TempBitMap.Dispose(); //Releases all resources used by the temporary bitmap
            }
            else if (keydown == Keys.P) //If the user presses P, he/she can print the design + inch calculator + notes
            {
                //Prints the document
                PrintDocument doc = new PrintDocument();
                doc.PrintPage += this.Doc_PrintPage;

                //Shows the Print Settings Dialog
                PrintDialog dlgSettings = new PrintDialog();
                dlgSettings.Document = doc;
                if (dlgSettings.ShowDialog() == DialogResult.OK)
                {
                    mainCanvas.MakeTransparent(Color.Black); //Sets the black background to transparent
                    for (int x = 0; x < mainCanvas.Width; x++)
                    {
                        for (int y = 0; y < mainCanvas.Height; y++)
                        {
                            if (mainCanvas.GetPixel(x, y).Name != "0") //If a pixel is transparent make it black
                                mainCanvas.SetPixel(x, y, Color.Black);
                        }
                    }

                    //Creates a temporary bitmap that will be printed
                    TempBitMap = new Bitmap(mainCanvas, new Size((int)(mainCanvas.Width),
                        (int)(mainCanvas.Height)));
                    doc.Print();
                    // TempBitMap.Dispose(); //Releases all resources used by the temporary bitmap
                    this.Activate();
                }
                else
                {
                    this.Activate();
                    return;
                }
            }
            else if (keydown == Keys.Z) //Opens the form that allows the user to pan zoom
            {
                ZoomFormOpen = true;
                ImageZoom.ImageZoomMainForm ZoomForm = new ImageZoom.ImageZoomMainForm();
                ZoomForm.SetFilePath = @CanvasCopy.FilePath;
                ZoomForm.StartPosition = FormStartPosition.CenterScreen;
                ZoomForm.ShowDialog();
                ZoomFormOpen = false;
                Locked = false;
            }
            else if (keydown == Keys.N) //If the user presses N, the noteform is shown
            {
                NoteBox NoteForm = new NoteBox();
                NoteForm.StartPosition = FormStartPosition.CenterParent;
                this.SendToBack();
                NoteForm.ShowDialog();
                Notes = NoteForm.NoteBox1.Text;
                Locked = false;
            }
            else if (keydown == Keys.Right && Trace == true) //If the user is tracing through the design and presses the right arrow
            {
                if (Position < NCList.Count - 1) //If the position is not the last line
                {
                    Position++; //Go to the next line
                    TraceFunction(g);
                }
            }
            else if (keydown == Keys.Left && Trace == true)
            {
                if (Position > 0) //If the selected line is not the less than the beginning
                {
                    Position--; //Go back one line
                    TraceFunction(g);
                }
            }
            else if (keydown == Keys.X) //If the user presses X, the form is reset
            {
                if (ArcLabel1 != null && ArcLabel2 != null)
                {
                    ArcLabel1.Hide();
                    ArcLabel2.Hide();
                }

                drawingBar.Hide(); //Hides the drawingBar trackbar
                drawingBarLabel.Hide(); //Hides the drawingLabel
                Locked = false; //Unlocks the form
                NCList[min_i].IsSelected = false; //No lines are selected
                g.Clear(Color.Black); //Clears the form to black
                Trace = false; //Form is not being traced
                RedrawTimer.Stop(); //Stops the RedrawTimer
                Position = 0; //Sets the position to 0

                //Redraws the main canvas
                Bitmap Canvas;
                using (FileStream stream = new FileStream(@CanvasCopy.FilePath, FileMode.Open, FileAccess.Read))
                {
                    Canvas = (Bitmap)Bitmap.FromStream(stream);
                }
                g.DrawImage(Canvas, new Point(0, 0));

                GCODE.Text = "GCODE:\nSpeed:\nPoint:\nLine Number:";
            }
            else if (keydown == Keys.Shift || keydown == Keys.ShiftKey) //If the user presses X, the form is reset
            {
                // Do nothing.
            }
            else //If the form is locked
                MessageBox.Show("Please press X to reset the form"); //The user must reset the form

            this.Refresh();
            g.Dispose();
        }
        #endregion

        #region Form Closed Event
        /*
         * Form closed event
         */
        private void Display_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (CanvasCopy != null)
                CanvasCopy.Dispose(); //Disposes of the temporary file CanvasCopy          
            if (ArcDirectionCanvasCopy != null)
                ArcDirectionCanvasCopy.Dispose();
            if (G0CanvasCopy != null)
                G0CanvasCopy.Dispose();

            int hwnd = FindWindow("Shell_TrayWnd", "");
            ShowWindow(hwnd, 4); //Shows the tray (4 = Show No Activate)
        }
        #endregion

        #endregion

        #region Command Line Arguments/Error Checks
        /*
         * Possible args commands
         */
        private void ArgsCommands(string[] args)
        {
            Graphics g = Graphics.FromImage(mainCanvas);
            if (args.Length > 2 && ContainsFBB != true)
            {
                if (args[2] == "/G0")
                {
                    //Draws the No G0 canvas
                    Bitmap Canvas;
                    using (FileStream stream = new FileStream(@G0CanvasCopy.FilePath, FileMode.Open, FileAccess.Read))
                    {
                        Canvas = (Bitmap)Bitmap.FromStream(stream);
                    }
                    g.DrawImage(Canvas, new Point(0, 0));
                    G0Show = false;
                }
                ErrorChecks(args);
            }
        }

        /*
        * Design Error Checks:
        * 1: Check for pointage that goes from a higher number to a lower number
        * 2: Check if a radius is bigger than 99.9
        * 3: Check if design starts and ends at the origin
        */
        private void ErrorChecks(string[] args)
        {
            bool PointCheck = false, RadiusCheck = false, OriginCheck = false;
            if (args.Length > 3)
            {
                if (args[3] == "/PointCheck")
                    PointCheck = true;
            }
            if (args.Length > 4)
            {
                if (args[4] == "/RadiusCheck")
                    RadiusCheck = true;
            }
            if (args.Length > 5)
            {
                if (args[5] == "/OriginCheck")
                    OriginCheck = true;
            }

            double PointageCheck = 0;
            double OldPointage = 0;
            foreach (NCParse n in NCList)
            {
                OldPointage = PointageCheck;
                switch (n.POINT)
                {
                    case "1 Pass":
                        PointageCheck = 1;
                        break;
                    case "1.5 Point":
                        PointageCheck = 1.5;
                        break;
                    case "2 Point":
                        PointageCheck = 2;
                        break;
                    case "3 Point":
                        PointageCheck = 3;
                        break;
                    case "PULSING":
                        PointageCheck = 3.5;
                        break;
                    case "4 Point":
                        PointageCheck = 4;
                        break;
                    case "6 Point":
                        PointageCheck = 6;
                        break;
                    case "8 Point":
                        PointageCheck = 8;
                        break;
                    default:
                        break;
                }
                if (PointageCheck < OldPointage && PointCheck == true) //Check if new pointage is less than old pointage
                {
                    SystemSounds.Exclamation.Play();
                    DialogResult ContinueorClode = MessageBox.Show("You went from a higher pointage to a lower pointage.  Please make sure this is correct.",
                        "Higher Pointage to Lower Pointage", MessageBoxButtons.OKCancel);
                }
                if ((Math.Sqrt(Math.Pow(n.I, 2) + Math.Pow(n.J, 2))) > 99.9 && RadiusCheck == true) //Check if radius is greater than 99.9
                {
                    SystemSounds.Exclamation.Play();
                    DialogResult ContinueorClose = MessageBox.Show("Line: " + n.LineNumber + " has a radius bigger than 99.9 inches.",
                        "Radius Bigger Than 99.9", MessageBoxButtons.OKCancel);
                    if (ContinueorClose == DialogResult.Cancel)
                        Application.Exit();
                }
            }
            if (((Math.Abs(X).ToString("0.0000") != Math.Abs(XMIN).ToString("0.0000"))
                || (Math.Abs(Y).ToString("0.0000") != Math.Abs(YMAX).ToString("0.0000"))) && OriginCheck == true) //Check if design does not end at the origin
            {
                SystemSounds.Exclamation.Play();
                MessageBox.Show("Design does not end at the origin.");
         
            }
        }
        #endregion

        #region Read and Parse NC File
        /*
         * Reads an NC File and Parses different data from it
         */
        private void Read_And_Parse()
        {
            NCFIN.BaseStream.Position = 0;
            NCFIN.DiscardBufferedData();

            //While each line of the NC File is read in
            while (!NCFIN.EndOfStream)
            {
                NCParse temp = new NCParse(NCFIN.ReadLine()); //Create a temp NCParse object
                NCList.Add(temp); //Create a list of NCParse objects
            }
            NCFIN.Close();

            //Finds the Min and Max X and Y
            foreach (NCParse n in NCList)
            {
                double x = n.Xabs;
                double y = n.Yabs;
                if (x < XMIN)
                    XMIN = x;
                else if (x > XMAX)
                    XMAX = x;
                if (y < YMIN)
                    YMIN = y;
                else if (y > YMAX)
                    YMAX = y;
            }

            Y = Math.Abs(YMAX);

            //Parses a comment and sets speed and pointage for each line
            for (int j = 0; j < NCList.Count; j++)
            {
                //Sets the Inverted Y Absolute for each line
                if (NCList[j].ContainsX || NCList[j].ContainsY)
                {
                    NCList[j].YabsInvert = Y - NCList[j].Y;
                    Y -= NCList[j].Y;
                }
            }

            //Adds pointages for the inch calculator hash table
            PointInches.Add("G0", 0);
            PointInches.Add("1 Pass", 0);
            PointInches.Add("ETCHING", 0);
            PointInches.Add("1.5 Point", 0);
            PointInches.Add("2 Point", 0);
            PointInches.Add("PULSING", 0);
            PointInches.Add("3 Point", 0);
            PointInches.Add("4 Point", 0);
            PointInches.Add("6 Point", 0);
            PointInches.Add("8 Point", 0);
        }
        #endregion

        #region Drawing and Tracing
        #region Draw Design
        /*
         * Draws the design on the canvas
         */
        private void draw_design(int PositionOfLine)
        {
            Graphics g = Graphics.FromImage(mainCanvas);

            g.FillRectangle(new SolidBrush(Color.Red), (float)(Math.Abs(XMIN) * SCALE),
                (float)(Math.Abs(YMAX) * SCALE - 15), 15.0F, 15.0F); //Rectangle at the origin

            /* Variables X, Y, I, J, G */
            double xnum = NCList[PositionOfLine].X;
            double ynum = NCList[PositionOfLine].Y;
            double inum = NCList[PositionOfLine].I;
            double jnum = NCList[PositionOfLine].J;
            string gnum = NCList[PositionOfLine].G;
            GNUMBER = gnum;
            /***************************/

            //Checks to see if the design contains fast burn bridges
            if (NCList[PositionOfLine].Speed == "Fast")
                ContainsFBB = true;

            Brush myBrush = new SolidBrush(Color.Blue); //Creates a blue colored brush
            Pen BluePen = null;

            //If the G code for a design is 0 or 00, a BlueViolet dotted line is set as the brush for G0 Moves
            if (GNUMBER == "00" || GNUMBER == "0")
            {
                float[] dashValues = { 5, 5, 5, 5, 5, 5 };
                if (NCList[PositionOfLine].Speed == "Fast" && ShowFBB == true)
                    BluePen = new Pen(Color.Red);
                else
                    BluePen = new Pen(Color.BlueViolet);
                BluePen.DashPattern = dashValues;
            }
            else if (NCList[PositionOfLine].Speed == "Fast" && ShowFBB == true)
                myBrush = new SolidBrush(Color.Red);
            else if (NCList[PositionOfLine].IsSelected) //Color if a line is selected
                myBrush = new SolidBrush(Color.White);
            else if (NCList[PositionOfLine].Buffer)
                myBrush = new SolidBrush(Color.LightBlue);
            else if (NCList[PositionOfLine].WideFeed)
                myBrush = new SolidBrush(Color.LightGreen);
            else if (NCList[PositionOfLine].SnugFeed)
                myBrush = new SolidBrush(Color.Gray);
            else if (NCList[PositionOfLine].POINT == "Etching")
                myBrush = new SolidBrush(Color.Blue);
            else if (NCList[PositionOfLine].POINT == "1 Pass")
                myBrush = new SolidBrush(Color.WhiteSmoke);
            else if (NCList[PositionOfLine].POINT == "1.5 Point")
                myBrush = new SolidBrush(Color.LightCyan);
            else if (NCList[PositionOfLine].POINT == "2 Point")
                myBrush = new SolidBrush(Color.Green);
            else if (NCList[PositionOfLine].POINT == "3 Point")
                myBrush = new SolidBrush(Color.Cyan);
            else if (NCList[PositionOfLine].POINT == "Pulsing")
                myBrush = new SolidBrush(Color.Salmon);

            if (PositionOfLine == Last - 1 || PositionOfLine == SecondToLast - 1)
            {
                myBrush = new SolidBrush(Color.White);
            }

            Pen myPen = new Pen(myBrush);

            //If an NC line contains X or Y
            if (NCList[PositionOfLine].ContainsX || NCList[PositionOfLine].ContainsY)
            {
                //If an NC line is an arc
                if (NCList[PositionOfLine].ContainsI || NCList[PositionOfLine].ContainsJ)
                {
                    if (ArcDirection)
                    {
                        if (NCList[PositionOfLine].G == "2" || NCList[PositionOfLine].G == "02")
                            myPen = new Pen(Color.Red);
                        else if (NCList[PositionOfLine].G == "3" || NCList[PositionOfLine].G == "03")
                            myPen = new Pen(Color.Blue);
                    }

                    //NOTE: An Arc is drawn by a rectangle (or in this case square), an arc start angle, and an arc sweep angle

                    double SweepAngle = DrawArc(X, Y, PositionOfLine, g, myPen); //Gets the sweepangle and draws the arc

                    //Radius = Sqrt(I^2 + J^2)
                    double Radius = (Math.Sqrt(Math.Pow(NCList[PositionOfLine].I, 2) + Math.Pow(NCList[PositionOfLine].J, 2)));

                    ARC_INCH = arc_length(Radius, Math.Abs(SweepAngle)); //Length of an arc used by inch calculator

                    PointInches[NCList[PositionOfLine].POINT] += ARC_INCH; //Adds the length of the arc to the hash table of Pointages for inch calculator

                    X += xnum; //Update X
                    Y -= ynum; //Update Y
                }
                else //If an NC line is not an arc (a line)
                {
                    //If the G Command is a G0
                    if (GNUMBER == "0" || GNUMBER == "00")
                    {
                        PointInches["G0"] += distance(X, Y, (X + xnum), (Y - ynum)); //Adds the length of the line to the hash table as a G0 for inch calculator
                        if (G0Show)
                            g.DrawLine(BluePen, new PointF((float)(X * SCALE), (float)(Y * SCALE)),
                                    new PointF((float)((X + xnum) * SCALE), (float)((Y - ynum) * SCALE))); //Draws the G0 Line
                    }
                    else //If the G Command is not a G0
                    {
                        LINE_INCH = distance(X, Y, (X + xnum), (Y - ynum));
                        PointInches[NCList[PositionOfLine].POINT] += LINE_INCH; //Adds the length of the line to the hash table of Pointages for inch calculator

                        g.DrawLine(myPen, new PointF((float)(X * SCALE), (float)(Y * SCALE)),
                            new PointF((float)((X + xnum) * SCALE), (float)((Y - ynum) * SCALE))); //Draws the Line
                    }
                    X += xnum; //Update X
                    Y -= ynum; //Update Y
                }
            }
            this.Refresh(); //Refreshes the canvas
            g.Dispose(); //Releases any resources used by g
        }
        #endregion

        #region Trace Design
        /*
            * When the user traces the design and presses the right
            * or left arrow key, this function draws the selected line
            * using the previous NC Line to the current NC Line
        */
        
        private void TraceFunction(Graphics g)
        {
            //Redraws the main canvas
            Bitmap Canvas;
            using (FileStream stream = new FileStream(@CanvasCopy.FilePath, FileMode.Open, FileAccess.Read))
            {
                Canvas = (Bitmap)Bitmap.FromStream(stream);
            }
            g.DrawImage(Canvas, new Point(0, 0));

            if (NCList[Position].ContainsX || NCList[Position].ContainsY) //If a line contains X or Y
            {
                if (NCList[Position].X == 0 && NCList[Position].Y == 0)
                    return;
                ContainsXorY = false; //Used to determine if selection is the first move

                for (int i = Position - 1; i >= 0; i--) //Selects the previous line
                {
                    if (NCList[i].ContainsX || NCList[i].ContainsY) //If the previous line contains X or Y
                    {
                        ContainsXorY = true;
                        if (NCList[Position].ContainsI || NCList[Position].ContainsJ) //If the previous line contains I or J
                        {
                            DrawArc(NCList[i].Xabs, NCList[i].YabsInvert, Position, g, new Pen(Color.White)); //Draw the Arc
                            break;
                        }
                        else
                        {
                            //Draw the Line
                            g.DrawLine(new Pen(Color.White), new PointF((float)(NCList[Position].Xabs * SCALE),
                                (float)(NCList[Position].YabsInvert * SCALE)), new PointF((float)(NCList[i].Xabs * SCALE),
                                    (float)(NCList[i].YabsInvert * SCALE)));
                            break;
                        }
                    }
                }
                
                if (ContainsXorY == false) //If the previous line does not contain X or Y
                {
                    if (NCList[Position].ContainsI || NCList[Position].ContainsJ)
                        //Draw an arc from the origin to the selected line
                        DrawArc((Math.Abs(XMIN) * SCALE), (Math.Abs(YMAX) * SCALE), Position, g, new Pen(Color.White));
                    else
                        //Draw a line from the origin to the selected line
                        g.DrawLine(new Pen(Color.White), new PointF((float)(Math.Abs(XMIN) * SCALE),
                            (float)(Math.Abs(YMAX) * SCALE)), new PointF((float)(NCList[Position].Xabs * SCALE),
                                (float)(NCList[Position].YabsInvert * SCALE)));
                }
            }

            GCODE.Text = "GCODE: " + NCList[Position].GCODE + "\nSpeed: " + NCList[Position].Speed + "\nPoint: " +
            NCList[Position].POINT + "\nLine Number: " + NCList[Position].LineNumber;
        }

        #endregion
        /*
         * Timer to redraw the design
         */
        private void RedrawTimer_Tick(object sender, EventArgs e)
        {
            //If the position is less than or equal to the last line of the list
            if (Position <= NCList.Count - 1)
            {
                //Output the GCODE, Speed and Pointage
                GCODE.Text = "GCODE: " + NCList[Position].GCODE + "\nSpeed: " + NCList[Position].Speed + "\nPoint: " +
                NCList[Position].POINT + "\nLine Number: " + NCList[Position].LineNumber;
                draw_design(Position); //Redraw the design from the position
                Position++; //Incriment the position
            }
            else //If the design is done drawing (or the user stops the drawing)
            {
                drawingBar.Hide(); //Hide the trackbar
                drawingBarLabel.Hide(); //Hide the drawingBar label
                RedrawTimer.Stop(); //Stop the timer
                Locked = false; //Unlock the form
                this.Focus();
            }
        }

        /*
         * Timer for rainbow ProgramDetals label
         */
        private void RainbowTimer_Tick(object sender, EventArgs e)
        {
            //Creates a random color
            Random random = new Random();
            Color rand = Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
            //Changes the color of the text to this random color
            ProgramDetails.ForeColor = rand;
        }

        /*
        * Trackbar scroll event for when the design is being redrawn
        * Allows the user to speed up/slow down drawing of the design
        */
        private void drawingBar_Scroll(object sender, EventArgs e)
        {
            RedrawTimer.Interval = drawingBar.Value; //Set the RedrawTimer equal to the drawingBar value
        }

        /*
         * If the trackbar is being focused and a key is pressed, it gets passed to the main form keydown event
         */
        private void drawingBar_KeyDown(object sender, KeyEventArgs e)
        {
            this.Display_KeyDown(sender, e);
        }

        #region Draw Arc
        /*
         * DrawArc function
         */
        private double DrawArc(double XOldAbs, double YOldAbs, int NewPosition, Graphics g, Pen p)
        {
            //NOTE: An Arc is drawn by a rectangle (or in this case square), an arc start angle, and an arc sweep angle

            //Radius = Sqrt(I^2 + J^2)
            double Radius = (Math.Sqrt(Math.Pow(NCList[NewPosition].I, 2) + Math.Pow(NCList[NewPosition].J, 2)));

            double Xinc = NCList[NewPosition].X; //X incrimental
            double Yinc = NCList[NewPosition].Y; //Y incrimental
            double SSA = NCList[NewPosition].I; //I
            double SSB = NCList[NewPosition].J; //J

            //X0,Y0 centered around the origin (-I, J)
            double X0 = -SSA;
            double Y0 = SSB;

            double Xnew = Xinc - SSA;
            double Ynew = Yinc * -1 - SSB * -1;

            double ArcStart = Radians_To_Degrees(Math.Atan2(Y0, X0)); //Arc start angle
            double ArcEnd = Radians_To_Degrees(Math.Atan2(Ynew, Xnew)); //Arc end angle
            double SweepAngle = ArcEnd - ArcStart; //Arc sweep angle

            //If the G code is a 2 or 02, SweepAngle = SweepAngle + 360
            if (SweepAngle < 0 && (NCList[NewPosition].G == "2" || NCList[NewPosition].G == "02"))
                SweepAngle = SweepAngle + 360;
            //If the G code is a 3 or 03, SweepAngle = SweepAngle - 360
            else if (SweepAngle > 0 && (NCList[NewPosition].G == "3" || NCList[NewPosition].G == "03"))
                SweepAngle = SweepAngle - 360;

            //Width of the rectangle is equal to the 2 * radius
            double Width = 2 * Radius;
            //Since all the arcs are part of a circle, width is equal to height
            double Height = Width;

            double I_New = XOldAbs + NCList[NewPosition].I;
            double J_New = YOldAbs - NCList[NewPosition].J;
            PointF Start_Corner = new PointF((float)(I_New - (1 / 2.0 * Width)), (float)(J_New - (1 / 2.0 * Height))); //Upper left hand corner of the rectangle

            RectangleF myRect = new RectangleF((float)(Start_Corner.X * SCALE), (float)(Start_Corner.Y * SCALE),
                (float)(Width * SCALE), (float)(Height * SCALE)); //Creates the rectangle used to draw an arc

            g.DrawArc(p, myRect, (float)ArcStart, (float)SweepAngle); //Length of an arc used by inch calculator
            return SweepAngle;
        }
        #endregion
        #endregion

        #region Formulas
        /*
         * Distance formula for two lines
         */
        private double distance(double X1, double Y1, double X2, double Y2)
        {
            return Math.Sqrt(Math.Pow((X2 - X1), 2) + Math.Pow((Y2 - Y1), 2));
        }

        /*
         * Arc length formula
         */
        private double arc_length(double radius, double angle)
        {
            return (angle / 360) * Math.PI * (radius * 2);
        }

        /*
         * Converts radians to degrees
         */
        private double Radians_To_Degrees(double Radians)
        {
            return Radians * 180 / Math.PI;
        }
        #endregion

        #region Printing
        /*
         * Document Printpage Settings
         */
        private void Doc_PrintPage(object sender, PrintPageEventArgs e)
        {
            // MessageBox.Show("In Doc_PrintPage");

            Graphics g = Graphics.FromImage(mainCanvas);
            Bitmap Canvas;
            Font font = new Font("Arial", 12); //Sets the font to 12 Point Arial

            double New_XMax = Math.Abs(XMIN - XMAX);
            double New_YMax = Math.Abs(YMIN - YMAX);

            if (e.PageSettings.PrinterResolution.X < 0 || e.PageSettings.PrinterResolution.Y < 0)
            {
                MessageBox.Show("Printer Resolution not provided in dots per inch. Can't calculate to draw image. Have to use a different Printer.");
            }

            // First Scale in X. Then Scale in Y if it does not fit.
            SCALE = Math.Floor((e.PageSettings.PrinterResolution.X * 1.3) / New_XMax);
            //If the design is scaled in the X direction but goes out of bounds in the Y direction, it is scaled to YMAX
            // MessageBox.Show("Printable area: " + e.PageSettings.PrintableArea.Height + ", " + e.PageSettings.PrintableArea.Width);
            // Use X resolution as Y returns 0 in some cases.
            if (New_YMax * SCALE > e.PageSettings.PrinterResolution.X * 1.3)
            {
                SCALE = Math.Floor((e.PageSettings.PrinterResolution.X * 1.3) / New_YMax);
            }
            New_YMax *= SCALE + 1; //Add 1 for extra room
            New_XMax *= SCALE + 1;

            e.Graphics.DrawImage(TempBitMap, 10, 10, (int)(New_XMax), (int)(New_YMax)); //Draws the image on the print page
            // TempBitMap.Dispose(); //Releases all resources used by the temporary bitmap
            //e.Graphics.DrawImage(TempBitMap, 10, 10, (float)((e.PageSettings.PrinterResolution.X * .90) * mainCanvas.Width), (float)((e.PageSettings.PrinterResolution.Y * .70) * mainCanvas.Height));

            //Draws the design name and inch calculator on the print page
            e.Graphics.DrawString(this.Text + "\n\n" + Burn_Inches + "\n\n" + "Size of Design: " + SizeDesign, font,
                Brushes.Black, new PointF(10, (float)(New_YMax + 15)));

            //Creates bounds for the notes so the text wraps around the print page. Should not be absolute size.
            RectangleF Bounds = new RectangleF(new PointF(10, (float)(float)(New_YMax + 150)), new Size(800, 400));
            e.Graphics.DrawString("Notes: " + Notes, font, Brushes.Black, Bounds);

            //Redraws the main canvas
            using (FileStream stream = new FileStream(@CanvasCopy.FilePath, FileMode.Open, FileAccess.Read))
            {
                Canvas = (Bitmap)Bitmap.FromStream(stream);
            }
            g.DrawImage(Canvas, new Point(0, 0));
        }
        #endregion
    }
}

#region Temporary File Class
//Class to create a temporary file
public sealed class TemporaryFile : IDisposable
{
    public TemporaryFile() :
        this(Path.GetTempPath()) { }

    public TemporaryFile(string directory)
    {
        Create(Path.Combine(directory, Path.GetRandomFileName()));
    }

    ~TemporaryFile()
    {
        Delete();
    }

    public void Dispose()
    {
        Delete();
        GC.SuppressFinalize(this);
    }

    public string FilePath { get; private set; }

    private void Create(string path)
    {
        FilePath = path;
        using (File.Create(FilePath)) { };
    }

    private void Delete()
    {
        File.Delete(FilePath);
        FilePath = null;
    }
}
#endregion

#region Constants
static class Constants
{
    public const string Version = "1.12";
}
#endregion