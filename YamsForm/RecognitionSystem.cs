using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Yams;
using YamsEvents;
using static System.Net.Mime.MediaTypeNames;
using ZedGraph;
using IronPython.Compiler.Ast;
using OpenCvSharp;
using Emgu.CV.Ocl;
using static IronPython.SQLite.PythonSQLite;
using Emgu.CV.Features2D;
using Microsoft.Scripting.Utils;
using OpenCvSharp.CPlusPlus;

namespace YamsForm
{
    public static class RecognitionSystem
    {
        public static Emgu.CV.Mat ResizeToSize(Emgu.CV.Mat img, int size)
        {
            Emgu.CV.CvInvoke.Resize(img, img, new System.Drawing.Size(size, size));
            return img;
        }

        public static Emgu.CV.Mat ResizeToImage(ref Image<Bgr, byte> img1, ref Image<Bgr, byte> img2)
        {
            Emgu.CV.CvInvoke.Resize(img1, img1, new Size(img2.Width, img2.Height), 1, 1, Inter.Linear);
            return img1.Mat;
        }

        public static Emgu.CV.Mat Resize(ref Image<Bgr, byte> sample, ref Image<Bgr, byte> image, int scale)
        {
            // check whether sample image and target image have the same dimensions
            if (image.Size == sample.Size)
            {
                Emgu.CV.CvInvoke.Resize(image, image, new System.Drawing.Size(scale, scale));
            }
            else
            {
                Emgu.CV.CvInvoke.Resize(image, image, new System.Drawing.Size(sample.Width, sample.Height), scale, scale, Inter.Linear);
            }

            //check whether image is in grayscale, BGR image
            //will have length of the shape equal to 3
            if (image.NumberOfChannels == 1)
            {
                Emgu.CV.CvInvoke.CvtColor(image, image, Emgu.CV.CvEnum.ColorConversion.Gray2Bgr);
            }

            return image.Mat;
        }

        public static Emgu.CV.Mat JoinImages(double scale, List<Emgu.CV.Mat> images, bool oneDimArr = true)
        {
            Emgu.CV.Mat result = new Emgu.CV.Mat();
            Emgu.CV.CvInvoke.HConcat(images.ToArray(), result);

            return result;
        }

        public static List<List<Emgu.CV.Mat>> MakeRows(ref List<Emgu.CV.Mat> images, int divider)
        {
            List<List<Emgu.CV.Mat>> newImages = new List<List<Emgu.CV.Mat>>();

            if (images.Count > 0)
            {
                int rows = images.Count / divider;
                if (images.Count % divider != 0)
                {
                    rows += 1;
                }

                Emgu.CV.Mat blank = new Emgu.CV.Mat();

                for (int i = 0; i < rows; i++)
                {
                    newImages.Add(new List<Emgu.CV.Mat>());
                    for (int j = 0; j < divider; j++)
                    {
                        newImages[i].Add(blank.Clone());
                    }
                }

                int row = 0;
                int col = 0;
                for (int i = 0; i < images.Count; i++)
                {
                    if (row < rows)
                    {
                        newImages[row][col] = images[i].Clone();
                        if (col == (divider - 1))
                        {
                            col = 0;
                            row += 1;
                        }
                        else
                        {
                            col += 1;
                        }
                    }
                }
            }

            return newImages;
        }

