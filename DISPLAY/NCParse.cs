using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DISPLAY
{
    public class NCParse
    {
        private static double XCalcAbs;
        private static double YCalcAbs;
        private static int LineNumberCalc;

        public readonly double Xabs;
        public readonly double Yabs;
        public readonly int LineNumber;
        public readonly string GCODE;
        public readonly double X;
        public readonly double Y;
        public readonly double I;
        public readonly double J;

        public readonly bool ContainsX;
        public readonly bool ContainsY;
        public readonly bool ContainsI;
        public readonly bool ContainsJ;
        public readonly bool ContainsG;
        public readonly bool SnugFeed;
        public readonly bool WideFeed;
        public readonly bool Buffer;

        public bool IsSelected;

        public readonly string G;
        public static string COMMENT;
        public readonly string COMPENSATION;
        private static string OLD_POINTAGE;
        public readonly string POINT;

        public double YabsInvert;
        public string Speed;

        public NCParse()
        {
            POINT = null;
            GCODE = null;
            IsSelected = false;
            ContainsX = false;
            ContainsY = false;
            ContainsI = false;
            ContainsJ = false;
            ContainsG = false;
            SnugFeed = false;
            WideFeed = false;
            X = 0;
            Y = 0;
            I = 0;
            J = 0;
            G = null;
            COMMENT = null;
            COMPENSATION = null;
        }

        public NCParse(string NC_Line)
        {
            GCODE = NC_Line;
            IsSelected = false;
            ContainsX = false;
            ContainsY = false;
            ContainsJ = false;
            ContainsI = false;
            ContainsG = false;
            SnugFeed = false;
            WideFeed = false;

            if (NC_Line == "" || NC_Line == null)
                return;

            string[] NC_line_array = NC_Line.Split(' ');
            foreach (string i in NC_line_array)
            {
                //Gets the values of G, X, Y, I and J
                switch (i[0])
                {
                    case 'G':
                        G = i.Substring(1); //Returns a G command in the form of 0, 1, 2, 72, etc.
                        ContainsG = true;
                        break;
                    case 'X':
                        X = double.Parse(i.Substring(1));
                        ContainsX = true;
                        break;
                    case 'Y':
                        Y = double.Parse(i.Substring(1));
                        ContainsY = true;
                        break;
                    case 'I':
                        I = double.Parse(i.Substring(1));
                        ContainsI = true;
                        break;
                    case 'J':
                        J = double.Parse(i.Substring(1));
                        ContainsJ = true;
                        break;
                    case '!':
                        COMMENT = i.Substring(0);  //Returns comment in the form !=SPEED, etc.
                        break;
                    default:
                        break;
                }
                switch (COMMENT)
                {
                    case "!=WIDE":
                        this.WideFeed = true;
                        break;
                    case "!=U":
                        this.SnugFeed = true;
                        break;
                    case "!=COMMENT":
                        this.Buffer = true;
                        break;
                    case "!=NORMAL":
                        this.Speed = "Normal";
                        break;
                    case "!=SLOW":
                        this.Speed = "Slow";
                        break;
                    case "!=MEDIUM":
                        this.Speed = "Medium";
                        break;
                    case "!=FAST":
                        this.Speed = "Fast";
                        break;
                    case "!1":
                        this.POINT = "1 Pass";
                        break;
                    case "!ETCHING":
                        this.POINT = "ETCHING";
                        break;
                    case "!1.5":
                        this.POINT = "1.5 Point";
                        break;
                    case "!2":
                        this.POINT = "2 Point";
                        break;
                    case "!PULSING":
                        this.POINT = "PULSING";
                        break;
                    case "!3":
                        this.POINT = "3 Point";
                        break;
                    case "!4":
                        this.POINT = "4 Point";
                        break;
                    case "!6":
                        this.POINT = "6 Point";
                        break;
                    case "!8":
                        this.POINT = "8 Point";
                        break;
                    default:
                        break;
                }
                //Compensation in the NC file will be in the form !=Compensation /comp where /comp is /nc, /cl, /cr
                //Switch will find the case when a /comp is found
                switch (i)
                {
                    case "/nc":
                        COMPENSATION = "/nc";
                        break;
                    case "/cl":
                        COMPENSATION = "/cl";
                        break;
                    case "/cr":
                        COMPENSATION = "/cr";
                        break;
                    default:
                        break;
                }
            }
            if (this.ContainsX || this.ContainsY)
            {
                XCalcAbs += this.X;
                Xabs = XCalcAbs;
                YCalcAbs += this.Y;
                Yabs = YCalcAbs;
            }
            if (this.POINT == null)
            {
                this.POINT = OLD_POINTAGE;
            }
            else
                OLD_POINTAGE = this.POINT;

            if (this.Speed == null && this.G != null)
            {
                this.Speed = "Normal (Assumed)";
            }

            LineNumberCalc += 1;
            LineNumber = LineNumberCalc;
        }
    }
}
