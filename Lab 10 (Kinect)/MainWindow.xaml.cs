//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.DepthBasics
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using Microsoft.Kinect;
    using Brushes = System.Drawing.Brushes;
    using Point = System.Drawing.Point;


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

        private WriteableBitmap rgbBitmap;

        private WriteableBitmap skeletonBitmap;

        /// <summary>
        /// Intermediate storage for the depth data received from the camera
        /// </summary>
        private DepthImagePixel[] depthPixels;

        

        /// <summary>
        /// Intermediate storage for the depth data converted to color
        /// </summary>
        private byte[] colorPixels;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the depth stream to receive depth frames
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

              
                // Turn on the rgb stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                // Turn on the rgb stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();
                
                // Allocate space to put the depth pixels we'll receive
                this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];

                
                

                // Allocate space to put the color pixels we'll create
                this.colorPixels = new byte[this.sensor.DepthStream.FramePixelDataLength * sizeof(int)];

                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth, this.sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                // This is the rgb bitmap we'll display on-screen
                this.rgbBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                //this.skeletonBitmap = new WriteableBitmap(this.sensor.SkeletonStream., this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                // Set the image we display to point to the bitmap where we'll put the image data
                this.Image.Source = this.colorBitmap;
                this.Image2.Source = this.rgbBitmap;
                //this.Image3.Source = this.skeletonBitmap;

                // Add an event handler to be called whenever there is new depth frame data
                this.sensor.DepthFrameReady += this.SensorDepthFrameReady;
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    
                    // Copy the pixel data from the image to a temporary array
                    depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);

                    // Get the min and max reliable depth for the current frame
                    int minDepth = depthFrame.MinDepth;
                    int maxDepth = depthFrame.MaxDepth;
                
                    // Convert the depth to RGB
                    int colorPixelIndex = 0;
                   
                    int mi = 1000;
                    int mx = 0;
                    for (int i = 0; i < this.depthPixels.Length; ++i)
                    {
                        // Get the depth for this pixel
                        short depth = depthPixels[i].Depth;
                        if (depth < mi)
                        {
                            mi = depth;
                        }
                        if (depth > mx)
                        {
                            mx = depth;
                        }


                        // To convert to a byte, we're discarding the most-significant
                        // rather than least-significant bits.
                        // We're preserving detail, although the intensity will "wrap."
                        // Values outside the reliable depth range are mapped to 0 (black).

                        // Note: Using conditionals in this loop could degrade performance.
                        // Consider using a lookup table instead when writing production code.
                        // See the KinectDepthViewer class used by the KinectExplorer sample
                        // for a lookup table example.
                        if (depth < 1000)
                        {
                            this.colorPixels[colorPixelIndex++] = 232;

                            // Write out green byte
                            this.colorPixels[colorPixelIndex++] = 232;

                            // Write out red byte                        
                            this.colorPixels[colorPixelIndex++] = 232;
                            ++colorPixelIndex;
                        }

                       else if (depth < 2000)
                        {
                            this.colorPixels[colorPixelIndex++] = 232;

                            // Write out green byte
                            this.colorPixels[colorPixelIndex++] = 32;

                            // Write out red byte                        
                            this.colorPixels[colorPixelIndex++] = 232;
                            ++colorPixelIndex;
                        }
                        else if (depth < 3000)
                        {
                            this.colorPixels[colorPixelIndex++] = 142;

                            // Write out green byte
                            this.colorPixels[colorPixelIndex++] = 132;

                            // Write out red byte                        
                            this.colorPixels[colorPixelIndex++] = 32;
                            ++colorPixelIndex;
                        }
                        else if (depth < 4000)
                        {
                            this.colorPixels[colorPixelIndex++] = 255;

                            // Write out green byte
                            this.colorPixels[colorPixelIndex++] = 255;

                            // Write out red byte                        
                            this.colorPixels[colorPixelIndex++] = 102;
                            ++colorPixelIndex;
                        }
                        else if(depth < 5000)
                        {
                            this.colorPixels[colorPixelIndex++] = 242;

                            // Write out green byte
                            this.colorPixels[colorPixelIndex++] = 132;

                            // Write out red byte                        
                            this.colorPixels[colorPixelIndex++] = 172;
                            ++colorPixelIndex;
                        }
                        else if (depth < 6000)
                        {
                            this.colorPixels[colorPixelIndex++] = 192;

                            // Write out green byte
                            this.colorPixels[colorPixelIndex++] = 255;

                            // Write out red byte                        
                            this.colorPixels[colorPixelIndex++] = 160;
                            ++colorPixelIndex;
                        }
                        else if (depth <7000)
                        {
                            this.colorPixels[colorPixelIndex++] = 45;

                            // Write out green byte
                            this.colorPixels[colorPixelIndex++] = 255;

                            // Write out red byte                        
                            this.colorPixels[colorPixelIndex++] = 255;
                            ++colorPixelIndex;
                        }

                        else if (depth < 8000)
                        {
                            this.colorPixels[colorPixelIndex++] = 202;

                            // Write out green byte
                            this.colorPixels[colorPixelIndex++] = 102;

                            // Write out red byte                        
                            this.colorPixels[colorPixelIndex++] = 255;
                            ++colorPixelIndex;
                        }

                        else if (depth < 9000)
                        {
                            this.colorPixels[colorPixelIndex++] = 162;

                            // Write out green byte
                            this.colorPixels[colorPixelIndex++] = 172;

                            // Write out red byte                        
                            this.colorPixels[colorPixelIndex++] = 222;
                            ++colorPixelIndex;
                        }


                        else if (depth < 10000)
                        {
                            this.colorPixels[colorPixelIndex++] = 052;

                            // Write out green byte
                            this.colorPixels[colorPixelIndex++] = 137;

                            // Write out red byte                        
                            this.colorPixels[colorPixelIndex++] = 230;
                            ++colorPixelIndex;
                        }


                        else if (depth < 11000)
                        {
                            this.colorPixels[colorPixelIndex++] = 76;

                            // Write out green byte
                            this.colorPixels[colorPixelIndex++] = 252;

                            // Write out red byte                        
                            this.colorPixels[colorPixelIndex++] = 182;
                            ++colorPixelIndex;
                        }

                        else if (depth < 12000)
                        {
                            this.colorPixels[colorPixelIndex++] = 32;

                            // Write out green byte
                            this.colorPixels[colorPixelIndex++] = 240;

                            // Write out red byte                        
                            this.colorPixels[colorPixelIndex++] = 247;
                            ++colorPixelIndex;

                        }
                        else if (depth < 13000)
                        {
                            this.colorPixels[colorPixelIndex++] = 32;

                            // Write out green byte
                            this.colorPixels[colorPixelIndex++] = 210;

                            // Write out red byte                        
                            this.colorPixels[colorPixelIndex++] = 147;
                            ++colorPixelIndex;

                        }



                        else
                        {
                            byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                            // Write out blue byte
                            this.colorPixels[colorPixelIndex++] = intensity;

                            // Write out green byte
                            this.colorPixels[colorPixelIndex++] = intensity;

                            // Write out red byte                        
                            this.colorPixels[colorPixelIndex++] = intensity;

                            // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                            // If we were outputting BGRA, we would write alpha here.
                            ++colorPixelIndex;
                        }
                    
                    }
                    
                    
                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                    
                }
            }
        }
      
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame rgbFrame = e.OpenColorImageFrame())
            {
                if (rgbFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    byte[] pixeldata = new byte[rgbFrame.PixelDataLength];

                    rgbFrame.CopyPixelDataTo(pixeldata);
                    
                    
                    
                    this.rgbBitmap.WritePixels(
                        new Int32Rect(0, 0, this.rgbBitmap.PixelWidth, this.rgbBitmap.PixelHeight),
                        pixeldata,
                        this.rgbBitmap.PixelWidth * sizeof(int),
                        0);
                    return;
                    
                }
            }
        }
        private Skeleton[] skeletons;
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                
                if (skeletonFrame != null)
                {
                    
                    //rgbFrame.CopySkeletonDataTo()
                    // Copy the pixel data from the image to a temporary array
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];

                    skeletonFrame.CopySkeletonDataTo(skeletons);
                  

                    //Skeleton[] skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    //skeletonFrame.CopySkeletonDataTo(skeletons);

                    DrawSkeletons();

                    
                    //return;

                }

            }
        }

        private void DrawSkeletons()
        {
            skeletonCanvas.Children.Clear();

            foreach (Skeleton skeleton in skeletons)
            {
                

                if(skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    //Console.WriteLine(skeleton.Joints.Count);
                    foreach (Joint joint in skeleton.Joints)
                    {
                        
                        // Convert joint position to screen coordinates
                        DepthImagePoint depthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(joint.Position, sensor.DepthStream.Format);
                        Point point = new Point(depthPoint.X, depthPoint.Y);
                       
                        // Create and draw a circle at the joint position
                        Ellipse ellipse = new Ellipse
                        {
                            Width = 10,
                            Height = 10,
                            Fill = new SolidColorBrush(Colors.Red)// or any color you prefer
                        };
                        statusBarText.Text = point.X + "...." + point.Y+">.......>"+skeletonCanvas.MaxHeight+ "..."+skeletonCanvas.MaxWidth;
                        Canvas.SetLeft(ellipse, (point.X - ellipse.Width / 2)/2.4);
                        Canvas.SetTop(ellipse, (point.Y - ellipse.Height / 2)/2.4);

                        skeletonCanvas.Children.Add(ellipse);
                    }
                    Ellipse ell = new Ellipse
                    {
                        Width = 10,
                        Height = 10,
                        Fill = new SolidColorBrush(Colors.Green)// or any color you prefer
                    };
                   
                    Canvas.SetLeft(ell, 235);
                    Canvas.SetTop(ell, 205);
                    // -5 , 25  
                    // 235, 205
                    skeletonCanvas.Children.Add(ell);
                }
            }
        }


        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ButtonScreenshotClick(object sender, RoutedEventArgs e)
        {
            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.ConnectDeviceFirst;
                return;
            }

            // create a png bitmap encoder which knows how to save a .png file
            BitmapEncoder encoder = new PngBitmapEncoder();

            // create frame from the writable bitmap and add to encoder
            encoder.Frames.Add(BitmapFrame.Create(this.colorBitmap));

            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            string path = System.IO.Path.Combine(myPhotos, "KinectSnapshot-" + time + ".png");

            // write the new file to disk
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                this.statusBarText.Text = string.Format(CultureInfo.InvariantCulture, "{0} {1}", Properties.Resources.ScreenshotWriteSuccess, path);
            }
            catch (IOException)
            {
                this.statusBarText.Text = string.Format(CultureInfo.InvariantCulture, "{0} {1}", Properties.Resources.ScreenshotWriteFailed, path);
            }
        }
        
        /// <summary>
        /// Handles the checking or unchecking of the near mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxNearModeChanged(object sender, RoutedEventArgs e)
        {
            if (this.sensor != null)
            {
                // will not function on non-Kinect for Windows devices
                try
                {
                    if (this.checkBoxNearMode.IsChecked.GetValueOrDefault())
                    {
                        this.sensor.DepthStream.Range = DepthRange.Near;
                    }
                    else
                    {
                        this.sensor.DepthStream.Range = DepthRange.Default;
                    }
                }
                catch (InvalidOperationException)
                {
                }
            }
        }
    }
}