        public static (List<Emgu.CV.Mat>, List<Point>) GetContours(ref Emgu.CV.Mat img, ref Emgu.CV.Mat drawImg)
        {
            List<Image<Bgr, byte>> dices = new List<Image<Bgr, byte>>();
            List<Point> positions = new List<Point>();
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                Emgu.CV.CvInvoke.FindContours(img, contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);

                using (Emgu.CV.Mat imageForCropping = drawImg.Clone())
                {
                    for (int i = 0; i < contours.Size; i++)
                    {
                        double area = Emgu.CV.CvInvoke.ContourArea(contours[i]);
                        if (area >= 1200 && area <= 10000)
                        {
                            RotatedRect rect = Emgu.CV.CvInvoke.MinAreaRect(contours[i]);
                            PointF[] box = rect.GetVertices();

                            int width = (int)rect.Size.Width;
                            int height = (int)rect.Size.Height;

                            if (Math.Abs(width - height) <= 40)
                            {
                                PointF[] sourcePoints = box;
                                PointF[] targetPoints = new PointF[]
                                {
                                    new PointF(0, height - 1),
                                    new PointF(0, 0),
                                    new PointF(width - 1, 0),
                                    new PointF(width - 1, height - 1)
                                };
                                // Compute the perspective transformation matrix
                                Emgu.CV.Mat perspectiveMatrix = Emgu.CV.CvInvoke.GetPerspectiveTransform(sourcePoints, targetPoints);

                                // Warp the image using the perspective transformation matrix
                                Emgu.CV.Mat warpedImage = new Emgu.CV.Mat();
                                Emgu.CV.CvInvoke.WarpPerspective(img, warpedImage, perspectiveMatrix, new Size(80, 80), Inter.Linear, Warp.Default, Emgu.CV.CvEnum.BorderType.Constant, new MCvScalar(0));

                                Emgu.CV.CvInvoke.Imshow("[warped]", warpedImage);
                                dices.Add(warpedImage.ToImage<Bgr, byte>());
                                positions.Add(new Point((int)box[0].X, (int)box[0].Y));

                                Emgu.CV.CvInvoke.DrawContours(drawImg, contours, i, new MCvScalar(0, 255, 0), 2);

                                //using (Matrix<float> perspectiveTransform = new Matrix<float>(Emgu.CV.CvInvoke.GetPerspectiveTransform(sourcePoints, targetPoints).Size))
                                //{
                                //    Emgu.CV.Mat warped = new Emgu.CV.Mat();
                                //    Emgu.CV.CvInvoke.WarpPerspective(imageForCropping, warped, perspectiveTransform, new Size(width, height), Inter.Linear, Warp.Default, Emgu.CV.CvEnum.BorderType.Constant, new MCvScalar(0, 0, 0));

                                //    ///debug
                                //    Emgu.CV.CvInvoke.Imshow("[contour]",warped);

                                //    dices.Add(warped.ToImage<Bgr, byte>());
                                //    positions.Add(new Point((int)box[0].X, (int)box[0].Y));

                                //    Emgu.CV.CvInvoke.DrawContours(drawImg, contours, i, new MCvScalar(0, 255, 0), 2);
                                //}
                            }
                        }
                    }
                }
            }
            List<Emgu.CV.Mat> matDices = new List<Emgu.CV.Mat>();
            for (int i = 0; i < dices.Count; i++)
            {
                matDices.Add(dices[i].Mat);
            }
            return (matDices, positions);

            //// Find contours
            //VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            //Emgu.CV.Mat hierarchy = new Emgu.CV.Mat();
            //Emgu.CV.CvInvoke.FindContours(img, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxSimple);

            //// Iterate through contours and calculate positions
            //List<Point> contourPositions = new List<Point>();

            //for (int i = 0; i < contours.Size; i++)
            //{
            //    VectorOfPoint contour = contours[i];
            //    Moments moments = Emgu.CV.CvInvoke.Moments(contour);

            //    // Calculate centroid of the contour
            //    int cx = (int)(moments.M10 / moments.M00);
            //    int cy = (int)(moments.M01 / moments.M00);
            //    Point centroid = new Point(cx, cy);

            //    double area = Emgu.CV.CvInvoke.ContourArea(contours[i]);
            //    if ( area >= 1200)
            //        contourPositions.Add(centroid);
            //}

            //// Iterate through contours and extract images inside the contours
            //List<Emgu.CV.Mat> contourImages = new List<Emgu.CV.Mat>();

