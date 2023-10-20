/**
 * A simple firestore database listener, which listens to document updates with a particular room key and a minimum date time.
 * Acts kinda like a queue.
 * Multiple instances with the same room key WILL read the same messages - not fit to be a service worker.
 */

using UnityEngine;
using Firebase.Firestore;
using System.Threading.Tasks;
using System;

public class FirestoreService : MonoBehaviour
{
    [FirestoreData]
    public struct Entry
    {
        [FirestoreProperty]
        public string RoomCode { get; set; }
        [FirestoreProperty]
        public DateTime DateTime { get; set; }
        [FirestoreProperty]
        public string Payload { get; set; }
    }

    public Entry CreateEntry(string payload) =>
        new() {
            RoomCode = RoomCode,
            DateTime = DateTime.Now,
            Payload = payload,
        };


    /// <summary> For logging </summary>
    private const string LOG_TAG = "[" + nameof(FirestoreService) + "]";
    /// <summary> Chars used when generating a room code </summary>
    private const string ROOM_CODE_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    /// <summary> Room code length </summary>
    private const int ROOM_CODE_LENGTH = 4;

    #region Events

    public delegate void OnReadyHandler(string roomCode);
    /// <summary>
    /// When the server is listening for messages at the provided roomCode
    /// </summary>
    public event OnReadyHandler OnReady;

    public delegate void OnMessageHandler(string message);
    /// <summary>
    /// When a new message has been recieved on the listening roomCode
    /// </summary>
    public event OnMessageHandler OnMessage;

    #endregion Events

    [SerializeField, Tooltip("Enable verbose debug messages")]
    private bool _verboseLogging = false;

    [SerializeField, Tooltip("Name of the top-level messages collection in FireStore. Will be created")]
    private string _instanceCollection = "Messages";
    // Maps to the Entry member
    private const string _roomCodeKey = "RoomCode";
    [SerializeField, Tooltip("Leave empty to generate on start")]
    private string _roomCode = null;

    // Maps to the Entry member
    private string _dateTimeKey = "DateTime";

    // message minimum datetime before being filtered
    private DateTime _watchDateTime = DateTime.MinValue;

    /// <summary>
    /// Current room code, will generate if doesn't yet exist
    /// </summary>
    public string RoomCode
    {
        get
        {
            if (string.IsNullOrEmpty(_roomCode))
            {
                _roomCode = GenerateRoomCode();
            }
            return _roomCode;
        }
        set => _roomCode = value;
    }

    private string GenerateRoomCode()
    {
        char[] roomCode = new char[ROOM_CODE_LENGTH];
        System.Random random = new ();
        for(int i = 0; i < ROOM_CODE_LENGTH; i++)
        {
            roomCode[i] = ROOM_CODE_CHARS[random.Next(ROOM_CODE_CHARS.Length)];
        }
        return new string(roomCode);
    }

    private FirebaseFirestore _db;
    private CollectionReference _collectionRef;
    private ListenerRegistration _updateListener;

    private async Task InitFirestore()
    {
        await StopUpdateListener();
        if (_collectionRef != null)
            _collectionRef = null;
        if (_db != null) {
            await _db.TerminateAsync();
            await _db.ClearPersistenceAsync();
            _db = null;
        }
        _db = FirebaseFirestore.DefaultInstance;
        _db.Settings.PersistenceEnabled = false;
        _collectionRef = _db.Collection(_instanceCollection);

        try
        {
            // Creat test document
            //await _collectionRef.Document().SetAsync(CreateEntry("test"));
            if (_verboseLogging)
                Debug.Log($"Ready Code: [{RoomCode}]");

            // Only read new messages (starting from now)
            StartUpdateListener(DateTime.UtcNow);
            OnReady?.Invoke(RoomCode);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void OnSnapshotUpdate(QuerySnapshot snapshot)
    {
        if (_verboseLogging)
            Debug.Log("Callback received query snapshot."); 
        Entry data;
        // Where to set the new mimimum date time
        DateTime newWatchDateTime = _watchDateTime;

        // Handle each document
        foreach (DocumentSnapshot documentSnapshot in snapshot.Documents)
        {
            data = documentSnapshot.ConvertTo<Entry>(serverTimestampBehavior:ServerTimestampBehavior.Estimate);
            // Skip stale messages.
            if (data.DateTime <= _watchDateTime)
                continue;

            if (_verboseLogging)
                Debug.Log($"Handle new message id [{documentSnapshot.Id}] at [{data.DateTime}] [{data.Payload}]");
            OnMessage?.Invoke(data.Payload);
            if (data.DateTime > _watchDateTime)
                newWatchDateTime = data.DateTime;
        }
        // Move the dateTime cutoff forward
        if (newWatchDateTime > _watchDateTime)
        {
            if (_verboseLogging)
                Debug.Log($"{LOG_TAG} Updating document monitor DateTime query");
            // restart message listener, whose DateTimes are newer than the new _watchDateTime
            StopUpdateListener().Wait();
            StartUpdateListener(newWatchDateTime);
        }
    }

    void Start()
    {
        _ = InitFirestore();
    }

    void Update()
    {
        MonitorUpdateListener();
    }

    /// <summary>
    /// Starts the firestore qury listener. Filters for it's own room code, and filters out stale DateTime values
    /// </summary>
    /// <param name="dateTime">The minimum date time to include in the listener's query. Anything older will be ignored</param>
    void StartUpdateListener(DateTime? dateTime = null)
    {
        if (dateTime != null)
            _watchDateTime = dateTime.Value;
        Query query = _collectionRef
            .WhereEqualTo(_roomCodeKey, RoomCode)
            .WhereGreaterThanOrEqualTo(_dateTimeKey, _watchDateTime)
            .OrderBy(_dateTimeKey);
        _updateListener = query.Listen(OnSnapshotUpdate);
    }

    /// <summary>
    /// Monitor the firestore update listener for faults.
    /// Does NOT restart the listener on fault.
    /// </summary>
    async void MonitorUpdateListener()
    {
        // If we have no state (already cleaned up) or if we're currently running, return
        if (_updateListener == null || !_updateListener.ListenerTask.IsCompleted)
            return;

        // Inspect and log any faults
        if (_updateListener.ListenerTask.IsFaulted )
        {
            Exception ex = _updateListener.ListenerTask.Exception;
            if (ex is AggregateException)
                ex = ex.InnerException;
            if (ex is FirestoreException)
            {
                FirestoreException fex = ex as FirestoreException;
                if(fex.Message.StartsWith("The query requires an index"))
                {
                    Debug.LogError($"{LOG_TAG} Firestore requires an index on \"DateTime\". This must be done in the online console.\nUse the link in the following exception log.");
                }
            }
            Debug.LogException(ex);
        }
        if (_verboseLogging)
            Debug.Log($"{LOG_TAG} Stopping document monitor");
        // cleanup
        await StopUpdateListener();
    }

    /// <summary>
    /// Kills the firestore query subscription
    /// </summary>
    async Task StopUpdateListener()
    {
        if (_updateListener != null && !_updateListener.ListenerTask.IsCompleted)
        {
            _updateListener.Stop();
            await _updateListener.ListenerTask;
        }
        _updateListener = null;
    }
}
