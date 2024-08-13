using System.Windows.Automation;
using Microsoft.Test.Input;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace MapAutomator
{
    internal class Program
    {
        //We need to set the screen capture paramters. This caters for different screen resolutions and multi monitor setups.
        //We assume a full screen canvas (maximise capture area) however intend to leave 10px margin on all sides to remove risk of edge artefacts.
        //At a screen resolution of 3840 x 2160 (Effective canvas of 3820 x 2140), and a pan step size of 150px, we can expect to capture > 120 output tiles per capture tile.

        static int intScreenCanvasWidthPX = 3820;              //Note here we have already taken 20px off the actual canvas width - 10px on each side.
        static int intScreenCanvasHeightPX = 2140;             //Note here we have already taken 20px off the actual canvas height - 10px on each side.
        static int intScreenCanvasOffsetX = -956;              //This is the top left corner of the canvas relative to 0,0 (top left of primary display) for multi screen setups. Note this should be 10px margin off of left.
        static int intScreenCanvasOffsetY = -2150;             //This is the top left corner of the canvas relative to 0,0 (top left of primary display) for multi screen setups. Note this should be 10px margin off of top.
        static int intFocusClickX = 0;                         //This is the X coordinate to click to set focus on the browser window with the map. This is not the same as canvas coordinates due to resolution scaling in multimonitor setups.
        static int intFocusClickY = -100;                      //This is the Y coordinate to click to set focus on the browser window with the map. This is not the same as canvas coordinates due to resolution scaling in multimonitor setups.
        static int intUnfocusClickX = 1;                       //This is the X coordinate to move the mouse to after setting focus to the browser window with the map. So that the mouse does not get in the way of the capture.
        static int intUnfocusClickY = 1;                       //This is the Y coordinate to move the mouse to after setting focus to the browser window with the map. So that the mouse does not get in the way of the capture.
        static int intStepSizePX = 150;                        //This is the number of pixels the map moves with one keypress of a direction arrow (up / down / left / right). Default observed is 150px.

        static string strCaptureBatchName = "TestBatch";       //What is the name of this capture batch (including ZoomLevel)
        static int intInputTilesToCaptureX = 3;                //Given a starting point at the top left of required region, and selected zoom level, how many input frames to the right (including the first one) do we want to capture?
        static int intInputTilesToCaptureY = 2;                //Given a starting point at the top left of required region, and selected zoom level, how many input frames down (including the first one) do we want to capture?

        static void Main(string[] args)
        {
            //These variables are derived from input information above.

            int intAvailableStepsX = (int)Math.Floor((decimal)intScreenCanvasWidthPX / intStepSizePX);
            int intAvailableStepsY = (int)Math.Floor((decimal)intScreenCanvasHeightPX / intStepSizePX);
            int intExpectedColumnStitch = intScreenCanvasWidthPX - intAvailableStepsX * intStepSizePX;
            int intExpectedRowStitch = intScreenCanvasHeightPX - intAvailableStepsY * intStepSizePX;

            //Main Execution logic

            int intCurrRow = 1;
            int intCurrCol = 1;
            int intCurrRowCrop = 0;
            int intCurrColCrop = 0;
            string strLastXDirection = "Right";
            string strLastYDirection = "Down";
            string strCurrDirection = "Right";

            Bitmap printscreen1 = new Bitmap(intScreenCanvasWidthPX, intScreenCanvasHeightPX); Graphics graphics1 = Graphics.FromImage(printscreen1 as Image);
            Bitmap printscreen2 = new Bitmap(intScreenCanvasWidthPX, intScreenCanvasHeightPX); Graphics graphics2 = Graphics.FromImage(printscreen2 as Image);

            graphics1.CopyFromScreen(intScreenCanvasOffsetX, intScreenCanvasOffsetY, 0, 0, printscreen1.Size);
            printscreen1.Save("C:\\Temp\\" + strCaptureBatchName + "-" + String.Format("{0:D4}", intCurrCol) + "-" + String.Format("{0:D4}", intCurrRow) + ".png", ImageFormat.Png);

            while (true)
            {
                //Move
                if (strCurrDirection=="Right" || strCurrDirection == "Left")
                    {moveMap(intAvailableStepsX, strCurrDirection);}
                else
                    {moveMap(intAvailableStepsY, strCurrDirection);}

                //Capture, Crop & Save printscreen2. Set last rowcrop and columncrop
                graphics2.CopyFromScreen(intScreenCanvasOffsetX, intScreenCanvasOffsetY, 0, 0, printscreen2.Size);
                Rectangle cropRect = new Rectangle();

                if (strCurrDirection == "Right") //Cropping off left edge (expect small match value)
                {
                    int intCropOverlayEdgeAt = findStitchMatch(ref printscreen2, ref printscreen1, "Right", intExpectedColumnStitch);
                    cropRect = new Rectangle(intCropOverlayEdgeAt, strLastYDirection == "Down" ? intCurrRowCrop : 0, printscreen2.Width - intCropOverlayEdgeAt, strLastYDirection == "Down" ? printscreen2.Height - intCurrRowCrop : printscreen2.Height - intCurrRowCrop - 1);
                    strLastXDirection = "Right";
                    intCurrColCrop = intCropOverlayEdgeAt;
                    intCurrCol++;
                }
                else if (strCurrDirection == "Left") //Cropping off right edge (expect large match value)
                {
                    int intCropOverlayEdgeAt = findStitchMatch(ref printscreen2, ref printscreen1, "Left", printscreen2.Width - intExpectedColumnStitch);
                    cropRect = new Rectangle(0, strLastYDirection == "Down" ? intCurrRowCrop : 0, intCropOverlayEdgeAt - 1, strLastYDirection == "Down" ? printscreen2.Height - intCurrRowCrop : printscreen2.Height - intCurrRowCrop - 1);
                    strLastXDirection = "Left";
                    intCurrColCrop = intCropOverlayEdgeAt;
                    intCurrCol--;
                }
                else if (strCurrDirection == "Up") //Cropping off bottom edge (expect large match value)
                {
                    int intCropOverlayEdgeAt = findStitchMatch(ref printscreen2, ref printscreen1, "Top", printscreen2.Height - intExpectedRowStitch);
                    cropRect = new Rectangle(strLastXDirection == "Right" ? intCurrColCrop : 0, 0, strLastXDirection == "Right" ? printscreen2.Width - intCurrColCrop : printscreen2.Width - intCurrColCrop - 1, intCropOverlayEdgeAt - 1);
                    strLastYDirection = "Up";
                    intCurrRowCrop = intCropOverlayEdgeAt;
                    intCurrRow--;
                }
                else if (strCurrDirection == "Down") //Cropping off top edge (expect small match value)
                {
                    int intCropOverlayEdgeAt = findStitchMatch(ref printscreen2, ref printscreen1, "Bottom", intExpectedRowStitch);
                    cropRect = new Rectangle(strLastXDirection == "Right" ? intCurrColCrop : 0, intCropOverlayEdgeAt, strLastXDirection == "Right" ? printscreen2.Width - intCurrColCrop : printscreen2.Width - intCurrColCrop - 1, printscreen2.Height - intCropOverlayEdgeAt);
                    strLastYDirection = "Down";
                    intCurrRowCrop = intCropOverlayEdgeAt;
                    intCurrRow++;
                }

                saveCroppedTile(ref printscreen2, cropRect, "C:\\Temp\\" + strCaptureBatchName + "-" + String.Format("{0:D4}", intCurrCol) + "-" + String.Format("{0:D4}", intCurrRow) + ".png");

                //Shift printscreen2 to printscreen1
                printscreen1 = new Bitmap(printscreen2);

                //Set Next Move
                
                //Check Finishing Conditions
                if (strCurrDirection == "Right" && intCurrCol == intInputTilesToCaptureX && intCurrRow == intInputTilesToCaptureY) { break; }
                if (strCurrDirection == "Left" && intCurrCol == 1 && intCurrRow == intInputTilesToCaptureY) { break; }

                //Check based on last move being horizontal (either to left or right)
                if (strCurrDirection == "Right" && intCurrCol < intInputTilesToCaptureX) { strCurrDirection = "Right"; continue; }
                if (strCurrDirection == "Right" && intCurrCol == intInputTilesToCaptureX) { strCurrDirection = "Down"; continue; }
                if (strCurrDirection == "Left" && intCurrCol > 1) { strCurrDirection = "Left"; continue; }
                if (strCurrDirection == "Left" && intCurrCol == 1) { strCurrDirection = "Down"; continue; }

                //Check based on last move being vertical (basically down only for now)
                if (strCurrDirection == "Down" && intCurrCol == 1) { strCurrDirection = "Right"; continue; }
                if (strCurrDirection == "Down" && intCurrCol == intInputTilesToCaptureX) { strCurrDirection = "Left"; continue; }
            }
        }

        static void saveCroppedTile(ref Bitmap bmpSource, Rectangle cropRect, string strName)
        {
            using (Bitmap target = new Bitmap(cropRect.Width, cropRect.Height))
            {
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(bmpSource, new Rectangle(0, 0, target.Width, target.Height), cropRect, GraphicsUnit.Pixel);
                }

                target.Save(strName, ImageFormat.Png);
            }
        }

        static void moveMap(int intSteps, string strDirection)
        {
            Thread.Sleep(1000);

            Mouse.MoveTo(new Point(intFocusClickX, intFocusClickY));
            Mouse.Click(MouseButton.Left);
            Mouse.MoveTo(new Point(intUnfocusClickX, intUnfocusClickY));

            Thread.Sleep(1000);

            int intDelayBetween = 500;

            for (int i = 1; i <= intSteps; i++)
            {
                if (strDirection == "Right") { Keyboard.Press(Key.Right); Thread.Sleep(intDelayBetween); }  //Each Keypress moves by 150px
                if (strDirection == "Left") { Keyboard.Press(Key.Left); Thread.Sleep(intDelayBetween); }    //Each Keypress moves by 150px
                if (strDirection == "Up") { Keyboard.Press(Key.Up); Thread.Sleep(intDelayBetween); }        //Each Keypress moves by 150px
                if (strDirection == "Down") { Keyboard.Press(Key.Down); Thread.Sleep(intDelayBetween); }    //Each Keypress moves by 150px
            }

            Thread.Sleep(1000);
        }

        static int findStitchMatch(ref Bitmap bmpOverlay, ref Bitmap bmpBG, string strBGEdgeToStitch, int intOverlayMatchGuess, int intTolerance = 5)
        {
            //Note this function returns *ONE* based coordinate. So if the first column in the bitmap matches, it will return 1, not 0.

            int intCurrTarget = intOverlayMatchGuess - 1;
            int intOffsetDirection = -1;
            int intOffsetValue = 0;
            bool blnRecordedFail = false;

            int intPixelDifference = 0;
            int intImperfectPixels = 0;

            while (1 == 1)
            {
                if (strBGEdgeToStitch=="Right" || strBGEdgeToStitch == "Left")
                {
                    //This is a column stitch
                    for (int i = 0; i < bmpBG.Height; i++)
                    {
                        intPixelDifference = Math.Abs(bmpBG.GetPixel(strBGEdgeToStitch == "Left" ? 0 : bmpBG.Width - 1, i).R - bmpOverlay.GetPixel(intCurrTarget, i).R) +
                                             Math.Abs(bmpBG.GetPixel(strBGEdgeToStitch == "Left" ? 0 : bmpBG.Width - 1, i).G - bmpOverlay.GetPixel(intCurrTarget, i).G) +
                                             Math.Abs(bmpBG.GetPixel(strBGEdgeToStitch == "Left" ? 0 : bmpBG.Width - 1, i).B - bmpOverlay.GetPixel(intCurrTarget, i).B);

                        if (intPixelDifference > 0)
                        {
                            intImperfectPixels++;

                            if (intPixelDifference > 3)
                            {
                                intOffsetDirection = intOffsetDirection * -1;
                                if (intOffsetDirection == 1) { intOffsetValue++; }
                                blnRecordedFail = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    //This is a row stitch
                    for (int i = 0; i < bmpBG.Width; i++)
                    {
                        intPixelDifference = Math.Abs(bmpBG.GetPixel(i, strBGEdgeToStitch == "Top" ? 0 : bmpBG.Height - 1).R - bmpOverlay.GetPixel(i, intCurrTarget).R) +
                                             Math.Abs(bmpBG.GetPixel(i, strBGEdgeToStitch == "Top" ? 0 : bmpBG.Height - 1).G - bmpOverlay.GetPixel(i, intCurrTarget).G) +
                                             Math.Abs(bmpBG.GetPixel(i, strBGEdgeToStitch == "Top" ? 0 : bmpBG.Height - 1).B - bmpOverlay.GetPixel(i, intCurrTarget).B);

                        if (intPixelDifference > 0)
                        {
                            intImperfectPixels++;

                            if (intPixelDifference > 3)
                            {
                                intOffsetDirection = intOffsetDirection * -1;
                                if (intOffsetDirection == 1) { intOffsetValue++; }
                                blnRecordedFail = true;
                                break;
                            }
                        }
                    }
                }

                if (blnRecordedFail)
                {
                    blnRecordedFail = false;
                    intImperfectPixels = 0;
                    intCurrTarget = intOverlayMatchGuess + intOffsetDirection * intOffsetValue;

                    if (Math.Abs(intCurrTarget - intOverlayMatchGuess) > intTolerance)
                    {
                        Console.WriteLine("INFO: No matching column found within tolerance of (" + intTolerance + ").");
                        return 0;
                    }
                }
                else
                {
                    Console.WriteLine("INFO: Matched column is (" + (intCurrTarget+1) + ") with (" + intImperfectPixels + ") imperfect pixels.");
                    return intCurrTarget + 1;
                }
            }
        }
    }
}
