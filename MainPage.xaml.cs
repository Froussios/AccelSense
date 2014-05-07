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
        private const int samplingFreq = 20;
        private const int samplingMillis = 1000 / samplingFreq;
        private const int partDurationMillis = 2000;
        private const int partSize = partDurationMillis / samplingMillis;

        private bool recording = false;
        private Recording runningRecording;
        private IList<Reading> lastRecording;
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

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                //Accelerometer.GetDefault().ReadingChanged += MainPage_ReadingChanged;
                reader = new DispatcherTimer() 
                {
                    Interval = TimeSpan.FromMilliseconds(samplingMillis) 
                };
                reader.Tick += reader_Tick;
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
                this.runningRecording.Add(reading);

            if (counted % 10 == 0)
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                AccX.Text = "" + reading.AccelerationX;
                AccY.Text = "" + reading.AccelerationY;
                AccZ.Text = "" + reading.AccelerationZ;
                AccC.Text = "" + counted;

                if (this.recording)
                {
                    SamplesTextBlock.Text = "Samples: " + this.runningRecording.Count;
                }
            });
        }


        /// <summary>
        /// 
        /// </summary>
        private void StartRecording()
        {
            this.runningRecording = new Recording();
            this.recording = true;
            RecordButton.Content = "End session";
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Recording StopRecording()
        {
            this.recording = false;
            RecordButton.Content = "Record";
            lastRecording = new List<Reading>(runningRecording.Select(x => new Reading(x)));
            return runningRecording;
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

                IList<Reading> readings = new List<Reading>(recording.Select(x => new Reading(x)));

                // Classify whole
                String classification = Recording.Analysis.Classify(readings, App.ViewModel.AllSessions);
                this.RecordingFeedback.Text = "Looking like " + classification;

                // TODO Clasify as parts
                String activitySequence = "";
                ICollection<IList<Reading>> parts = BreakRecording(readings);
                foreach (IList<Reading> part in parts)
                {
                    String partClass = Recording.Analysis.Classify(part, App.ViewModel.AllSessions);
                    activitySequence += partClass.First();
                }
                this.DetailedResults.Text = activitySequence;
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


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveSession_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            // Break into parts
            ICollection<IList<Reading>> parts = BreakRecording(lastRecording);

            // Save parts
            foreach (IList<Reading> part in parts)
                App.ViewModel.AddSession(part, (ActivityNameInput.SelectedItem as ListBoxItem).Content.ToString());
        }


        /// <summary>
        /// Breaks recording into parts of equal size
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        private ICollection<IList<Reading>> BreakRecording(IList<Reading> recording)
        {
            ICollection<IList<Reading>>  parts = new List<IList<Reading>>();

            for (int i = 0; i < recording.Count(); i++)
            {
                // Start new part
                if (i % partSize == 0)
                    parts.Add(new List<Reading>(partSize));

                // Add to last part
                parts.Last().Add(recording[i]);
            }

            return parts;
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
            public double AverageX { get; protected set; }
            public double AverageY { get; protected set; }
            public double AverageZ { get; protected set; }
            public double MaxX { get; protected set; }
            public double MaxY { get; protected set; }
            public double MaxZ { get; protected set; }
            public double MinX { get; protected set; }
            public double MinY { get; protected set; }
            public double MinZ { get; protected set; }
            public double AmplitudeX { get; protected set; }
            public double AmplitudeY { get; protected set; }
            public double AmplitudeZ { get; protected set; }
            public double VarianceX { get; protected set; }
            public double VarianceY { get; protected set; }
            public double VarianceZ { get; protected set; }
            public double ZeroCrossingsX { get; protected set; }
            public double ZeroCrossingsY { get; protected set; }
            public double ZeroCrossingsZ { get; protected set; }


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

                this.MinX = recording.Min(x => x.accX);
                this.MinY = recording.Min(x => x.accY);
                this.MinZ = recording.Min(x => x.accZ);

                this.AmplitudeX = this.MaxX - this.MinX;
                this.AmplitudeY = this.MaxY - this.MinY;
                this.AmplitudeZ = this.MaxZ - this.MinZ;

                this.AverageX = recording.Average(x => x.accX);
                this.AverageY = recording.Average(x => x.accY);
                this.AverageZ = recording.Average(x => x.accZ);

                this.VarianceX = recording.Average(x => (x.accX - AverageX) * (x.accX - AverageX));
                this.VarianceY = recording.Average(x => (x.accY - AverageY) * (x.accY - AverageY));
                this.VarianceZ = recording.Average(x => (x.accZ - AverageZ) * (x.accZ - AverageZ));

                // Count zero-crossings
                Reading lastReading = null;
                foreach (Reading reading in recording)
                {
                    if (lastReading != null)
                    {
                        if (reading.accX * lastReading.accX < 0) this.ZeroCrossingsX++;
                        if (reading.accY * lastReading.accY < 0) this.ZeroCrossingsY++;
                        if (reading.accZ * lastReading.accZ < 0) this.ZeroCrossingsZ++;
                    }
                    lastReading = reading;
                }
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
            /// Convert the analysis to an unweighted vector
            /// </summary>
            /// <returns>The vector</returns>
            public IList<double> ToVector()
            {
                return new double[] 
                {
                    this.AmplitudeX,
                    this.AmplitudeY,
                    this.AmplitudeZ,
                    this.VarianceX,
                    this.VarianceY,
                    this.VarianceZ,
                    this.ZeroCrossingsX,
                    this.ZeroCrossingsY,
                    this.ZeroCrossingsZ,
                };
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
            public static String Classify(IEnumerable<Reading> input, IEnumerable<Session> training)
            {
                return KNN(input, training);
            }


            /// <summary>
            /// Classify activity
            /// </summary>
            /// <param name="input">The recording to classify.</param>
            /// <param name="training">Past recordings to use as training.</param>
            /// <returns>The activity the recording was classified as.</returns>
            public static String Classify(IEnumerable<AccelerometerReading> input, IEnumerable<Session> training)
            {
                return Classify(input.Select(x => new Reading(x)), training);
            }


            /// <summary>
            /// 
            /// </summary>
            /// <param name="input"></param>
            /// <param name="training"></param>
            /// <returns></returns>
            public static String KNN(IEnumerable<Reading> input, IEnumerable<Session> training)
            {
                if (training == null || training.Count() == 0)
                    return "?";

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
                IEnumerable<int> KNeighbours = scores.Keys.OrderByDescending(x => scores[x]).
                                                           Take(K);
              
                // Find plurality in neighbours
                String plurality = KNeighbours.GroupBy(x => App.ViewModel.GetSession(x).Activity).
                                               OrderByDescending(g => g.Count()).
                                               First().
                                               Key;

                // TODO Do not favour most populous training

                return plurality;
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