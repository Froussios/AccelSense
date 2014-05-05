using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using AccelSense.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace AccelSense
{
    public partial class ViewSession : PhoneApplicationPage
    {
        public ViewSession()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string parameter = string.Empty;
            if (NavigationContext.QueryString.TryGetValue("sessionId", out parameter))
            {
                int sessionId = Int32.Parse(parameter);
                Session session = App.ViewModel.GetSession(sessionId);
                IEnumerable<Reading> readings = App.ViewModel.GetReadings(session);
                Recording.Analysis analysis = new Recording.Analysis(readings);
                this.DataContext = new Tuple<Session, IEnumerable<Reading>, Recording.Analysis>(session, readings, analysis);

                this.ChartContainer.Children.Add(DrawChart(readings));
            }
        }
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
}