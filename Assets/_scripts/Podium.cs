using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Podium : MonoBehaviour {

	// Use this for initialization
	public orderManger om;
	public int podID;
	public bool objPlaced;
	public bool doubleReady;
	public Podium pair;
	public int room;


	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		if (pair.objPlaced && objPlaced == true) {
			doubleReady = true;
		}
	}
	void OnCollisionEnter(Collision col){
		
		if (col.gameObject.tag == "object") {
			col.gameObject.GetComponent<Rigidbody> ().isKinematic = true;
			col.gameObject.SetActive (false);
			om.order [podID] = col.gameObject.GetComponent<ObjectID> ().objID;
			objPlaced = true;

			Debug.Log (om.order[podID]);


		}
	}

}
