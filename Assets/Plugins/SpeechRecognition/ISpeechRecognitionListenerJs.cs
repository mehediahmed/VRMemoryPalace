using UnityEngine;
using System.Collections;

public interface ISpeechRecognitionListenerJs{
	void OnBeginningOfSpeech();
	void OnBufferReceived(string buffer);
	void OnEndOfSpeech();
	void OnError(int error, string errorMessage);
	void OnEvent(int eventType, string bundle);
	void OnPartialResults(string partialResults);
	void OnReadyForSpeech(string bundle);
	void OnResults(string results);
	void OnRmsChanged(float rmsdB);
	void OnChangeState(int newState);
}
