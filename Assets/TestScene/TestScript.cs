using System.Collections;
using UnityEngine;

public class TestScript : MonoBehaviour {
	private float timeCounter_ = 0f;

	private IEnumerator Start () {
		while (true) {
			timeCounter_ += 1;
			Debug.Log(timeCounter_);
			yield return new WaitForSeconds(1f);
		}
	}
}
