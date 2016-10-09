using UnityEngine;
using System.Collections;

public class playerManager : MonoBehaviour {


	public Podium[] rooms;
	public int currentRoom;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if (rooms [currentRoom].doubleReady == true) {
			iTween.MoveTo (gameObject,iTween.Hash("path",iTweenPath.GetPath(rooms[currentRoom].room.ToString()), "time",15));
			currentRoom++;	
		}
	}
}
