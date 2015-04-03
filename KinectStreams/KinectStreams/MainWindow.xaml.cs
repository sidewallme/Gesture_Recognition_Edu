using Microsoft.Kinect;
using Ventuz.OSC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KinectStreams
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        #region Members

        Mode _mode = Mode.Depth;

        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        IList<Body> _bodies;

        bool _displayBody = false;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion
        
        #region Event handlers
        const int port = 3334;
        //const string host = "141.142.21.24";
        const string host = "172.17.163.181";
        static UdpWriter writer;
        float max_head = 0;
        float max_body = 0;
        float min_head = 0;
        float min_body = 0;
        bool flag = true;
        double thre = 0.1;
        int timeInt = 0;
        public static long lastTimeStamp = DateTime.UtcNow.Ticks;

        private void sendTimeMessage(long lastTime, float min1, float max1)
        {
            
            long nowTimeStamp = DateTime.UtcNow.Ticks;
            if ((double)(nowTimeStamp - lastTimeStamp)/10000000.0 >= thre)
            {
                timeInt++;
                sendMessage(timeInt, min1, max1);
                lastTimeStamp = nowTimeStamp;
            }
            
        }
        private void sendMessage(int timeInt, float min1, float max1)
        {
            UdpWriter u = new UdpWriter(host, 3334);
            String one = timeInt + "," + min1 + "," + max1+ "\n";
            OscElement total = new OscElement("/test/squat", one);
           // OscElement min = new OscElement("/test/mindata", min1);
            u.Send(total);
            //u.Send(min);
        }

        private void sendMessageHub(float input, String op)
        {
            UdpWriter u = new UdpWriter(host, 3334);
            String one = input+"\n";
            OscElement total = new OscElement(op, one);
            u.Send(total);
        }

        private void sendMessageStop()
        {
            UdpWriter u = new UdpWriter(host, 3334);
            String one = "/stop" ;
            // "/squat/ypos"
            OscElement total = new OscElement(one, one);
            u.Send(total);
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (writer == null)
            {
                writer = new UdpWriter(host, port);
            }



            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            // Color
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Color)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Depth
            using (var frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Depth)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Infrared
            using (var frame = reference.InfraredFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Infrared)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Body
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    canvas.Children.Clear();

                    _bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(_bodies);
                    
                    foreach (var body in _bodies)
                    {
                        if (body != null)
                        {
                            if (body.IsTracked)
                            {
                               
                                Joint head = body.Joints[JointType.Head];
                                Joint handleft = body.Joints[JointType.HandLeft];
                                Joint handright = body.Joints[JointType.HandRight];
                                Joint spinemid = body.Joints[JointType.SpineMid];

                                //squatting data
                                float y1 = head.Position.Y;
                                float y2 = handleft.Position.Y;
                                float y3 = handright.Position.Y;

                                //punching data should, left
                                float zMid = spinemid.Position.Z;
                                float zleftp = handleft.Position.Z;
                                float zrightp = handright.Position.Z;

                                if (flag)
                                {
                                    min_head = y1;
                                    max_body = y1;
                                    flag = false;
                                }
                                System.Diagnostics.Debug.Write("z " + zleftp + "\n");
                                if (y1 > max_body)
                                {
                                    max_body = y1;
                                    //System.Diagnostics.Debug.Write("max " + max_body + "\n");
                                }
                                if (y2 > max_body)
                                {
                                    max_body = y2;
                                    //System.Diagnostics.Debug.Write("max " + max_body + "\n");
                                }
                                if (y3 > max_body)
                                {
                                    max_body = y3;
                                    //System.Diagnostics.Debug.Write("max " + max_body + "\n");
                                }
                                if (y1 < min_head)
                                {
                                    min_head = y1;
                                    //System.Diagnostics.Debug.Write("min " + min_head + "\n");
                                }
                                // Draw skeleton.
                                if (_displayBody)
                                {
                                    canvas.DrawSkeleton(body);
                                }

                                sendMessageHub(y1, "/squat/ypos");
                                sendMessageHub(max_body, "/squat/maxbody");
                                
                                sendMessageHub(zleftp, "/punch/left");
                                //sendMessageHub(zrightp, "/punch/right");
                            }
                        }
                    }
                }
            }
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Color;
        }

        private void Depth_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Depth;
        }

        private void Infrared_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Infrared;
        }

        private void Body_Click(object sender, RoutedEventArgs e)
        {
            _displayBody = !_displayBody;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            sendMessageStop();
        }

        #endregion
    }

    public enum Mode
    {   Depth,
        Infrared,
        Color
        
    }

    
}
