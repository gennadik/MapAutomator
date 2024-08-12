using System.Windows.Automation;
using Microsoft.Test.Input;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace MapAutomator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            /*
            Bitmap printscreen1 = new Bitmap(3584, 2048);
            Graphics graphics1 = Graphics.FromImage(printscreen1 as Image);

            graphics1.CopyFromScreen(-960, -2150, 0, 0, printscreen1.Size);
            printscreen1.Save(@"C:\Temp\Screen1.png", ImageFormat.Png);

            Thread.Sleep(1000);

            Mouse.MoveTo(new Point(0, -500));
            Mouse.Click(MouseButton.Left);
            Mouse.MoveTo(new Point(0, 500));

            Thread.Sleep(1000);

            Keyboard.Press(Key.Right); Thread.Sleep(500); Keyboard.Press(Key.Right); Thread.Sleep(500); //Each Keypress seems to move map by 150px, so 10 moves by 1500px.
            Keyboard.Press(Key.Right); Thread.Sleep(500); Keyboard.Press(Key.Right); Thread.Sleep(500);
            Keyboard.Press(Key.Right); Thread.Sleep(500); Keyboard.Press(Key.Right); Thread.Sleep(500);
            Keyboard.Press(Key.Right); Thread.Sleep(500); Keyboard.Press(Key.Right); Thread.Sleep(500);
            Keyboard.Press(Key.Right); Thread.Sleep(500); Keyboard.Press(Key.Right); Thread.Sleep(500);

            Thread.Sleep(1000);

            Bitmap printscreen2 = new Bitmap(3584, 2048);
            Graphics graphics2 = Graphics.FromImage(printscreen2 as Image);

            graphics2.CopyFromScreen(-960, -2150, 0, 0, printscreen2.Size);
            printscreen2.Save(@"C:\Temp\Screen2.png", ImageFormat.Png);
            */

            Bitmap printscreen1 = (Bitmap)Bitmap.FromFile(@"C:\Temp\Screen1.png");
            Bitmap printscreen2 = (Bitmap)Bitmap.FromFile(@"C:\Temp\Screen2.png");

            //So here we have printscreen1 & printscreen2 bitmaps. 2 has new data TO THE RIGHT of 1. So left edge of 2 is expected 1500px from left edge of 1. But burn at least 5px at the edge...
            //So Bitmap 2: Colum 5 = Bitmap 1: Column 1505

            int intResult = findColumnOffset(ref printscreen2, ref printscreen1, 1505);

            //Chop 1 at 1505
            //Start 2 at 6

        }

        static int findColumnOffset(ref Bitmap bmpOverlay, ref Bitmap bmpBG, int intGuess, int intTolerance = 100)
        {
            int intCurrCol = intGuess;
            int intOffsetDirection = -1;
            int intOffsetValue = 0;
            bool blnRecordedFail = false;

            int intPixelDifference = 0;
            int intImperfectPixels = 0;

            while (1 == 1)
            {
                for (int i = 1; i < bmpBG.Height; i++)
                {
                    intPixelDifference = Math.Abs(bmpBG.GetPixel(intCurrCol, i).R - bmpOverlay.GetPixel(5, i).R) +
                        Math.Abs(bmpBG.GetPixel(intCurrCol, i).G - bmpOverlay.GetPixel(5, i).G) +
                        Math.Abs(bmpBG.GetPixel(intCurrCol, i).B - bmpOverlay.GetPixel(5, i).B);

                    if (intPixelDifference > 0)
                    {
                        intImperfectPixels++;
                    }
                    else if (intPixelDifference > 3)
                    {
                        intOffsetDirection = intOffsetDirection * -1;
                        if (intOffsetDirection == 1) { intOffsetValue++; }
                        blnRecordedFail = true;
                        break;
                    }
                }

                if (blnRecordedFail)
                {
                    blnRecordedFail = false;
                    intImperfectPixels = 0;
                    intCurrCol = intGuess + intOffsetDirection * intOffsetValue;
                    if (Math.Abs(intCurrCol - intGuess) > intTolerance)
                    {
                        Console.WriteLine("INFO: No matching column found within tolerance of (" + intTolerance + ").");
                        return 0;
                    }
                }
                else
                {
                    Console.WriteLine("INFO: Matched column is (" + intCurrCol + ") with (" + intImperfectPixels + ") imperfect pixels.");
                    return intCurrCol;
                }
            }
        }
    }
}
