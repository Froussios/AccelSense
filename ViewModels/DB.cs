using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Linq;
using System.Linq;
using System.Data.Linq.Mapping;
using System.Collections.Generic;


namespace AccelSense.ViewModels
{
    public class ViewModel : Notifier
    {
        // LINQ to SQL data context for the local database.
        private AccelSenseDataContext DB = null;

        public AccelSenseDataContext DataContext
        {
            get
            {
                if (DB != null)
                    return DB;
                return DB = new AccelSenseDataContext("isostore:/MyDatabase.sdf");
            }
        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public void LoadData()
        {
            // Sample data; replace with real data
            LoadReadings();
            LoadSessions();

            this.IsDataLoaded = true;
        }

        /// <summary>
        /// Loads all entries from the database into memory
        /// </summary>
        public void LoadReadings()
        {
            var readingsInDB = from Reading entry in this.DataContext.Readings
                              select entry;
            AllReadings = new ObservableCollection<Reading>(readingsInDB);
        }

        private ObservableCollection<Reading> _allReadings;
        public ObservableCollection<Reading> AllReadings
        {
            get { return _allReadings; }
            set
            {
                _allReadings = value;
                NotifyPropertyChanged("AllReadings");
            }
        }


        private ObservableCollection<Session> _allSessions;
        public ObservableCollection<Session> AllSessions
        {
            get { return _allSessions; }
            set
            {
                _allSessions = value;
                NotifyPropertyChanged("AllSessions");
            }
        }


        /// <summary>
        /// Loads all activities from the database into memory
        /// </summary>
        public void LoadSessions()
        {
            var sessionsInDB = from Session entry in this.DataContext.Sessions
                                 select entry;
            AllSessions = new ObservableCollection<Session>(sessionsInDB);
        }


        /// <summary>
        /// Insert an empty session in the datastore.
        /// </summary>
        /// <param name="inSession">The session</param>
        public void AddSession(Session inSession)
        {
            this.DataContext.Sessions.InsertOnSubmit(inSession);
            this.DataContext.SubmitChanges();
        }


        /// <summary>
        /// Insert a single reading into the datastore.
        /// </summary>
        /// <param name="inReading">The reading</param>
        public void AddReading(Reading inReading)
        {
            this.DataContext.Readings.InsertOnSubmit(inReading);
            this.DataContext.SubmitChanges();
        }


        /// <summary>
        /// Insert a new session. A new session instance will be created automatically.
        /// </summary>
        /// <param name="readings">The readings for this session</param>
        /// <param name="activity">The name of the activity for this session</param>
        public void AddSession(IEnumerable<Reading> readings, String activity)
        {
            // Create new session for this bulk
            Session session = new Session();
            session.Activity = activity;

            // Apply to datastore
            this.DataContext.Sessions.InsertOnSubmit(session);
            this.DataContext.SubmitChanges();

            // Assign session to bulk
            foreach (Reading reading in readings)
                reading.Session = session;

            // Apply to datastore
            this.DataContext.Readings.InsertAllOnSubmit(readings);
            this.DataContext.SubmitChanges();
        }


        /// <summary>
        /// Get the readings in a session
        /// </summary>
        /// <param name="session">The session</param>
        /// <returns>The readings for the specified session</returns>
        public IEnumerable<Reading> GetReadings(Session session)
        {
            return AllReadings.Where(reading => reading.Session.Equals(session));
        }


        /// <summary>
        /// Get the session by session id
        /// </summary>
        /// <param name="sessionId">The id of the session to retrieve</param>
        /// <returns>The session</returns>
        public Session GetSession(int sessionId)
        {
            return AllSessions.Where(x => x.Id == sessionId).Single();
        }


    }


    /// <summary>
    /// The data context
    /// </summary>
    public class AccelSenseDataContext : DataContext
    {
        // Pass the connection string to the base class.
        public AccelSenseDataContext(string connectionString)
            : base(connectionString)
        {
            if (!this.DatabaseExists())
                this.CreateDatabase();
        }

        public Table<Reading> Readings;
        public Table<Session> Sessions;
    }


    /// <summary>
    /// A short-hand class that implements INotifyPropertyChanged and INotifyPropertyChanging in a generic way.
    /// </summary>
    public class Notifier : INotifyPropertyChanged, INotifyPropertyChanging
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region INotifyPropertyChanging Members

        public event PropertyChangingEventHandler PropertyChanging;

        // Used to notify that a property is about to change
        protected void NotifyPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }

        #endregion
    }


    /// <summary>
    /// A reading from the accelerometer, belonging to a recording session.
    /// </summary>
    [Table]
    public class Reading : Notifier
    {
        public Reading()
        {
        }

        public Reading(double inx, double iny, double inz)
        {
            this.accX = inx;
            this.accY = iny;
            this.accZ = inz;
        }

        public Reading(Windows.Devices.Sensors.AccelerometerReading reading)
        {
            this.accX = reading.AccelerationX;
            this.accY = reading.AccelerationY;
            this.accZ = reading.AccelerationZ;
        }

        private int id;

        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "INT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public int Id
        {
            get { return id; }
            set
            {
                if (id != value)
                {
                    NotifyPropertyChanging("Id");
                    id = value;
                    NotifyPropertyChanged("Id");
                }
            }
        }

        private double accx;
        [Column]
        public double accX
        {
            get { return accx; }
            set
            {
                if (accx != value)
                {
                    NotifyPropertyChanging("accX");
                    accx = value;
                    NotifyPropertyChanged("accX");
                }
            }
        }

        private double accy;
        [Column]
        public double accY
        {
            get { return accy; }
            set
            {
                if (accy != value)
                {
                    NotifyPropertyChanging("accY");
                    accy = value;
                    NotifyPropertyChanged("accY");
                }
            }
        }

        private double accz;
        [Column]
        public double accZ
        {
            get { return accz; }
            set
            {
                if (accz != value)
                {
                    NotifyPropertyChanging("accZ");
                    accz = value;
                    NotifyPropertyChanged("accZ");
                }
            }
        }


        // Internal column for the associated ToDoCategory ID value
        [Column]
        internal int _sessionId;
        // Entity reference, to identify the ToDoCategory "storage" table
        private EntityRef<Session> _session;
        // Association
        [Association(Storage = "_session", ThisKey = "_sessionId", OtherKey = "Id", IsForeignKey = true)]
        public Session Session
        {
            get { return _session.Entity; }
            set
            {
                NotifyPropertyChanging("Session");
                _session.Entity = value;

                if (value != null)
                {
                    _sessionId = value.Id;
                }

                NotifyPropertyChanging("Session");
            }
        }
    }


    /// <summary>
    /// A collection of readings 
    /// </summary>
    [Table]
    public class Session : Notifier
    {
        private int id;

        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "INT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public int Id
        {
            get { return id; }
            set
            {
                if (id != value)
                {
                    NotifyPropertyChanging("Id");
                    id = value;
                    NotifyPropertyChanged("Id");
                }
            }
        }

        private String activity;
        [Column]
        public String Activity
        {
            get { return activity; }
            set
            {
                if (activity != value)
                {
                    NotifyPropertyChanging("Activity");
                    activity = value;
                    NotifyPropertyChanged("Activity");
                }
            }
        }


        public bool Equals(Object other)
        {
            Session otherS = other as Session;
            if (otherS != null)
                return otherS.Id == this.Id;
            return false;
        }


        public override int HashCode()
        {
            return this.id;
        }
    }
}