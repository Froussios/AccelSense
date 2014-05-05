using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using AccelSense.Resources;
using AccelSense.ViewModels;

using Windows.Devices.Sensors;
using System.Windows.Threading;

namespace AccelSense
{
    public partial class MainPage : PhoneApplicationPage
    {
        private bool recording = false;
        private Recording recorded;
        private DispatcherTimer reader;


        /// <summary>
        /// Constructor
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }

            //ShowPreviously(); // The data context does this

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                //Accelerometer.GetDefault().ReadingChanged += MainPage_ReadingChanged;
                (reader = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(500) }).Tick += reader_Tick;
                reader.Start();
            });
        }


        /// <summary>
        /// Take a measurement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void reader_Tick(object sender, EventArgs e)
        {
            ProcessNewReading(Accelerometer.GetDefault().GetCurrentReading());
        }


        /// <summary>
        /// Show measurements from the datastore
        /// </summary>
        private void ShowPreviously()
        {
            foreach (Session session in App.ViewModel.AllSessions)
            {
                IEnumerable<Reading> readings = App.ViewModel.AllReadings.Where(x => x.Session.Equals(session));
                TextBlock tb = new TextBlock() { Text = String.Format("Session {0} ({2}): {1}", session.Id, readings.Count(), session.Activity) };
                tb.Tap += Session_Tap;
                tb.DataContext = session;

                DatastorePanel.Children.Add(tb);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void MainPage_ReadingChanged(Accelerometer sender, AccelerometerReadingChangedEventArgs args)
        {
            ProcessNewReading(args.Reading);
        }


        private int counted = 0;
        /// <summary>
        /// Store accelerometer reading and update the GUI
        /// </summary>
        /// <param name="reading"></param>
        private void ProcessNewReading(AccelerometerReading reading)
        {
            counted++;
            if (this.recording)
                this.recorded.Add(reading);

            if (counted % 10 == 0)
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                AccX.Text = "" + reading.AccelerationX;
                AccY.Text = "" + reading.AccelerationY;
                AccZ.Text = "" + reading.AccelerationZ;
                AccC.Text = "" + counted;

                if (this.recording)
                {
                    SamplesTextBlock.Text = "Samples: " + this.recorded.Count;
                }
            });
        }


        /// <summary>
        /// 
        /// </summary>
        private void StartRecording()
        {
            this.recorded = new Recording();
            this.recording = true;
            RecordButton.Content = "Save session";
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Recording StopRecording()
        {
            this.recording = false;
            RecordButton.Content = "Record";
            return this.recorded;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.recording)
            {
                StartRecording();
            }
            else
            {
                Recording recording = StopRecording();

                IEnumerable<Reading> readings = new List<Reading>(recording.Select(x => new Reading(x)));
                App.ViewModel.AddSession(readings, ActivityNameInput.Text);

                //DrawChart(recording);
            } 
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Session_Tap(object sender, RoutedEventArgs e)
        {
            FrameworkElement tb = sender as FrameworkElement;
            Session session = tb.DataContext as Session;

            NavigationService.Navigate(new Uri("/ViewSession.xaml?sessionId=" + session.Id, UriKind.Relative));
            return;

            ChartGrid.Children.Clear();
            ChartGrid.Children.Add(DrawChart(session));

            Recording.Analysis analysis = new Recording.Analysis(App.ViewModel.GetReadings(session));
            ChartGrid.Children.Add(new TextBlock() { Text = analysis.ToString() });
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="recording"></param>
        /// <returns></returns>
        private Sparrow.Chart.SparrowChart DrawChart(Session recording)
        {
            return DrawChart(App.ViewModel.AllReadings.Where(x => x.Session.Equals(recording)));
        }


        /// <summary>
        /// Create a graph for a recording,
        /// </summary>
        /// <param name="recording">To recording to graph</param>
        /// <returns>The GUI element</returns>
        private Sparrow.Chart.SparrowChart DrawChart(IEnumerable<Reading> recording)
        {
            Sparrow.Chart.LineSeries lseriesX = new Sparrow.Chart.LineSeries { StrokeThickness = 3, };
            Sparrow.Chart.LineSeries lseriesY = new Sparrow.Chart.LineSeries { StrokeThickness = 3, };
            Sparrow.Chart.LineSeries lseriesZ = new Sparrow.Chart.LineSeries { StrokeThickness = 3, };

            int i = 0;
            foreach (Reading reading in recording)
            {
                lseriesX.Points.Add(new Sparrow.Chart.DoublePoint { Data = i++, Value = reading.accX });
                lseriesY.Points.Add(new Sparrow.Chart.DoublePoint { Data = i++, Value = reading.accY });
                lseriesZ.Points.Add(new Sparrow.Chart.DoublePoint { Data = i++, Value = reading.accZ });
                i++;
            }
            Sparrow.Chart.SparrowChart chart = new Sparrow.Chart.SparrowChart
            {
                Height = 200,
                XAxis = new Sparrow.Chart.LinearXAxis { Interval = 25 },
                YAxis = new Sparrow.Chart.LinearYAxis { Interval = 2 }
            };
            chart.Series.Add(lseriesX);
            chart.Series.Add(lseriesY);
            chart.Series.Add(lseriesZ);
            return chart;
        }
    }


    /// <summary>
    /// The measurements of a recording session
    /// </summary>
    public class Recording
        : List<AccelerometerReading>
    {
        public class Analysis
        {
            public double AverageAbsoluteX { get; protected set; }
            public double AverageAbsoluteY { get; protected set; }
            public double AverageAbsoluteZ { get; protected set; }
            public double MaxX { get; protected set; }
            public double MaxY { get; protected set; }
            public double MaxZ { get; protected set; }


            /// <summary>
            /// Analyse a series of measurements
            /// </summary>
            /// <param name="recording"></param>
            public Analysis(IEnumerable<Reading> recording)
            {
                this.AverageAbsoluteX = recording.Average(x => Math.Abs(x.accX));
                this.AverageAbsoluteY = recording.Average(x => Math.Abs(x.accY));
                this.AverageAbsoluteZ = recording.Average(x => Math.Abs(x.accZ));

                this.MaxX = recording.Max(x => x.accX);
                this.MaxY = recording.Max(x => x.accY);
                this.MaxZ = recording.Max(x => x.accZ);    
            }


            /// <summary>
            /// Analyse a series of measurements
            /// </summary>
            /// <param name="recording"></param>
            public Analysis(IEnumerable<AccelerometerReading> recording)
                : this(recording.Select(x => new Reading(x)))
            {
            }


            /// <summary>
            /// Get the default comparison between analyses
            /// </summary>
            /// <param name="other">The analysis to compare this against</param>
            /// <returns>The distance</returns>
            public double DistanceFrom(Analysis other)
            {
                IList<double> v1 = this.ToVector();
                IList<double> v2 = this.ToVector();

                return EuclideanDistance(v1, v2);
            }


            /// <summary>
            /// Get the euclidean distance between two vectors.
            /// </summary>
            /// <param name="a1">The first vector</param>
            /// <param name="a2">The second vector</param>
            /// <returns></returns>
            public static double EuclideanDistance(IList<double> a1, IList<double> a2)
            {
                if (a1.Count != a2.Count)
                    throw new ArgumentException("Dimensions do not match.");

                double sum = 0;
                for (int i = 0; i < a1.Count; i++)
                {
                    double sumpart = a1[i] - a2[i];
                    sumpart *= sumpart;
                    sum += sumpart;
                }

                return Math.Sqrt(sum);
            }


            /// <summary>
            /// Classify activity
            /// </summary>
            /// <param name="input">The recording to classify.</param>
            /// <param name="training">Past recordings to use as training.</param>
            /// <returns>The activity the recording was classified as.</returns>
            public String Classify(IEnumerable<AccelerometerReading> input, IEnumerable<Session> training)
            {
                return KNN(input, training);
            }


            /// <summary>
            /// 
            /// </summary>
            /// <param name="input"></param>
            /// <param name="training"></param>
            /// <returns></returns>
            public String KNN(IEnumerable<AccelerometerReading> input, IEnumerable<Session> training)
            {
                Analysis analysis = new Analysis(input);

                // Compare input to every unit in training set
                Dictionary<int, double> scores = new Dictionary<int, double>();
                foreach (Session session in training)
                {
                    Analysis trainUnitAnalysis = new Analysis(App.ViewModel.GetReadings(session));
                    double distance = analysis.DistanceFrom(trainUnitAnalysis);
                    scores[session.Id] = distance;
                }

                // Get K closest neighbours
                int K = 10;
                IEnumerable<int> KNeighbours = scores.Keys.OrderByDescending(x => scores[x])
                                                          .Take(K);
              
                // Find plurality in neighbours
                String plurality = KNeighbours.GroupBy(x => App.ViewModel.GetSession(x).Activity)
                                              .OrderByDescending(g => g.Count())
                                              .First()
                                              .Key;

                return plurality;
            }


            /// <summary>
            /// Convert the analysis to an unweighted vector
            /// </summary>
            /// <returns>The vector</returns>
            public IList<double> ToVector()
            {
                return new double[] 
                {
                    this.AverageAbsoluteX,
                    this.AverageAbsoluteY,
                    this.AverageAbsoluteZ,
                    this.MaxX,
                    this.MaxY,
                    this.MaxZ,
                };
            }


            /// <summary>
            /// Human-readable representation of the analysis
            /// </summary>
            /// <returns></returns>
            public override String ToString()
            {
                return String.Format("X({0}, {3}) Y({1}, {4}) Z({2}, {5})", AverageAbsoluteX, AverageAbsoluteY, AverageAbsoluteZ, MaxX, MaxY, MaxZ);
            }
        }
    }
}