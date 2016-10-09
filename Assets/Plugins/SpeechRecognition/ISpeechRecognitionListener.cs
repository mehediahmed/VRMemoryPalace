using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public interface ISpeechRecognitionListener {

	void OnBeginningOfSpeech();
	void OnBufferReceived(byte[] buffer);
	void OnEndOfSpeech();
	void OnError(int error, string errorMessage);
	void OnEvent(int eventType, Dictionary<string,string> bundle);
	void OnPartialResults(Dictionary<string,string> partialResults);
	void OnReadyForSpeech(Dictionary<string,string> bundle);
	void OnResults(string[] results);
	void OnRmsChanged(float rmsdB);
	void OnChangeState(SpeechRecognition.State newState);
}
