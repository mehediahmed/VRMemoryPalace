using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpeechDictionary : MonoBehaviour {
	
	public bool enableDictionary = false;

	public Dictionary<string,List<string>> commands = new Dictionary<string, List<string>>()
	{
		{ "UP", new List<string>(){"up", "go up"}} ,
		{ "DOWN", new List<string>(){"down", "go down"}},
		{ "RIGHT", new List<string>(){"right", "go right"}},
		{ "LEFT", new List<string>(){"left", "go left"}}
	};

	//is filled on calling ReloadDictionary, is used for faster detection
	private Dictionary<string,HashSet<string>> revertedCommands = new Dictionary<string,HashSet<string>>();

	public void ReloadDictionary(){
		revertedCommands.Clear();
		foreach(KeyValuePair<string,List<string>> kvp in commands){
			foreach(string speechText in kvp.Value){
				string trimmed = speechText.Trim();
				if(revertedCommands.ContainsKey(trimmed)){
					revertedCommands[trimmed].Add(kvp.Key.Trim());
				}else{
					HashSet<string> hs = new HashSet<string>();
					hs.Add(kvp.Key.Trim());
					revertedCommands.Add(trimmed,hs);
				}
			}
		}
	}

	public void TestResults(ref string[] speechResults, ref HashSet<string> foundCommands){
		foreach(string result in speechResults){
			string trimmed = result.Trim();
			if(revertedCommands.ContainsKey(trimmed)){
				foundCommands.UnionWith(revertedCommands[trimmed]);
			}
		}
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
