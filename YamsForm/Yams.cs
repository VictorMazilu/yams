using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV;
using Emgu.CV.Features2D;
using System.IO.Pipes;
using Yams;
using YamsEvents;
using System.Xml.Linq;
using Emgu.CV.Linemod;
using System.Security.Cryptography;
using ZedGraph;
using OpenCvSharp.CPlusPlus;
using IronPython;
using IronPython.Hosting;
using System.Diagnostics;
using Microsoft.VisualBasic.ApplicationServices;
using static Community.CsharpSqlite.Sqlite3;
using System.Runtime.InteropServices;
using OpenCvSharp;
using static IronPython.Modules._ast;
using static System.Reflection.Metadata.BlobBuilder;
using static OpenCvSharp.CvHaarFeature;
using Microsoft.VisualBasic.Devices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Threading.Channels;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using System.Text.RegularExpressions;

namespace YamsForm
{

    

    public class MyEventArgs : EventArgs
    {
        public string Message { get; }

        public MyEventArgs(string message)
        {
            Message = message;
        }
    }

    public partial class Yams : Form
    {

        static Dictionary<int, (Emgu.CV.Mat, Emgu.CV.Mat)> computed = new Dictionary<int, (Emgu.CV.Mat, Emgu.CV.Mat)>();
        static string[] videoExtensions = { ".mp4" };

        static List<string> GetFiles(string path, string[] ext)
        {
            List<string> files = new List<string>();
            foreach (string f in Directory.GetFiles(path))
            {
                string extension = Path.GetExtension(f).ToLower();
                if (ext.Contains(extension))
                    files.Add(f);
            }
            return files;
        }

        static string directory = Directory.GetCurrentDirectory() + "\\resources" + "\\dices";
        string[] files = Directory.GetFiles(directory);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        public delegate void MyEventHandler(object sender, MyEventArgs e);

        public static event MyEventHandler MyEvent;

        // Method to raise the event
        public static void RaiseEvent(string pipsCount)
        {
            // Check if there are any subscribers to the event
            if (MyEvent != null)
            {
                // Create an instance of EventArgs
                MyEventArgs e = new MyEventArgs(pipsCount);

                // Raise the event
                MyEvent(new object(), e);
            }
        }

        public Yams()
        {
            InitializeComponent();
        }

        private void Yams_Load(object sender, EventArgs e)
        {

            AllocConsole();

            // Subscribe to the event
            MyEvent += MyEventHandlerMethod;
            RecognitionStart();

            for (int i = 0; i < Settings.noOfPlayers; i++) {
                dataGridView1.Columns.Add(Settings.PlayerNames[i], Settings.PlayerNames[i]);
            }
            for (int i = 0; i < 20; i++) {
                dataGridView1.Rows.Add(new DataGridViewTextBoxColumn());
            }
            dataGridView1[0, 0].Value = "1";
            dataGridView1[0, 1].Value = "2";
            dataGridView1[0, 2].Value = "3";
            dataGridView1[0, 3].Value = "4";
            dataGridView1[0, 4].Value = "5";
            dataGridView1[0, 5].Value = "6";
            dataGridView1[0, 6].Value = "Total";
            dataGridView1[0, 7].Value = "Double";
            dataGridView1[0, 8].Value = "1P";
            dataGridView1[0, 9].Value = "2P";
            dataGridView1[0, 10].Value = "3BUC + 10";
            dataGridView1[0, 11].Value = "Q+20";
            dataGridView1[0, 12].Value = "F+30";
            dataGridView1[0, 13].Value = "K+40";
            dataGridView1[0, 14].Value = "Y+50";
            dataGridView1[0, 15].Value = "MAX";
            dataGridView1[0, 16].Value = "MIN";
            dataGridView1[0, 17].Value = "Difference";
            dataGridView1[0, 18].Value = "Total";
            dataGridView1[0, 19].Value = "Grand Total";
            label1.Text = "label1";
        }
        // Event handler method
        void MyEventHandlerMethod(object sender, MyEventArgs e)
        {
            if (label1.InvokeRequired)
            {
                label1.Invoke(new MethodInvoker(delegate { label1.Text = e.Message; }));
            }
        }

        async void RecognitionStart()
        {
            Thread newThread = new Thread(RecognitionSystemMainLoop);
            newThread.Start();
        }

        void RecognitionSystemMainLoop() {
            string directory = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "resources", "dices");
            List<string> files = GetFiles(directory, new string[] { ".jpg", ".png", ".jpeg" });
            int i = 0;

            while (true)
            {
                computed = new Dictionary<int, (Emgu.CV.Mat, Emgu.CV.Mat)>();
                if (videoExtensions.Contains(Path.GetExtension(files[i]).ToLower()))
                {
                    VideoCapture cap = new VideoCapture(files[i]);
                    if (!cap.IsOpened)
                    {
                        i++;
                        continue;
                    }

                    while (true)
                    {
                        Emgu.CV.Mat frame = new Emgu.CV.Mat();
                        cap.Read(frame);

                        if (!frame.IsEmpty)
                        {
                            (Emgu.CV.Mat image, Emgu.CV.Mat resultImage, Emgu.CV.Mat dices) = RecognitionSystem.Recognize(Path.GetFileName(files[i]), frame);
                            Emgu.CV.CvInvoke.Imshow("original", image);
                            Emgu.CV.CvInvoke.Imshow("result", resultImage);
                            Emgu.CV.CvInvoke.Imshow("pits", dices);

                            if (Emgu.CV.CvInvoke.WaitKey(5) == 'q')
                                break;
                        }
                        else
                            break;
                    }

                    cap.Dispose();
                    i++;
                }
                using (VideoCapture capture = new VideoCapture(0))
                {
                    // Grab a single frame
                    Emgu.CV.Mat frame = new Emgu.CV.Mat();
                    capture.Grab();

                    // Retrieve the grabbed frame
                    capture.Retrieve(frame);

                    (Emgu.CV.Mat image, Emgu.CV.Mat resultImage, Emgu.CV.Mat dices) = RecognitionSystem.Recognize("videocapture", frame);
                    computed.Add(i, (image, dices));
                    Emgu.CV.CvInvoke.Imshow("original", image);
                    Emgu.CV.CvInvoke.Imshow("result", resultImage);
                    if (dices.GetData() != null)
                        Emgu.CV.CvInvoke.Imshow("pits", dices);
                }

                int key = Emgu.CV.CvInvoke.WaitKey(0) % 256;

                if (key == 13) //Enter key
                    continue;
                if (key == 'q' || key == 'Q')
                    break;
                else if (key == 'd' || key == 'D')
                    i = (i + 1) % files.Count;
                else if (key == 'a' || key == 'A')
                    i = (i - 1 + files.Count) % files.Count;
            }

            Emgu.CV.CvInvoke.DestroyAllWindows();
        }
    }

}