            //for (int i = 0; i < contours.Size; i++)
            //{
            //    // Create a mask for each contour
            //    Emgu.CV.Mat mask = new Emgu.CV.Mat(img.Size, DepthType.Cv8U, 1);
            //    mask.SetTo(new MCvScalar(0));

            //    // Draw the contour on the mask
            //    Emgu.CV.CvInvoke.DrawContours(mask, contours, i, new MCvScalar(255), -1);

            //    // Extract the region of interest (ROI) using the mask
            //    Emgu.CV.Mat roi = new Emgu.CV.Mat();
            //    img.CopyTo(roi, mask);

            //    // Convert the ROI to Image<Bgr, byte> format
            //    Emgu.CV.Mat contourImage = roi;//.ToImage<Bgr, byte>();

            //    Emgu.CV.Mat binaryImage = new Emgu.CV.Mat();
            //    Emgu.CV.CvInvoke.Threshold(contourImage, binaryImage, 127, 255, Emgu.CV.CvEnum.ThresholdType.Binary);

            //    double area = Emgu.CV.CvInvoke.ContourArea(binaryImage);
            //    if(area >= 1200)
            //        contourImages.Add(contourImage);
            //}
            //return (contourImages, contourPositions);
            // Display the contours and their positions
            //for (int i = 0; i < contourPositions.Count; i++)
            //{
            //    Point position = contourPositions[i];
            //    //Console.WriteLine("Contour " + i + " position: (" + position.X + ", " + position.Y + ")");
            //}
        }

