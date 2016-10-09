using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpeechRecognition : MonoBehaviour {

	public int maxResults = 5;
	public string preferredLanguage = "en-US";

	public bool enableDebugLog = false;
	public bool enableEventLog = false;

	public bool enableOnBeginningOfSpeech = false;
	public bool enableOnBufferReceived = false;
	public bool enableOnEndOfSpeech = false;
	public bool enableOnEvent = false;
	public bool enableOnPartialResults = false;
	public bool enableOnReadyForSpeech = false;
	public bool enableOnRmsChanged = false;
	
	public bool autoRestart = false;
	public bool autoRestartOnResume = false;
	public float autoRestartAmpThreshold = 0.5f;

	public bool disableScreenLockOnResult = false;
	public float screenLockTimeout = 10f;


	public enum State{
		NOT_INITIALIZED = 0,
		IDLE = 1,
		LISTENING_TO_SOUND = 2,
		LISTENING_TO_SPEECH = 3,
		LISTINING_TO_SPEECH_INIT = 4,
		STOPPED_LISTENING = 5,
	}
	private State state = State. NOT_INITIALIZED;

	//for use in javascript
	public static int NOT_INITIALIZED = 0;
	public static int IDLE = 1;
	public static int LISTENING_TO_SOUND = 2;
	public static int LISTENING_TO_SPEECH = 3;
	public static int LISTINING_TO_SPEECH_INIT = 4;
	public static int STOPPED_LISTENING = 5;

	private static Rect touchToListenRect = new Rect(0,0,0,0);
	private static bool touchToListenEnabled = false;

#if UNITY_ANDROID && !UNITY_EDITOR
	private static AndroidJavaObject currentActivity;
	private static AndroidJavaClass speechRecognition;
#endif

	private static bool isRecognitionAvailable = false;
	public static SpeechRecognition instance = null;

#if UNITY_ANDROID && !UNITY_EDITOR
	private int maxResultsJni = 0;
	private string preferredLanguageJni = "en-US";

	private bool enableOnBeginningOfSpeechJni = false;
	private bool enableOnBufferReceivedJni = false;
	private bool enableOnEndOfSpeechJni = false;
	private bool enableOnEventJni = false;
	private bool enableOnPartialResultsJni = false;
	private bool enableOnReadyForSpeechJni = false;
	private bool enableOnRmsChangedJni = false;
	private bool autoRestartJni = false;
	private bool autoRestartOnResumeJni = false;
	private float autoRestartAmpThresholdJni = -1f;
#endif

	private float screenLockTimeLeft = -1f;

	private SpeechDictionary speechDictionary = null;

	private static System.Text.Encoding utf8 = System.Text.Encoding.UTF8;
	private static string[] newLineSplit = new string[]{System.Environment.NewLine};
	private static List<ISpeechRecognitionListener> eventListeners = new List<ISpeechRecognitionListener>();
	private static List<ISpeechRecognitionListenerJs> eventListenersJs = new List<ISpeechRecognitionListenerJs>();

	private static Dictionary<int,string> errorMessages = new Dictionary<int, string>();

	private static HashSet<string> newCommands = new HashSet<string>();
	private static HashSet<string> activeCommandsInThisFrame = new HashSet<string>();

	// Use this for initialization
	void Awake () {
		//next line is added to avoid the not used warning
		touchToListenRect.Contains(new Vector3(1,1,1));

		if(instance != null){
			Destroy(this.gameObject);
			return;
		}
		DontDestroyOnLoad(this.gameObject);
		instance = this;
		speechDictionary = gameObject.GetComponent<SpeechDictionary>();
		speechDictionary.ReloadDictionary();
		errorMessages.Add(3, "Audio recording error.");
		errorMessages.Add(5, "Other client side errors.");
		errorMessages.Add(9, "Insufficient permissions");
		errorMessages.Add(2, "Other network related errors.");
		errorMessages.Add(1, "Network operation timed out.");
		errorMessages.Add(7, "No recognition result matched.");
		errorMessages.Add(8, "RecognitionService busy.");
		errorMessages.Add(4, "Server sends error status.");
		errorMessages.Add(6, "No speech input.");
#if UNITY_ANDROID && !UNITY_EDITOR
		AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
		currentActivity = jc.GetStatic<AndroidJavaObject>("currentActivity");
		speechRecognition = new AndroidJavaClass("be.jannesplyson.unity3dspeechrecognition.Unity3DSpeechRecognition");
		isRecognitionAvailable = speechRecognition.CallStatic<bool>("isRecognitionAvailable",currentActivity);
		if(isRecognitionAvailable){
			speechRecognition.SetStatic<int>("maxResults",maxResults);
			speechRecognition.SetStatic<string>("preferredLanguage",preferredLanguage);
			speechRecognition.SetStatic<bool>("enableOnBeginningOfSpeech", enableOnBeginningOfSpeech);
			speechRecognition.SetStatic<bool>("enableOnBufferReceived", enableOnBufferReceived);
			speechRecognition.SetStatic<bool>("enableOnEndOfSpeech", enableOnEndOfSpeech);
			speechRecognition.SetStatic<bool>("enableOnEvent", enableOnEvent);
			speechRecognition.SetStatic<bool>("enableOnPartialResults", enableOnPartialResults);
			speechRecognition.SetStatic<bool>("enableOnReadyForSpeech", enableOnReadyForSpeech);
			speechRecognition.SetStatic<bool>("enableOnRmsChanged", enableOnRmsChanged);
			speechRecognition.SetStatic<bool>("autoRestart", autoRestart);
			speechRecognition.SetStatic<bool>("autoRestartOnResume", autoRestartOnResume);
			speechRecognition.SetStatic<float>("autoRestartAmpThreshold", autoRestartAmpThreshold);
			maxResultsJni = speechRecognition.GetStatic<int>("maxResults");
			preferredLanguageJni = speechRecognition.GetStatic<string>("preferredLanguage");
			enableOnBeginningOfSpeechJni = speechRecognition.GetStatic<bool>("enableOnBeginningOfSpeech");
			enableOnBufferReceivedJni = speechRecognition.GetStatic<bool>("enableOnBufferReceived");
			enableOnEndOfSpeechJni = speechRecognition.GetStatic<bool>("enableOnEndOfSpeech");
			enableOnEventJni = speechRecognition.GetStatic<bool>("enableOnEvent");
			enableOnPartialResultsJni = speechRecognition.GetStatic<bool>("enableOnPartialResults");
			enableOnReadyForSpeechJni = speechRecognition.GetStatic<bool>("enableOnReadyForSpeech");
			enableOnRmsChangedJni = speechRecognition.GetStatic<bool>("enableOnRmsChanged");
			autoRestartJni = speechRecognition.GetStatic<bool>("autoRestart");
			autoRestartOnResumeJni = speechRecognition.GetStatic<bool>("autoRestartOnResume");
			autoRestartAmpThresholdJni = speechRecognition.GetStatic<float>("autoRestartAmpThreshold");
			speechRecognition.CallStatic("initSpeechRecognition",currentActivity);
		}
#endif
	}

	// Update is called once per frame
	void Update () {
		if(state != State.NOT_INITIALIZED){
#if UNITY_ANDROID && !UNITY_EDITOR
			if(maxResults != maxResultsJni){
				speechRecognition.SetStatic<int>("maxResults",maxResults);
				maxResultsJni = speechRecognition.GetStatic<int>("maxResults");
			}
			if(preferredLanguage != preferredLanguageJni){
				speechRecognition.SetStatic<string>("preferredLanguage",preferredLanguage);
				preferredLanguageJni = speechRecognition.GetStatic<string>("preferredLanguage");
			}
			if(enableOnBeginningOfSpeechJni != enableOnBeginningOfSpeech){
				speechRecognition.SetStatic<bool>("enableOnBeginningOfSpeech", enableOnBeginningOfSpeech);
				enableOnBeginningOfSpeechJni = speechRecognition.GetStatic<bool>("enableOnBeginningOfSpeech");
			}
			if(enableOnBufferReceivedJni != enableOnBufferReceived){
				speechRecognition.SetStatic<bool>("enableOnBufferReceived", enableOnBufferReceived);
				enableOnBufferReceivedJni = speechRecognition.GetStatic<bool>("enableOnBufferReceived");
			}
			if(enableOnEndOfSpeechJni != enableOnEndOfSpeech){
				speechRecognition.SetStatic<bool>("enableOnEndOfSpeech", enableOnEndOfSpeech);
				enableOnEndOfSpeechJni = speechRecognition.GetStatic<bool>("enableOnEndOfSpeech");
			}
			if(enableOnEventJni != enableOnEvent){
				speechRecognition.SetStatic<bool>("enableOnEvent", enableOnEvent);
				enableOnEventJni = speechRecognition.GetStatic<bool>("enableOnEvent");
			}
			if(enableOnPartialResultsJni != enableOnPartialResults){
				speechRecognition.SetStatic<bool>("enableOnPartialResults", enableOnPartialResults);
				enableOnPartialResultsJni = speechRecognition.GetStatic<bool>("enableOnPartialResults");
			}
			if(enableOnReadyForSpeechJni != enableOnReadyForSpeech){
				speechRecognition.SetStatic<bool>("enableOnReadyForSpeech", enableOnReadyForSpeech);
				enableOnReadyForSpeechJni = speechRecognition.GetStatic<bool>("enableOnReadyForSpeech");
			}
			if(enableOnRmsChangedJni != enableOnRmsChanged){
				speechRecognition.SetStatic<bool>("enableOnRmsChanged", enableOnRmsChanged);
				enableOnRmsChangedJni = speechRecognition.GetStatic<bool>("enableOnRmsChanged");
			}
			if(autoRestartJni != autoRestart){
				speechRecognition.SetStatic<bool>("autoRestart", autoRestart);
				autoRestartJni = speechRecognition.GetStatic<bool>("autoRestart");
			}
			if(autoRestartOnResumeJni != autoRestartOnResume){
				speechRecognition.SetStatic<bool>("autoRestartOnResume", autoRestartOnResume);
				autoRestartOnResumeJni = speechRecognition.GetStatic<bool>("autoRestartOnResume");
			}
			if(autoRestartAmpThresholdJni != autoRestartAmpThreshold){
				speechRecognition.SetStatic<float>("autoRestartAmpThreshold", autoRestartAmpThreshold);
				autoRestartAmpThresholdJni = speechRecognition.GetStatic<float>("autoRestartAmpThreshold");
			}
			if(touchToListenEnabled){
				if(state == State.IDLE){
					foreach(Touch t in Input.touches){
						if(t.phase == TouchPhase.Began && touchToListenRect.Contains(t.position)){
							StartListening();
						}
					}
				}else if(state == State.LISTENING_TO_SPEECH){
					bool touching = false;
					foreach(Touch t in Input.touches){
						if(touching || touchToListenRect.Contains(t.position)){
							touching = true;
						}
					}
					if(!touching){
						StopListening();
					}
				}
			}
#endif
		}
		//making sure the screen lock is not set
		if(screenLockTimeLeft > 0){
			screenLockTimeLeft -= Time.deltaTime;
			if(screenLockTimeLeft <= 0){
				Screen.sleepTimeout = SleepTimeout.SystemSetting;
			}
		}
	}

	void LateUpdate(){
		activeCommandsInThisFrame.Clear();
		activeCommandsInThisFrame.UnionWith(newCommands);
		newCommands.Clear();
	}

	void OnApplicationPause(bool paused) {
#if UNITY_ANDROID && !UNITY_EDITOR
		if(paused){
			speechRecognition.CallStatic("pause");
		}else{
			speechRecognition.CallStatic("resume");
		}
#endif
	}


	void OnApplicationQuit() {
#if UNITY_ANDROID && !UNITY_EDITOR
		speechRecognition.CallStatic("quit");
#endif
	}

	public static void AddSpeechRecognitionListeren(ISpeechRecognitionListener listener){
		eventListeners.Add(listener);
	}

	public static void RemoveSpeechRecognitionListeren(ISpeechRecognitionListener listener){
		eventListeners.Remove(listener);
	}

	public static void AddSpeechRecognitionListerenJs(ISpeechRecognitionListenerJs listener){
		eventListenersJs.Add(listener);
	}
	
	public static void RemoveSpeechRecognitionListerenJs(ISpeechRecognitionListenerJs listener){
		eventListenersJs.Remove(listener);
	}

	public static void SetTouchToListenZone(Rect rect, bool isUISpace){
		if(isUISpace){
			touchToListenRect = new Rect(rect.x,Screen.height-rect.y-rect.height,rect.width,rect.height);
		}else{
			touchToListenRect = rect;
		}
	}

	//watch out: enabling touchToListen will disable the autoRestart functionality 
	public static void SetTouchToListenEnabled(bool enabled){
		touchToListenEnabled = enabled;
		if(enabled){
			instance.autoRestart = false;
		}
	}

	public static bool GetTouchToListenEnabled(){
		return touchToListenEnabled;
	}

	public static bool IsRecognitionAvailable(){
		return isRecognitionAvailable;
	}

	public static SpeechDictionary GetSpeechDictionary(){
		return instance.speechDictionary;
	}

	public static bool CommandRecognized(string commandName){
		return activeCommandsInThisFrame.Contains(commandName);
	}

	public static void StartListening(){
#if UNITY_ANDROID && !UNITY_EDITOR
		speechRecognition.CallStatic("startListening");
#endif
	}

	public static void StopListening(){
#if UNITY_ANDROID && !UNITY_EDITOR
		speechRecognition.CallStatic("stopListening");
		instance.state = State.STOPPED_LISTENING;
#endif
	}
	
	private void OnBeginningOfSpeech(string message) {
		if(instance.enableEventLog){
			Debug.Log ("SpeechRecognition event: OnBeginningOfSpeech");
		}
		foreach(ISpeechRecognitionListener isrl in eventListeners){
			isrl.OnBeginningOfSpeech();
		}
		foreach(ISpeechRecognitionListenerJs isrl in eventListenersJs){
			isrl.OnBeginningOfSpeech();
		}
	}
	
	private void OnBufferReceived(string message) {
		if(instance.enableEventLog){
			Debug.Log ("SpeechRecognition event: OnBufferReceived, " + message);
		}
		byte[] buffer = utf8.GetBytes(message);
		foreach(ISpeechRecognitionListener isrl in eventListeners){
			isrl.OnBufferReceived(buffer);
		}
		foreach(ISpeechRecognitionListenerJs isrl in eventListenersJs){
			isrl.OnBufferReceived(message);
		}
	}

	private void OnEndOfSpeech(string message) {
		if(instance.enableEventLog){
			Debug.Log ("SpeechRecognition event: OnEndOfSpeech");
		}
		foreach(ISpeechRecognitionListener isrl in eventListeners){
			isrl.OnEndOfSpeech();
		}
		foreach(ISpeechRecognitionListenerJs isrl in eventListenersJs){
			isrl.OnEndOfSpeech();
		}
	}

	private void OnError(string message) {
		int error = -1;
		if(int.TryParse(message, out error)){
			if(instance.enableEventLog){
				Debug.Log ("SpeechRecognition event: OnError, " + errorMessages[error]);
			}
			foreach(ISpeechRecognitionListener isrl in eventListeners){
				isrl.OnError(error, errorMessages[error]);
			}
			foreach(ISpeechRecognitionListenerJs isrl in eventListenersJs){
				isrl.OnError(error, errorMessages[error]);
			}
		}
	}

	private void OnEvent(string message) {
		if(instance.enableEventLog){
			Debug.Log ("SpeechRecognition event: OnEvent, " + message);
		}
		string[] lines = message.Split(newLineSplit,System.StringSplitOptions.None);
		int eventType = -1;
		Dictionary<string,string> bundle = new Dictionary<string,string>();
		if(int.TryParse(lines[0],out eventType)){
			for(int i = 1 ; i < lines.Length; i+=2){
				if(i+1 < lines.Length){
					bundle.Add(lines[i], lines[i+1]);
				}
			}
			foreach(ISpeechRecognitionListener isrl in eventListeners){
				isrl.OnEvent(eventType,bundle);
			}
			foreach(ISpeechRecognitionListenerJs isrl in eventListenersJs){
				isrl.OnEvent(eventType,message);
			}
		}else if(instance.enableDebugLog){
			Debug.Log("Can't parse eventType");
		}
	}

	private void OnPartialResults(string message) {
		if(instance.enableEventLog){
			Debug.Log ("SpeechRecognition event: OnPartialResults, " + message);
		}
		string[] lines = message.Split(newLineSplit,System.StringSplitOptions.None);
		Dictionary<string,string> partialResults = new Dictionary<string,string>();
		for(int i = 0 ; i < lines.Length; i+=2){
			if(i+1 < lines.Length){
				partialResults.Add(lines[i], lines[i+1]);
			}
		}
		foreach(ISpeechRecognitionListener isrl in eventListeners){
			isrl.OnPartialResults(partialResults);
		}

		foreach(ISpeechRecognitionListenerJs isrl in eventListenersJs){
			isrl.OnPartialResults(message);
		}
	}

	private void OnReadyForSpeech(string message) {
		if(instance.enableEventLog){
			Debug.Log ("SpeechRecognition event: OnReadyForSpeech, " + message);
		}
		string[] lines = message.Split(newLineSplit,System.StringSplitOptions.None);
		Dictionary<string,string> bundle = new Dictionary<string,string>();
		for(int i = 0 ; i < lines.Length; i+=2){
			if(i+1 < lines.Length){
				bundle.Add(lines[i], lines[i+1]);	
			}
		}
		foreach(ISpeechRecognitionListener isrl in eventListeners){
			isrl.OnReadyForSpeech(bundle);
		}
		foreach(ISpeechRecognitionListenerJs isrl in eventListenersJs){
			isrl.OnReadyForSpeech(message);
		}
	}

	private void OnResults(string message) {
		if(instance.enableEventLog){
			Debug.Log ("SpeechRecognition event: OnResults, " + message);
		}
		string[] results = message.Split(newLineSplit,System.StringSplitOptions.None);
		foreach(ISpeechRecognitionListener isrl in eventListeners){
			isrl.OnResults(results);
		}
		foreach(ISpeechRecognitionListenerJs isrl in eventListenersJs){
			isrl.OnResults(message);
		}
		if(speechDictionary.enableDictionary){
			speechDictionary.TestResults(ref results, ref newCommands);
		}
		if(disableScreenLockOnResult){
			if(Screen.sleepTimeout != SleepTimeout.NeverSleep){
				Screen.sleepTimeout = SleepTimeout.NeverSleep;
			}
			screenLockTimeLeft = screenLockTimeout;
		}
	}

	private void OnRmsChanged(string message) {
		if(instance.enableEventLog){
			Debug.Log ("SpeechRecognition event: OnRmsChanged, " + message);
		}
		float rmsdB = -1;
		if(float.TryParse(message, out rmsdB)){
			foreach(ISpeechRecognitionListener isrl in eventListeners){
				isrl.OnRmsChanged(rmsdB);
			}
			foreach(ISpeechRecognitionListenerJs isrl in eventListenersJs){
				isrl.OnRmsChanged(rmsdB);
			}
		}
	}

	private void OnChangeState(string state){
		int state_ = 0;
		if(int.TryParse(state.Substring(0,1),out state_)){
			this.state = (State)state_;
			foreach(ISpeechRecognitionListener isrl in eventListeners){
				isrl.OnChangeState(this.state);
			}
			foreach(ISpeechRecognitionListenerJs isrl in eventListenersJs){
				isrl.OnChangeState((int)(this.state));
			}
		}
		if(enableDebugLog && state.Length > 1){
			Debug.Log (state.Substring(1));
		}
	}


}
