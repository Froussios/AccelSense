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

            if (counted % samplingFreq == 0) // Update every 1 second
            {
                //Deployment.Current.Dispatcher.BeginInvoke(() =>
                //{
                AccX.Text = "" + reading.AccelerationX;
                AccY.Text = "" + reading.AccelerationY;
                AccZ.Text = "" + reading.AccelerationZ;
                AccC.Text = "" + counted;

                if (this.recording)
                {
                    SamplesTextBlock.Text = "Samples: " + this.runningRecording.Count;
                }
                //});
            }
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

                // Clasify as parts
                String activitySequence = "";
                ICollection<IList<Reading>> parts = BreakRecording(readings);
                foreach (IList<Reading> part in parts)
                {
                    String partClass = Recording.Analysis.Classify(part, App.ViewModel.AllSessions);
                    activitySequence += partClass.First();
                }

                // Add marker every 30s
                for (int i = 0; i < activitySequence.Length; i += 16)
                {
                    activitySequence = activitySequence.Insert(i, "$");
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


        /// <summary>
        /// Calculate and print the distance between two sessions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompareButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int sessionId1;
            int sessionId2;
            Boolean parsed = false;
            parsed = int.TryParse(CompareInput1.Text, out sessionId1);
            parsed = int.TryParse(CompareInput2.Text, out sessionId2) && parsed;

            if (parsed)
            {
                Session session1 = App.ViewModel.GetSession(sessionId1);
                Session session2 = App.ViewModel.GetSession(sessionId2);

                IEnumerable<Reading> readings1 = App.ViewModel.GetReadings(session1);
                IEnumerable<Reading> readings2 = App.ViewModel.GetReadings(session2);

                Recording.Analysis analysis1 = new Recording.Analysis(readings1);
                Recording.Analysis analysis2 = new Recording.Analysis(readings2);

                double distance1 = analysis1.DistanceFrom(analysis2);
                double distance2 = analysis2.DistanceFrom(analysis1);

                CompareResults.Text = "";
                CompareResults.Text = String.Format("Distance: {0} ({1})\n Ids: {2}, {3}\n Stamps: {4}, {5}",
                                                    distance1, distance2,
                                                    session1.Id, session2.Id,
                                                    analysis1.Stamp(), analysis2.Stamp());
            }
            else
            {
                CompareResults.Text = "Provide two session ids";
            }
        }

        private void Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            RecordingChartContainer.Children.Clear();
            RecordingChartContainer.Children.Add(DrawChart(lastRecording));
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
            public double AverageCrossingsX { get; protected set; }
            public double AverageCrossingsY { get; protected set; }
            public double AverageCrossingsZ { get; protected set; }


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
                        if ((reading.accX - AverageX) * (lastReading.accX - AverageX) < 0) this.AverageCrossingsX++;
                        if ((reading.accY - AverageY) * (lastReading.accY - AverageY) < 0) this.AverageCrossingsY++;
                        if ((reading.accZ - AverageZ) * (lastReading.accZ - AverageZ) < 0) this.AverageCrossingsZ++;
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

            private Analysis()
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
                    //this.AverageCrossingsX,
                    //this.AverageCrossingsY,
                    //this.AverageCrossingsZ,
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
                IList<double> v2 = other.ToVector();

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
            /// Get the normalised euclidean distance between two vectors
            /// </summary>
            /// <param name="v1"></param>
            /// <param name="v2"></param>
            /// <param name="stdDevs">The standard deviation for every dimension</param>
            /// <returns></returns>
            public static double NormalisedEuclideanDistance(IList<double> v1, IList<double> v2, IList<double> stdDevs)
            {
                throw new NotImplementedException();
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
                // Smaller score is closer
                int K = (int)Math.Sqrt(training.Count());
                IEnumerable<int> KNeighbours = scores.Keys.OrderBy(x => scores[x]).
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
            /// Calculate the standard deviations for the Normalised Euclidean Distance
            /// </summary>
            /// <param name="analyses"></param>
            /// <returns></returns>
            private static IList<double> GetStandardDeviations(IEnumerable<Analysis> analyses)
            {
                Analysis averages = new Analysis();
                averages.AmplitudeX = analyses.Average(a => a.AmplitudeX);
                averages.AmplitudeY = analyses.Average(a => a.AmplitudeY);
                averages.AmplitudeZ = analyses.Average(a => a.AmplitudeZ);
                averages.VarianceX = analyses.Average(a => a.VarianceX);
                averages.VarianceY = analyses.Average(a => a.VarianceY);
                averages.VarianceZ = analyses.Average(a => a.VarianceZ);

                Analysis standardDeviations = new Analysis();
                standardDeviations.AmplitudeX = Math.Sqrt(analyses.Average(a => a.AmplitudeX * a.AmplitudeX) - averages.AmplitudeX);
                standardDeviations.AmplitudeY = Math.Sqrt(analyses.Average(a => a.AmplitudeY * a.AmplitudeY) - averages.AmplitudeY);
                standardDeviations.AmplitudeZ = Math.Sqrt(analyses.Average(a => a.AmplitudeZ * a.AmplitudeZ) - averages.AmplitudeZ);
                standardDeviations.VarianceX = Math.Sqrt(analyses.Average(a => a.VarianceX * a.VarianceX) - averages.VarianceX);
                standardDeviations.VarianceY = Math.Sqrt(analyses.Average(a => a.VarianceY * a.VarianceY) - averages.VarianceY);
                standardDeviations.VarianceZ = Math.Sqrt(analyses.Average(a => a.VarianceZ * a.VarianceZ) - averages.VarianceZ);

                return standardDeviations.ToVector();
            }


            /// <summary>
            /// Human-readable representation of the analysis
            /// </summary>
            /// <returns></returns>
            public override String ToString()
            {
                return String.Format("X({0}, {3}) Y({1}, {4}) Z({2}, {5})", AverageAbsoluteX, AverageAbsoluteY, AverageAbsoluteZ, MaxX, MaxY, MaxZ);
            }


            /// <summary>
            /// Quick value for distringuishing analyses
            /// </summary>
            /// <returns></returns>
            public double Stamp()
            {
                return this.ToVector().Sum();
            }
        }
    }
}