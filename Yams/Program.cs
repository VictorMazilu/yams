using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

VideoCapture capture = new VideoCapture();

// Check if the capture is open
if (!capture.IsOpened)
{
    Console.WriteLine("Error: Unable to access the camera.");
    return;
}

// Set the capture frame width and height
capture.Set(Emgu.CV.CvEnum.CapProp.FrameWidth, 640);
capture.Set(Emgu.CV.CvEnum.CapProp.FrameHeight, 480);

// Create a window to display the camera feed
CvInvoke.NamedWindow("Dice Recognition");

while (true)
{
    // Capture a frame from the camera
    Mat frame = new Mat();
    capture.Read(frame);

    // Convert the frame to grayscale
    Mat grayFrame = new Mat();
    CvInvoke.CvtColor(frame, grayFrame, ColorConversion.Bgr2Gray);

    // Perform dice recognition processing here...

    // Apply Gaussian blur to reduce noise
    CvInvoke.GaussianBlur(grayFrame, grayFrame, new Size(5, 5), 0);

    // Perform thresholding to create a binary image
    Mat binaryImage = new Mat();
    CvInvoke.Threshold(grayFrame, binaryImage, 150, 255, ThresholdType.Binary);

    // Find contours in the binary image
    VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
    CvInvoke.FindContours(binaryImage, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

    int totalPipCount = 0;

    // Iterate through contours
    for (int i = 0; i < contours.Size; i++)
    {
        // Get the area of the contour
        double area = CvInvoke.ContourArea(contours[i]);

        // Check if the contour is a pip (based on area threshold)
        if (area > 10 && area < 100) // Adjust area thresholds as needed
        {
            totalPipCount++;
            // Draw a circle around the pip
            Rectangle boundingBox = CvInvoke.BoundingRectangle(contours[i]);
            CvInvoke.Circle(frame, new Point(boundingBox.X + boundingBox.Width / 2, boundingBox.Y + boundingBox.Height / 2), 5, new MCvScalar(0, 255, 0), 2);
        }
    }

    // Display the total pip count
    Console.WriteLine( totalPipCount.ToString());


    // Display the frame
    CvInvoke.Imshow("Dice Recognition", frame);

    // Wait for key press (press 'q' to exit)
    if (CvInvoke.WaitKey(1) == 'q')
        break;
}

// Release the camera capture and destroy the window
capture.Dispose();
CvInvoke.DestroyAllWindows();