        public static Emgu.CV.Mat OpenImage(string file)
        {
            string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "dices");
            directoryPath = Path.GetFullPath(directoryPath);

            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine("no directory: " + directoryPath);
                Environment.Exit(1);
            }

            string filePath = Path.Combine(directoryPath, file);
            if (!File.Exists(filePath))
            {
                Console.WriteLine("no file: " + file + "\nat: " + filePath);
                Environment.Exit(1);
            }

            Emgu.CV.Mat img = null;
            try
            {
                img = Emgu.CV.CvInvoke.Imread(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can't open file: " + filePath);
                Environment.Exit(1);
            }

            return img;
        }

        public static (Emgu.CV.Mat, int) SimpleBlobDetection(ref Emgu.CV.Mat img, double minThreshold, double maxThreshold, double minArea, double maxArea, double minCircularity, double minInertiaRatio)
        {

            // Set up parameters for SimpleBlobDetector
            SimpleBlobDetectorParams blobParams = new SimpleBlobDetectorParams();
            blobParams.FilterByArea = true;
            blobParams.MinArea = 100; // Minimum blob area
            blobParams.MaxArea = 2000; // Maximum blob area
            blobParams.FilterByCircularity = true;
            blobParams.MinCircularity = 0.7f; // Minimum circularity
            blobParams.FilterByConvexity = false;
            blobParams.FilterByInertia = false;
            blobParams.blobColor = 255;

            // Create a SimpleBlobDetector
            SimpleBlobDetector detector = new SimpleBlobDetector();

            // Detect keypoints (blobs) in the grayscale image
            MKeyPoint[] keypoints = detector.Detect(img);

            Emgu.CV.Mat imgWithKeypoints = img.Clone();
            VectorOfKeyPoint vectorOfKeyPoints = new VectorOfKeyPoint();
            vectorOfKeyPoints.Push(keypoints);
            Features2DToolbox.DrawKeypoints(img, vectorOfKeyPoints, imgWithKeypoints, new Bgr(0, 255, 0), Features2DToolbox.KeypointDrawType.Default);

            return (imgWithKeypoints, keypoints.Count());
        }

        public static Emgu.CV.Mat ApplyGamma(Emgu.CV.Mat img, double gamma)
        {
            double invGamma = 1.0 / gamma;
            byte[] table = new byte[256];

            for (int i = 0; i < 256; i++)
            {
                table[i] = (byte)(Math.Pow(i / 255.0, invGamma) * 255);
            }

            Emgu.CV.Mat lut = new Emgu.CV.Mat(1, 256, DepthType.Cv8U, 1);
            lut.SetTo(table);

            Emgu.CV.Mat result = new Emgu.CV.Mat();
            Emgu.CV.CvInvoke.LUT(img, lut, result);

            return result;
        }
        public static Emgu.CV.Mat ProcessImage(Emgu.CV.Mat img, double gamma, Emgu.CV.Mat kernel, Emgu.CV.CvEnum.ThresholdType method)
        {
            // Convert the image to grayscale
            Emgu.CV.Mat imgGray = new Emgu.CV.Mat();
            Emgu.CV.CvInvoke.CvtColor(img, imgGray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

            // Apply gamma correction
            Emgu.CV.Mat imgGammaApplied = ApplyGamma(imgGray, gamma);
            //Emgu.CV.Mat imgGammaApplied = imgGray;

            // Apply Gaussian blur
            Emgu.CV.Mat imgBlur = new Emgu.CV.Mat();
            Emgu.CV.CvInvoke.GaussianBlur(imgGammaApplied, imgBlur, new Size(3, 3), 8);

            // Thresholding
            Emgu.CV.Mat imgThreshold = new Emgu.CV.Mat();
            Emgu.CV.CvInvoke.Threshold(imgBlur, imgThreshold, 0, 255, method);
            //imgThreshold = imgBlur;

            // Morphological operations
            Emgu.CV.Mat imgMorph = new Emgu.CV.Mat();
            Emgu.CV.CvInvoke.MorphologyEx(imgThreshold, imgMorph, MorphOp.Close, kernel, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar());
            Emgu.CV.CvInvoke.MorphologyEx(imgMorph, imgMorph, MorphOp.Open, kernel, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar());

            // Canny edge detection
            Emgu.CV.Mat imgCanny = new Emgu.CV.Mat();
            Emgu.CV.CvInvoke.Canny(imgMorph, imgCanny, 1, 255);

            // Dilation
            Emgu.CV.Mat dilated = new Emgu.CV.Mat();
            Emgu.CV.CvInvoke.Dilate(imgCanny, dilated, kernel, new Point(-1, -1), 2, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar());

            return imgThreshold;
        }
        public static (Emgu.CV.Mat, Emgu.CV.Mat) Recognize(string fileName, Emgu.CV.Mat img = null)
        {
            // Blob detection parameters
            double minThreshold = 50;
            double maxThreshold = 200;
            double minArea = 60;
            double maxArea = 1000;
            double minCircularity = 0.4;
            double minInertiaRatio = 0.4;

            int totalPips = 0;
            int totalDices = 0;

            if (img == null)
            {
                img = OpenImage(fileName);
            }

            Emgu.CV.Mat kernel = Emgu.CV.CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(3, 3),new Point(-1,-1));

            Emgu.CV.Mat output = ProcessImage(img, 0.5, kernel, Emgu.CV.CvEnum.ThresholdType.Binary | Emgu.CV.CvEnum.ThresholdType.Otsu);
            
            Emgu.CV.Mat resultImage = img.Clone();
            List<Emgu.CV.Mat> dices = new List<Emgu.CV.Mat>();
            List<Point> positions = new List<Point>();

            (dices, positions) = GetContours(ref output, ref resultImage);
            totalDices = dices.Count;

            if (totalDices == 0 || totalDices == 1)
            {
                output = ProcessImage(img, 0.2, kernel, Emgu.CV.CvEnum.ThresholdType.Triangle);
                resultImage = img.Clone();
                (dices, positions) = GetContours(ref output, ref resultImage);
                totalDices = dices.Count;
            }

            Emgu.CV.CvInvoke.PutText(resultImage, "Number of dices: " + totalDices, new Point(30, 30), Emgu.CV.CvEnum.FontFace.HersheyComplex, 1, new MCvScalar(0, 0, 255), 2);
            List<Emgu.CV.Mat> filteredDices = new List<Emgu.CV.Mat>();

            if (totalDices > 0)
            {
                foreach (var dice in dices)
                {
                    
                    Emgu.CV.Mat resizedDice = ResizeToSize(dice, 128);

                    double imgArea = resizedDice.Width * resizedDice.Height;
                    maxArea = (int)(imgArea / 2);
                    Emgu.CV.Mat diceGamma = ApplyGamma(resizedDice, 0.6);
                    Emgu.CV.Mat diceMorph = new Emgu.CV.Mat();
                    Emgu.CV.CvInvoke.MorphologyEx(diceGamma, diceMorph, MorphOp.Close, kernel, new Point(-1,-1), 1, Emgu.CV.CvEnum.BorderType.Constant, new MCvScalar(0, 0, 0));
                    (Emgu.CV.Mat imgWithKeypoints, int number) = SimpleBlobDetection(ref diceMorph, minThreshold, maxThreshold, minArea, maxArea, minCircularity, minInertiaRatio);

                    if (number == 0)
                    {
                        diceGamma = ApplyGamma(resizedDice, 0.18);
                        //Emgu.CV.CvInvoke.MorphologyEx(diceGamma,diceMorph, MorphOp.Close, kernel, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Constant, new MCvScalar(0, 0, 0));
                        //Emgu.CV.CvInvoke.MorphologyEx(diceMorph, diceMorph, MorphOp.Open, kernel, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Constant, new MCvScalar(0, 0, 0));
                        (imgWithKeypoints, number) = SimpleBlobDetection(ref diceGamma, minThreshold, maxThreshold, minArea, maxArea, minCircularity, minInertiaRatio);
                    }

                    Emgu.CV.CvInvoke.PutText(imgWithKeypoints, number.ToString(), new Point(5, 25), Emgu.CV.CvEnum.FontFace.HersheyComplex, 1, new MCvScalar(0, 0, 255), 2);
                    Emgu.CV.CvInvoke.PutText(resultImage, number.ToString(), positions[filteredDices.Count], Emgu.CV.CvEnum.FontFace.HersheyComplex, 1, new MCvScalar(0, 0, 255), 2);

                    filteredDices.Add(imgWithKeypoints);
                    totalPips += number;
                }
            }

            if (totalPips > 0)
            {
                Emgu.CV.CvInvoke.PutText(img, "Filename: " + fileName, new Point(30, 30), Emgu.CV.CvEnum.FontFace.HersheyComplex, 1, new MCvScalar(0, 0, 255), 2);
                Emgu.CV.Mat full = JoinImages(0.6, new List<Emgu.CV.Mat> { img, resultImage }, true);

                if (filteredDices.Count > 10)
                {
                    //filteredDices = MakeRows(filteredDices, 10);
                    Emgu.CV.Mat dicesImage = JoinImages(0.5, filteredDices, false);
                    return (full, dicesImage);
                }
                else
                {
                    Emgu.CV.Mat dicesImage = JoinImages(0.5, filteredDices, true);
                    return (full, dicesImage);
                }
            }
            else
            {
                Emgu.CV.Mat copy = img.Clone();
                Emgu.CV.CvInvoke.PutText(img, "Filename: " + fileName, new Point(30, 30), Emgu.CV.CvEnum.FontFace.HersheyComplex, 1, new MCvScalar(0, 0, 255), 2);
                Emgu.CV.CvInvoke.PutText(copy, "Number of dices: 0", new Point(30, 30), Emgu.CV.CvEnum.FontFace.HersheyComplex, 1, new MCvScalar(0, 0, 255), 2);
                Emgu.CV.Mat blankDice = new Emgu.CV.Mat();
                Emgu.CV.Mat full = JoinImages(0.6, new List<Emgu.CV.Mat> { img, copy }, true);
                Emgu.CV.Mat dicesImage = JoinImages(0.5, new List<Emgu.CV.Mat> { blankDice, blankDice }, true);
                return (full, dicesImage);
            }
        }
    }
}
